# ANSWER — M4 storage accounting

**Q1 walk-through.**
| Step | Should `ref_count` be | Today |
|---|---|---|
| A uploads X | 1 | 1 (`BlobObject.Create` sets 1) |
| B uploads X | 2 | 2 (`AddReference`) |
| A deletes, then purge | 1 | **2** (purge cascades versions, never decrements) |
| B copies X | 2 | 3 |
A's `used_bytes` never decreases (no `Release` caller). The blob's object is never deleted, even when every referencing version is gone.

**Q2 GC race.** GC reads `ref_count = 0` and deletes the S3 object. At the same moment `CompleteUploadHandler` finds the blob row (`:51`) and calls `AddReference` → a new version points at deleted bytes, so downloads 500 forever. Prevention:
- Mark before delete: `UPDATE blobs SET state='PendingDelete' WHERE id=@id AND ref_count=0 AND updated_at < now()-'24h'` (rows==1). Dedupe lookups only match `state='Live'`. A new upload of the same content creates a *new* blob row (the unique index must become partial on `state='Live'`, or re-key the row).
- A grace period covers in-flight transactions that read the row before the mark.
- Delete the object, then the row. Retries are idempotent (S3 delete of a missing key is 204).

**Q3.** The refcount must be exactly consistent with the set of versions. That's a DB invariant, so it lives in the same transaction. The S3 delete is irreversible and not transactional. If you did it inside the transaction and the transaction rolled back, the version would point at nothing. So: decrement in the transaction; physically delete later, asynchronously and idempotently.

**Q4.** Don't read-modify-write in C#. `BlobObject.RemoveReference` does `RefCount--` and relies on a token that never changes (review #10). Use `ExecuteUpdateAsync(s => s.SetProperty(b => b.RefCount, b => b.RefCount - 1))` or raw SQL. Same for `used_bytes`: `SET used_bytes = GREATEST(0, used_bytes - @n)`. Or fix the token with `IsRowVersion()` → `xmin` and retry on `DbUpdateConcurrencyException`.

**Q5 backfill.** Per user, in batches: recompute `used_bytes` from `file_versions` joined to `file_nodes`, following the quota definition you chose. Update only where there's a difference, one user per transaction, and throttle. For blobs: `UPDATE blobs b SET ref_count = sub.c FROM (SELECT blob_id, count(*) c FROM file_versions GROUP BY blob_id) sub WHERE …` in keyset batches of 10 k IDs. Run it twice. The second run should report 0 changed rows unless there's concurrent activity. Emit drift metrics.

**Quota definition (decide and document).** The common product choice: all versions of non-purged files count (trash counts until it's emptied). The current code charges on upload but never on copy (review #3). Fix copy too.

**Tests.** Two users share a blob: purge A → `ref_count` 1, and A's `used_bytes` drops. GC doesn't delete while a completion references the blob (inject a barrier). Reconciliation is idempotent.
