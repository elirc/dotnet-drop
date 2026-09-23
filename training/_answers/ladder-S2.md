# ANSWER — S2 migrations baseline + scan status move (reference)

**1. Baseline.** Generate `0001_Baseline` from the current model (after J0's mapping fix). Diff its SQL against prod's schema (`pg_dump --schema-only`) and fix any drift by hand first. In prod, insert the baseline row into `__EFMigrationsHistory` without running it. From then on, migrations run as a **separate pipeline step** (`dotnet ef migrations bundle` → run once, before rolling pods). Not at app startup, because three replicas would race to migrate and a failed migration would crash-loop every pod. Remove `EnsureCreatedAsync` from anything but local dev.

**2. Expand/contract (each line is one deploy):**
| # | Schema | Code | Old code vs new schema | Rollback |
|---|---|---|---|---|
| D1 | add `file_versions.scan_status varchar(32) NULL` | none | ignores the column | drop the column |
| D2 | — | **dual-write**: worker, copy and restore write both the node and the version | D1 code writes only the node → backfill catches it | redeploy D1 |
| D3 | — | backfill job (below), then a verify query | — | rerun (idempotent) |
| D4 | — | **read** from the version (download gate, DTO derived); still dual-write | D2/D3 pods still correct | flip the read flag back |
| D5 | — | stop writing the node column | nothing reads it | redeploy D4 |
| D6 | drop `file_nodes.scan_status` (after ≥ 1 week) | — | — | point of no return; take a backup first |

A replica still running D1 during D2's rollout writes only the node column. That's why the backfill must run *after* D2 is fully rolled out, and why the verify query runs right before D4.

**3. Backfill SQL (one batch):**
```sql
WITH batch AS (
  SELECT v.id, n.scan_status FROM netdropbox.file_versions v
  JOIN netdropbox.file_nodes n ON n.current_version_id = v.id
  WHERE v.id > @last_id AND v.scan_status IS NULL ORDER BY v.id LIMIT 5000)
UPDATE netdropbox.file_versions v SET scan_status = b.scan_status FROM batch b WHERE v.id = b.id
RETURNING v.id;
```
Keyset on `id`. Batch size 5 k. Sleep ~100 ms between batches, or throttle on replication lag. Persist `@last_id` in a progress table. Non-current versions get `PendingScan` (unknown). It's idempotent because of `scan_status IS NULL`. A crash at 60 % resumes from the saved ID.

**4. Indexes:** `CREATE INDEX CONCURRENTLY` via `migrationBuilder.Sql(..., suppressTransaction: true)`. Check for `INVALID` indexes afterwards (`pg_index.indisvalid`).

**5. Verify:** `SELECT count(*) FROM file_nodes n JOIN file_versions v ON v.id = n.current_version_id WHERE n.scan_status <> v.scan_status;` must be 0 (or explained by in-flight scans), sampled twice, 10 minutes apart.

**6. Drills.** Backfill dies → resume from the checkpoint. D4 rolled back after 2 h → dual-write kept both columns correct, so there's no data loss. That's the point of dual-writing before switching reads. Stale D1 replica → covered by ordering D3 after the full D2 rollout.

**Explain-before-touching.** PG11+ stores a constant default in the catalog, so `ADD COLUMN … DEFAULT` doesn't rewrite the table, but `NOT NULL` with a *volatile* default does. We add a nullable column anyway. Unsafe EF operations on big tables: `AlterColumn` type changes (rewrite), `CreateIndex` without CONCURRENTLY (blocks writes), `RenameColumn` (breaks old code during a rolling deploy), `AddForeignKey` (validates while locking; use `NOT VALID` then `VALIDATE CONSTRAINT`).
