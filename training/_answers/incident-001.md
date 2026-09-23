# ANSWER — INC-001 quota meter drift

## Root cause
PR #212 moved `user.Reserve(request.TotalSize)` into `StartUploadHandler.Handle` (`src/NetDropbox.Application/Uploads/StartUpload.cs`, after the quota check). Nobody noticed that this handler is **deliberately idempotent**. When the client resumes, it calls `POST /api/v1/uploads` again with the same parent, name and hash, and the handler returns the *existing* open session (`StartUpload.cs:51-69`). The reservation runs anyway, so each resume charges the full file size again. In the log, the 900 MB video was charged at 10:02 and again at 10:24. The third start at 10:52 then failed the quota check because of the user's own phantom reservations.

Two more leaks come from the same move:
- Sessions that never complete (abandoned, `Expired` by `JanitorWorker.SweepAsync`, `JanitorWorker.cs:37-44`) keep their reservation forever. Nobody calls `UserStorageAccount.Release`. It has **zero callers** in `src/`.
- `CompleteUploadHandler` can still reject the upload after the reservation, with "missing chunks" or "checksum mismatch" (`CompleteUpload.cs:42-48`). Nothing refunds the reservation in those cases either.

Why deleting files didn't help: `FileService.DeleteAsync` only soft-deletes (`FileNode.MarkDeleted`), and the janitor's 30-day purge (`JanitorWorker.cs:46-51`) never releases bytes either. `used_bytes` can only go up. That bug is on `main`, not new, but the PR made it visible.

## Diagnostic path a senior takes
1. The ratio `used_bytes / current bytes` climbs continuously → accounting drift, not a one-off migration.
2. The 409s are on `POST /uploads` (start), not on complete → the start path is involved.
3. The log shows the same file started three times → resumption. Read `StartUploadHandler`: the idempotent lookup plus the new unconditional `Reserve`.
4. `grep -rn "Release(" src` → no callers → explains the second quote.

## Fix (minimal, PR-sized)
Choose **one** reservation model and make it idempotent:
- **Option A (smallest):** revert to reserving at completion. Then close the oversubscription race separately with a conditional update: `UPDATE users SET used_bytes = used_bytes + @n WHERE id=@id AND used_bytes + @n <= quota_bytes` (check the affected-row count). The current `Version` concurrency token is inert (see review finding), so EF won't protect you.
- **Option B (the PR's intent):** add `ReservedBytes` to `UploadSession`. Reserve only when a *new* session is created (`existing is null`). Release it on expire, abandon and failed completion. On completion, convert the reservation to used bytes. Quota check = `UsedBytes + Σ open reservations + request.TotalSize ≤ Quota`.

Either way, also wire `Release` into the permanent purge path, or define "used" as "current versions of non-purged files" and recompute it.

## Data repair
Recompute from source of truth:
`UPDATE users u SET used_bytes = (SELECT coalesce(sum(v.size),0) FROM file_nodes n JOIN file_versions v ON v.id=n.current_version_id WHERE n.owner_id=u.id AND NOT n.is_deleted)`.
First decide whether "used" counts all versions and trashed files; the product has never defined this. Running it concurrently with uploads races with in-flight completes. Either run it per user under `SELECT ... FOR UPDATE` on the user row, or run it twice and alert on any delta.

## Regression tests
- Handler test: Start → Start again (resume) → assert `UsedBytes` changed exactly once, or not at all under Option A.
- Handler test: Start → janitor expires the session → assert the reservation is released (Option B).
- The current test project references only Domain and Application and has no DB provider. You need EF InMemory for fast handler tests (the Astra working-tree change adds it). You need Testcontainers PostgreSQL, already referenced but unused, for the concurrent-reserve test, because InMemory has no row locks or conditional updates.

## Model postmortem (short)
- **Impact:** about 6 % of upload starts rejected for 2 days. Usage meter wrong for every user who resumed or abandoned an upload.
- **Why review missed it:** the diff showed only the new line. The idempotent-resume branch ten lines below wasn't in the hunk. The PR test ("3 parallel uploads") never resumed.
- **The reviewer's missing question:** *"Is this handler called more than once per logical operation?"*
- **Actions:** idempotent reservation (owner: uploads); make `Release` reachable and add a nightly reconciliation job that alerts on drift over 1 %; add a handler-test harness; add "resume" to the upload smoke test.
