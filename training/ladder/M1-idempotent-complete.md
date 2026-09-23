# M1 — Idempotent upload completion

**Context.** `POST /api/v1/uploads/{id}/complete` (`CompleteUploadHandler`, `src/NetDropbox.Application/Uploads/CompleteUpload.cs:28-87`) reassembles and hashes the whole file before it commits. For a 3 GB file that takes about 40 s, which is longer than a typical 30 s gateway timeout. The client will soon retry on timeout.

**Ticket (lead).** "Mobile is adding retry-on-timeout for `complete`. Today a retry can (a) create two versions of the file, (b) charge quota twice, (c) bump the blob refcount twice, or (d) return 409 'no longer open' to a user whose upload actually succeeded. Make `complete` safe to call any number of times, concurrently."

**Constraints.**
- The same session ID must always yield the same `FileNodeDto`.
- The second call must not re-hash the file if the first succeeded.
- There's no distributed lock service. You have Postgres.
- Remember `Entity.Version` is configured as a concurrency token but never changes (review #10).

**Definition of done.**
- Sequential retry → 200 with the same body. Two concurrent calls → exactly one version, one reservation, one refcount increment, one job. The loser gets 200 (same body) or a documented 409 the client handles.
- A concurrency test against **real Postgres** (Testcontainers) that fires two completes at once.
- The failure matrix below, filled in.

**Explain before touching.**
1. Draw the interleaving of two concurrent completes on the current code. Mark the line where both pass the guard.
2. Options: `SELECT … FOR UPDATE` on the session row; an atomic `UPDATE upload_sessions SET status='Completing' WHERE id=@id AND status='Open'` claim; a working `xmin` row version; an idempotency-key table. For each, what happens if the process dies after claiming?
3. Where is the transaction boundary today? Which effects are **outside** the DB (blob write, chunk delete, enqueue), and in what order should they happen?
4. Fill in this matrix: crash after blob saved / after `SaveChanges` / after chunk delete / after enqueue. What state is left each time, and what does a retry do?
5. What does the client need from the API to know "already completed"? Check `UploadSession.CompletedFileNodeId`.

<details><summary>Hints</summary>

- A claim-by-update with an affected-row check gives single-winner semantics without holding a lock during the 40 s hash. Add a `Completing` status with a lease timeout so a crashed claimer can be taken over.
- Completed + `CompletedFileNodeId` set → load and return that node. That's your idempotent response.
</details>

Answer: `training/_answers/ladder-M1.md`
