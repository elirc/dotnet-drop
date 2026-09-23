# NetDropbox technical questions (20)

Answer out loud, then in writing. The key is in `training/_answers/interview-technical.md`.

> Do this after the review and the incidents. Several questions (5, 11, 13) name findings those exercises ask you to discover.

## API design
1. `POST /api/v1/uploads` returns an existing open session when owner, parent, name and hash match (`StartUpload.cs:51-69`). What makes that idempotent, what race still creates two sessions, and what DB change closes it?
2. `UploadsController.Status` returns 500 for someone else's session. What should it return, and why 404 over 403?
3. The public share password is a query parameter (`PublicSharesController.cs:15`). Redesign the flow without breaking existing links.
4. You need pagination on `GET /api/v1/files` without breaking `web/src/api.ts`, which expects an array. Give two options.

## Database
5. `Entity.Version` is configured with `IsConcurrencyToken()`. Why does it protect nothing here, and what are two ways to make `used_bytes` updates safe?
6. Why is the `(OwnerId, ParentId, Name, IsDeleted)` index not enough to stop duplicate names? Write the index that is.
7. Only one foreign key exists in the model. Name three relationships that should have one, and what breaks in the janitor purge because they don't.
8. `SearchAsync` uses `ToLower().Contains` on names and on 1 MB text. Predict the plan at 2 M rows and give the index for each column.

## Concurrency and async
9. Walk through two concurrent `complete` calls on the same session. Where do they diverge from correct behavior?
10. The process dies between `SaveChangesAsync` and `EnqueueAsync` in `CompleteUploadHandler`. What's the user-visible effect, and how would you have detected it?
11. Why did adding `Task.Delay` in the worker's catch block stall all processing (INC-002)?
12. A blob's refcount reaches 0 and GC deletes it while another upload dedupes onto it. How do you prevent that?

## Security
13. Two paths let a user download bytes that weren't scanned Clean. Name both.
14. What's wrong with `jwt["SigningKey"] ?? "local-development-…"`, and how should config for secrets fail?
15. `OpenPublicDownloadAsync` doesn't check `IsDeleted`. Is that a bug or a product decision? Argue both sides, then pick one.
16. How would you make authorization mistakes like INC-003 harder to write in this codebase?

## React / frontend
17. `sha256(file)` in `api.ts` calls `blob.arrayBuffer()` on the whole file. What happens with a 4 GB video, and what's the fix?
18. `PreviewPanel` downloads the full file when a row is selected and never revokes object URLs. What's the user impact after clicking through 30 large files?

## Testing and operations
19. The test project has 6 domain tests and an unused Testcontainers reference. Which three tests would you add first, and at what layer?
20. The API and the background worker share a process. Give one failure where that coupling hurts, and the deployment change that removes it.
