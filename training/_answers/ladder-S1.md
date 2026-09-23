# ANSWER — S1 durable processing pipeline (reference design + rubric)

**Explain-before-touching answers.**
- Crash after `SaveChangesAsync` (`CompleteUpload.cs:82`) and before `EnqueueAsync` (`:84`): the job row is `Queued`, but no message exists anywhere. The file stays `PendingScan` forever; nothing re-reads `processing_jobs`.
- In the recommended design there's no separate publish: the job row *is* the queue, committed atomically with the version. If you add RabbitMQ with an outbox, the relay publishes after commit. Publish fails → retry from the outbox; publish succeeds but marking it sent fails → duplicate publish → the consumer must be idempotent.
- Why not synchronous: scanning, thumbnails and extraction on multi-GB files would push `complete` past gateway timeouts and couple upload availability to scanner availability. You'd also scale web and CPU work together.

**Reference choice: Postgres jobs table as the queue** (reusing `processing_jobs`), not RabbitMQ, at this scale.
- Why: the handoff is atomic with the upload transaction (no dual write), there's one fewer system to operate, and ~10⁵–10⁶ jobs/day is comfortably within `SKIP LOCKED` range. Revisit when you need more than about 1–2 k jobs/s, fan-out to other services, or cross-region consumers. Then add an outbox → broker.
- Claim: `UPDATE processing_jobs SET status='Running', lease_until=now()+'2 min', attempts=attempts+1 WHERE id IN (SELECT id FROM processing_jobs WHERE status IN ('Queued','Failed') AND next_attempt_at <= now() ORDER BY priority, next_attempt_at FOR UPDATE SKIP LOCKED LIMIT 10) RETURNING *`.
- Lease expiry: a sweeper moves `Running AND lease_until < now()` back to `Failed` (worker death).
- **Idempotency key:** `(blob_id, scanner_signature_version)` for the scan (M3). A re-run writes the same verdict. Thumbnail key `thumb/{version_id}` (not `{node_id}`, which caused the stale-thumbnail bug) is written with overwrite semantics.
- **Retries:** `next_attempt_at = now() + base * 2^attempts + jitter`. No sleeping in the consumer (INC-002). Classify errors: `UnknownImageFormatException` and other deterministic ones → `DeadLettered` immediately; IO or timeouts → retry up to 5. The node is only `Failed` once the scan job dead-letters.
- **DLQ:** status `DeadLettered`, an admin endpoint or SQL runbook to inspect and requeue, and an alert when the count is above 0 for 30 minutes.
- **Fairness:** `priority` (interactive = 0, bulk import = 10) plus a per-tenant cap: the claim query excludes tenants with ≥ N `Running`. Or two worker pools polling different priorities. The bulk tenant can't starve the interactive p95.
- **Stages:** separate job rows per kind (`VirusScan`, `Thumbnail`, `TextExtraction`, already in `ProcessingJobKind`). The scan job gates downloads; the others are enqueued only after Clean and never change `ScanStatus`.
- **Hosting:** workers as a separate deployment (the same image, `--role worker`), so a crash doesn't take down the API (review #7) and each scales on its own.
- **Observability:** `jobs_queued{kind,priority}`, `oldest_queued_age_seconds{kind}`, `job_duration_seconds` histogram, `job_attempts` histogram, `dead_letter_total`. Alerts: oldest interactive scan > 60 s; DLQ growth; lease-expiry rate > 1 %.
- **Rollout:** deploy 1: the worker also polls the DB while the Channel stays. Deploy 2: `complete` stops enqueuing to the Channel. Deploy 3: remove the Channel. Also run a one-off requeue of all `Queued` or `PendingScan` rows older than 10 minutes, which recovers the historical losses. Rollback at any step is safe, because both paths are idempotent.
- **Tests:** kill the worker mid-job → the lease expires → the job completes once. Concurrent claimers in a Testcontainers test → no job claimed twice. Poison plus good → the good one completes promptly. Load test: 500 k bulk jobs while 1 interactive upload/s → scan p95 < 10 s.

**Rubric.** Strong: names the dual-write line, picks one handoff, *and* states when to revisit; idempotency key tied to content; lease semantics; fairness mechanism; stage separation; alert thresholds; rollout with recovery of already-lost jobs. Weak: "use RabbitMQ with retries" and no crash analysis.
