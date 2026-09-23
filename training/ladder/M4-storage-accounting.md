# M4 — Storage accounting that goes down too

**Context.** `UserStorageAccount.Release` and `BlobObject.RemoveReference` exist but have **no callers** in `src/`. `JanitorWorker.SweepAsync` hard-deletes trash older than 30 days (`src/NetDropbox.Infrastructure/Processing/JanitorWorker.cs:46-51`). That cascades `file_versions` (the only FK in the model) but leaves `blobs.ref_count` and the files in blob storage untouched. Users' `used_bytes` only ever grows.

**Ticket (finance + support).** "Storage bills grow about 9 % a month while active data is flat. Customers who empty their trash still hit quota. Make deletion actually free space: quota first, then physical blobs."

**Constraints.**
- A blob can be shared by versions in several accounts (content-addressed dedupe, `CompleteUpload.cs:51-61`).
- Deleting bytes from S3 is irreversible. Nothing may delete a blob that a concurrent `complete` or `copy` is about to reference.
- Define (and document) what counts toward quota: all versions? trashed items?

**Definition of done.**
- Permanent delete (the janitor purge and a new "empty trash") decrements refcounts and releases quota in the **same transaction** as removing the versions.
- Blob garbage collection is a separate, delayed, idempotent step, with a race analysis written out.
- A nightly reconciliation that recomputes `used_bytes` and `ref_count` from source rows, reports drift and fixes it behind a flag.
- Tests: refcount correctness across two users sharing a blob; purge releases quota; the GC race (concurrent re-reference) is covered.

**Explain before touching.**
1. Walk through: user A uploads X, user B uploads identical X, A deletes and purges, B copies X. What should `ref_count` be after each step? What is it today?
2. The GC race: GC sees `ref_count = 0` and deletes the S3 object while `CompleteUploadHandler` finds the blob row and calls `AddReference`. How do you prevent it? (Tombstone plus grace period? Row lock? A `deleting` state?)
3. Why must the refcount change and the version delete share a transaction, while the S3 delete must not?
4. Given review #10 (the inert concurrency token), how do you make `ref_count` updates safe under concurrency?
5. How do you backfill correct values for existing data without locking `users` for minutes?

<details><summary>Hints</summary>

- `UPDATE blobs SET ref_count = ref_count - 1 WHERE id = @id` (atomic in SQL, no read-modify-write in C#).
- GC: `WHERE ref_count = 0 AND updated_at < now() - interval '24 hours'` → mark `pending_delete` → delete the object → delete the row. Upload dedupe must ignore `pending_delete` blobs.
</details>

Answer: `training/_answers/ladder-M4.md`
