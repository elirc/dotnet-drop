SEALED — attempt first. Open this only after you've written your own answers to `learn/00-onramp.md`.

# Answers: NetDropbox on-ramp

Line numbers are for `d612e0b`, the original source the on-ramp targets (see the status note in `learn/00-onramp.md`).

## 1. Quota meter
- `Create` trims and lower-cases the email (`UserStorageAccount.cs:11`), so (d) prints `a@example.com`.
- After `Reserve(8)`: `UsedBytes = 8`.
- (a) `8 + 3 > 10`, so it throws `DomainException("Storage quota exceeded.")` (`:38`). `UsedBytes` stays **8**, because the check runs before the add.
- (b) `Math.Max(0, 8 - 20)` → **0** (`:46`). Release silently clamps. An over-release is hidden, not reported. That's a design smell: a bug that releases twice would never show up.
- (c) `0 + 10 > 10` is false, so it succeeds: **10**. The quota is inclusive.
- `grep -rn "Release(" src` finds only the definition at `:43`. There are **zero callers**. `UsedBytes` can only go up in this app: deleting files, the 30-day trash purge (`JanitorWorker.cs:46-51`) and expired sessions never give bytes back. INC-001 and ladder M4 are built on this fact.

## 2. Duplicate folder trace
1. `FilesController.CreateFolder` (`FilesController.cs:23-25`) calls `FileService.CreateFolderAsync` with `User.UserId()` (`CurrentUser.cs:7-12` reads the `NameIdentifier` claim).
2. `FileService.cs:36` runs `AnyAsync(...)` → true → `:37` throws `AppException("A node with that name already exists.", Conflict)`.
3. The exception bubbles out of MVC into `ExceptionMiddleware.InvokeAsync` (`ExceptionMiddleware.cs:15`). `:19` maps `AppException` to its status, **409**. No error log, because status < 500 (`:25`). `:26-34` writes ProblemDetails with `Title` and `Detail` both set to the message.
4. `web/src/api.ts:74-76`: `response.ok` is false → parse the JSON → `throw new ApiError(problem.detail ?? problem.title ...)` → the user sees **"A node with that name already exists."**
- `CorrelationIdMiddleware` runs first (`Program.cs:78`) but doesn't change the status.
- **Bonus:** yes, two folders can be created. It's a check-then-act race: both requests run `AnyAsync` before either commits, and both insert. The index at `NetDropboxDbContext.cs:38` is **not unique** (no `.IsUnique()`), so the database doesn't stop it. It also includes `IsDeleted`, so even a unique version would need care (a partial unique index `WHERE NOT is_deleted`). That's ladder J3 and *curriculum 03* (external apprenticeship note, not in this repo).

## 3. The Move guard
- (a) **No.** Line 46 only fires if a *file* is moved into a parent whose id equals its own id. But `MoveAsync` loads the parent and rejects anything that isn't a folder (`FileService.cs:58`), and a file's id can never be a folder's id. For this path the domain guard is dead code: it looks like protection and protects nothing.
- (b) `GetOwnedNode(A)` ✓, `GetOwnedNode(B)` ✓, `B.Type == Folder` ✓, `A.Move(B.Id)`: `A` is a folder, so line 46 doesn't apply → saved. Now `A.ParentId = B` and `B.ParentId = A`. That's a **cycle**, detached from the root. Neither folder appears in any listing that starts at the root, and a recursive walk (for sharing or deleting a subtree) would loop forever. Moving `A` into `A` directly also succeeds, since a folder is a valid parent.
- (c) **J2**, "Stop folders being moved into themselves" (it needs an ancestor walk, e.g. a recursive CTE).

## 4. Schema
- (a) Unique indexes:
  - `users.email` (`:28`): one account per email.
  - `file_versions (file_node_id, version_number)` (`:54`): no two "v3"s for one file. Note that `AddVersion` computes `max + 1` in memory (`FileNode.cs:55`), so two concurrent completes for the same file will have one fail on this index with a 500. That's good (no corruption) but the error is ugly.
  - `blobs.content_hash` (`:62`): one stored copy per content, which is the dedupe.
  - `upload_chunks (upload_session_id, index)` (`:80`): a chunk index is recorded once.
  - `share_links.token` (`:87`): tokens are unique.
  - `direct_shares (file_node_id, shared_with_user_id)` (`:96`): no duplicate share to the same user.
  - Not unique: `file_nodes (owner, parent, name, is_deleted)` (`:38`), `upload_sessions` (`:70`), `processing_jobs` (`:111`).
