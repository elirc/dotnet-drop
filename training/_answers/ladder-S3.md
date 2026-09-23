# ANSWER — S3 three replicas (reference)

**Explain-before-touching.** Chunk 0 PUT → replica A writes `A:/app/chunks/{session}/00000000.chunk` and adds the DB row. Chunk 1 → replica B, same thing on B's disk. `complete` → replica C: the DB says all chunks are present, then `FileSystemChunkStorage.OpenAssembledAsync` does `File.OpenRead` on C's disk → `FileNotFoundException` → 500. Every multi-chunk upload fails whenever requests spread across replicas. With 4 parallel chunk requests (`App.tsx:285`), that's nearly always.

**1. State inventory → fix.**
| Item | Breaks | Fix |
|---|---|---|
| Local chunks | see above | presigned S3 multipart (below), or a shared volume (EFS/Azure Files: slow and costly), or sticky sessions (fragile under scale-in) |
| Local blob provider | files exist on one replica | `Storage:Provider=S3`, enforced at startup in non-dev |
| Channel queue + worker per replica | messages lost on scale-in; no shared view | S1 (DB queue), separate worker service |
| Janitor on every replica | 3× the sweeps; racing deletes | a Postgres advisory lock (`pg_try_advisory_lock(42)`) around the sweep, or a scheduled job |
| In-memory rate limit | the limit becomes 3× and resets on deploy | a distributed limiter (Redis, finally used) or WAF/API gateway rate rules |
| `EnsureCreated`/seed | any pod in Development env would mutate prod | separate migration step (S2), and never run with `Development` in the cloud |

**2. Uploads.** Presigned multipart: the API creates the session and the S3 `CreateMultipartUpload`, then returns presigned part URLs. The client PUTs parts directly to S3, and `complete` calls `CompleteMultipartUpload`. Checksums: S3 supports `x-amz-checksum-sha256` per part, so integrity survives. The final whole-file SHA-256 must then be computed by a worker streaming from S3, or you trust S3's composite checksum and dedupe on it. Quota: reserve at session start (INC-001 lesson: idempotently) and release on abort or expiry via an S3 lifecycle rule plus the janitor. Cost: less API egress and CPU, and no 64 MB request bodies through the load balancer. Trade-off: more client logic, and dedupe happens after upload, not before.

**3. Pipeline.** CI builds one image and tags it with the git SHA. Stages: migrate (bundle) → deploy to staging → smoke (login, upload 2 chunks, complete, download) → prod rolling with `maxUnavailable=0`. Readiness = DB + S3 reachable + options validated. Liveness must not check dependencies (to avoid cascading restarts). Rollback = redeploy the previous digest. This is safe only if the migration was an *expand* step (S2). Contract steps ship a release later.

**4. Config.** Secrets live in AWS Secrets Manager / Azure Key Vault, injected as env vars or read via the managed identity provider. `ValidateOnStart` for the JWT key, the DB and the S3 bucket, and refuse the dev key outside Development (INC-005). S3 access through the task role / managed identity, with no static access keys (`appsettings.json:15-16` has static keys today).

**5. Day-one observability.** Dashboards: (1) API RED per route; (2) upload funnel: start → chunks → complete → scan clean, with a latency breakdown; (3) DB: connections, slow queries, replication lag. Alerts: 5xx > 1 % for 5 min; scan oldest-age > 60 s; `complete` p95 > 20 s; DB connections > 80 % of pool; DLQ > 0 for 30 min.

**6. Cost (order of magnitude, 2026 list prices, check before quoting):** 3 small containers ≈ tens of $/month; Postgres with an HA standby ≈ low hundreds of $/month; 20 TB of object storage ≈ $400–500/month in storage alone, plus **egress**, which usually dominates for a file-sharing product. Downloads and public links at a few TB/month cost more than compute. Put a CDN in front of public-share downloads.

**AWS versus Azure.** Either is fine. Pick the one the team already runs. Justify it by managed identity, the multipart/presign API, and the Postgres HA offering, not by features you won't use.
