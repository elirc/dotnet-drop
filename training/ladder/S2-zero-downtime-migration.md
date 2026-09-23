# S2 — Adopt migrations, then move scan status to versions, with zero downtime

**Scenario.** The schema is created only by `EnsureCreatedAsync` in Development (`src/NetDropbox.Infrastructure/Persistence/DbSeeder.cs:14`, `src/NetDropbox.Api/Program.cs:81-86`). Production was bootstrapped by hand from a dev database. M3 concluded that scan state must live on `file_versions` (or `blobs`), not `file_nodes.scan_status`. Production has 40 M `file_nodes`, 55 M `file_versions`, and 2 API replicas that are rolled one at a time. Uploads and downloads continue during deploys, and there's no maintenance window.

**Operating constraints.**
- During a rolling deploy, old and new code run **at the same time** against the same schema.
- Any step must be reversible until the final "contract" step.
- A backfill of 55 M rows must not saturate IO or hold long locks.
- `FileNodeDto.ScanStatus` is rendered by the web client (`web/src/App.tsx:203`) and must keep working.

**Deliverable (≤ 2 pages).**
1. **Baseline:** how you introduce EF migrations onto an existing hand-made schema without re-creating tables (baseline migration, `__EFMigrationsHistory` seeding, drift check). Who runs migrations: app startup, a CI job, or a separate step? Why?
2. **Expand/contract plan** as numbered deploys. For each deploy: the schema change, the code change, what old code does against the new schema, and the rollback.
   - add `file_versions.scan_status` (nullable) → dual-write → backfill in batches → read from the version → stop writing the node column → drop the column.
3. **Backfill:** batch size, keyset iteration, throttling, idempotency, and progress tracking. Show the SQL for one batch.
4. **Index creation** without write locks.
5. **Verification:** the queries that prove node and version status agree before you flip reads.
6. **Failure drills:** the backfill dies at 60 %; deploy 3 is rolled back after 2 hours of dual-writing; a replica still running deploy-1 code writes the old column only.

**Explain before touching.**
- Why is `ALTER TABLE … ADD COLUMN … NOT NULL DEFAULT …` fine on PG16 but a rewrite on older versions? Does that matter here?
- Which EF-generated migration operations are unsafe on a 55 M-row table, and how do you override them?

Answer: `training/_answers/ladder-S2.md`