- (b) **One** FK: `file_versions.file_node_id → file_nodes`, cascade (`:44-47`). Nothing in the database stops `parent_id`, `current_version_id`, `owner_id`, `blob_id` and the rest from dangling. Only C# code checks them, and only on the paths that remember to.
- (c) Nothing in `src/` assigns `Version`. `Entity.cs:8` has `protected set`, and there are no writes. EF adds `WHERE id = @id AND version = @originalVersion` (here always 0) to every UPDATE. Because no writer ever changes the value, two concurrent updates both match and both succeed, so the last write wins. The update affects 0 rows only if the row was **deleted** in between. The token is inert. To make it real you need `xmin`/`IsRowVersion()` or code that increments it on every change. (B01 verified: plain `bigint`, `ValueGenerated=Never`. Review finding #10.)
- (d) `ProcessingWorker.cs:55` stores up to 1 MB of the file's text (`ReadTextSampleAsync`, `:78-92`) for text/JSON/XML/JS mime types, for search. A 1 MB string is why `text` is used, and it's also why loading whole `FileNode` entities in a listing is expensive (INC-004).

## 5. Side effects of CompleteUpload
- (a) In order:
  - **`:45`** chunks are assembled (`OpenAssembledAsync`, a file-system read; it may create a temp file, but the adapter isn't in `main`'s git: UNVERIFIED).
  - `:46` hash (a read only).
  - **`:54`** `blobs.SaveAsync`, a blob-store write, only if the hash is new.
  - `:56`/`:60` a new `blobs` row, or `ref_count + 1` (tracked, not yet written).
  - `:71` a new `file_nodes` row if needed.
  - `:74` a new `file_versions` row, and the node's `current_version_id` / `scan_status = PendingScan`.
  - `:76` a `processing_jobs` row.
  - `:77` the session becomes Completed.
  - `:80` `users.used_bytes += size`.
  - **`:82` the one DB transaction.**
  - **`:83`** chunk files deleted (file system).
  - **`:84`** message enqueued into the in-memory `Channel`.
- (b) Inside the transaction: every *tracked* change (`:56/60, 71, 74, 76, 77, 80`). Before it: the blob write (`:54`). After it: chunk deletion (`:83`) and the enqueue (`:84`). Three stores, one transaction that covers only one of them.
- (c) The chunk files stay on disk forever: the janitor only cleans chunks for **Open** expired sessions (`JanitorWorker.cs:37-44`), and this session is Completed. The message is never enqueued, so the file stays `PendingScan` forever and every download returns 409 (`FileService.cs:152`). The `processing_jobs` row says `Queued`, which is false: nothing reads that table back (`grep -rn ProcessingJobs src` shows only writes and the worker's lookup by id). This is the dual-write gap that ladder S1 and *curriculum 01* (external apprenticeship note, not in this repo) address.
- (d) An **orphan blob**: bytes in the store with no `blobs` row. No code lists the store to find orphans. The next upload of the same content saves again, probably to the same hash-derived key (UNVERIFIED: the adapter isn't in `main`'s git).

## 6. Characterization test (reference)
```csharp
public sealed class BlobObjectTests
{
    [Fact] public void New_blob_starts_with_one_reference() =>
        BlobObject.Create(new string('a', 64), 10, "k").RefCount.Should().Be(1);

    [Fact] public void Removing_more_references_than_exist_stops_at_zero()
    {
        var blob = BlobObject.Create(new string('a', 64), 10, "k");
        blob.RemoveReference(); blob.RemoveReference();
        blob.RefCount.Should().Be(0);
    }

    [Fact] public void Zero_byte_upload_has_one_chunk() =>
        UploadSession.Create(Guid.NewGuid(), null, "empty.txt", 0, new string('a', 64), "text/plain", 4_000_000)
            .TotalChunks.Should().Be(1);
}
```
- Grading: the names describe *behavior*, there is one behavior per test, and the test goes red when you mutate the line it pins.
- Zero-byte uploads: the client (`App.tsx:275`) will try to send chunk 0 as an empty slice, and the server requires indexes 0..TotalChunks-1 (`CompleteUpload.cs:42`), so one chunk is consistent. **Keep** it. It's a product question ("do we allow empty files?") to confirm with whoever owns the upload API. Never "fix" a characterized behavior without asking.
- Silent clamping in `RemoveReference` at 0 is the same smell as `Release` in #1. Pin it now, and question it in M4.
- At `d612e0b` the test project references only Domain and Application, so this suite should build there without J0 (not executed in the 2026-10-06 re-check). On current `main` it does **not**: the test project now references Infrastructure, which needs the missing storage adapters.

## 7. Resume edge cases
- (a) An upper-case hash never matches the stored lower-case value, so every "resume" creates a **new** session and re-uploads everything. The comparison should normalize the request (`Trim().ToLowerInvariant()`) the same way the entity does. The real client emits lower-case hex (`api.ts:163`, `toString(16)`), so this doesn't happen today. It will bite the first other client (a CLI or mobile app). The lesson: normalization rules must be applied at *every* comparison, not only at creation.
- (b) The lookup has no `ExpiresAt` filter, so it returns the stale Open session. The first chunk PUT hits `UploadChunk.cs:34`: `IsExpired` → **409 "Upload session is no longer open."** If all chunks were already there, `CompleteUpload.cs:33` gives the same 409. `uploadOne` rejects, `Promise.all` in `uploadFiles` rejects (`App.tsx:69`), `setUploading({})` never runs, and the progress bar freezes with the error unhandled. The user retries and gets the same stale session **until the janitor marks it Expired**. The janitor runs every 15 minutes (`JanitorWorker.cs:26`), so the window is at most about 15 minutes, or indefinitely if the sweep keeps failing (it logs and loops, `:21-24`). Fix: add `&& x.ExpiresAt > now` to the lookup.

## 8. Expired token in the SPA
- (a) `[Authorize]` controllers return **401** (the JWT is past `exp` plus the 1-minute `ClockSkew`, `Program.cs:55`). The 401 comes from the auth middleware, so the body is usually empty. `request()` (`api.ts:74-76`) catches the JSON parse failure and throws `ApiError(response.statusText, 401)`, i.e. "Unauthorized". React Query puts the query in an error state, so the file list shows an error or stays empty.
- (b) **No.** Nothing on the 401 path calls `setToken(null)`. `App` only shows `<Login>` when `getToken()` is falsy (`App.tsx:13-17`), and the token is still in `localStorage`. Every request keeps failing, and a reload doesn't help. The only way out is the Logout button or clearing storage.
- (c) Handle 401 centrally in `request()` and `downloadBlob()`: clear the token and notify the app (a callback or event) so `App` re-renders `<Login>`. Test it with a mocked `fetch` returning 401, then assert that `localStorage` is empty and the login form renders. Full reference: `training/_answers/ladder-J4.md`.
