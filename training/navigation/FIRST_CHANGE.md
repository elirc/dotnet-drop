# Before your first production change to NetDropbox

1. **`main` doesn't build or boot as committed.** The storage adapters are git-ignored and the EF model throws. Do ladder J0 first, or you'll debug the wrong thing.
2. **Uncommitted work exists.** The working tree holds the Astra upload-integrity changes (`astraupskill/`, modified upload handlers, new tests). Never branch-switch in that tree; use a worktree.
3. **Three stores, no shared transaction.** The DB, blob storage and the in-memory queue. Any change to `CompleteUploadHandler` must be reasoned as "what if the process dies between these two lines" (`CompleteUpload.cs:54`, `:82`, `:83`, `:84`).
4. **Authorization is inline, per query.** There's no policy layer. Every read path repeats `x.OwnerId == userId`. When you add a read path, copy the predicate and add a test with a *stranger* user (INC-003).
5. **Scan status gates downloads, and it lives on the node, not the bytes.** Any path that serves or duplicates bytes must go through the gate (review #3–#4, ladder M3).
6. **Concurrency tokens don't work.** `Version` never changes. Don't rely on EF to catch lost updates on `users.used_bytes`, `blobs.ref_count` or `share_links.access_count`. Use atomic SQL updates or fix the token.
7. **The worker shares the API process.** An exception outside the worker's try block stops the host (`ProcessingWorker.cs:32-34`). Anything you add to the worker needs its own error boundary.
8. **Config falls back silently.** JWT key, connection string and CORS origins all have hard-coded fallbacks (`Program.cs:36,42,74`, `DependencyInjection.cs:19-20`). A typo in a key name doesn't fail; it quietly uses dev values.
9. **No migrations.** Schema changes need S2's baseline first. Don't add columns by editing the model and hoping `EnsureCreated` runs. It only runs in Development and never alters an existing DB.
10. **The frontend loads whole files into memory** for hashing and preview (`api.ts:160-163`, `App.tsx:224`). Test UI changes with a >1 GB file.

**The minimum loop for a change:** a worktree from `main` → J0 fix applied → handler test with EF InMemory for logic, Testcontainers for anything involving uniqueness, locking or SQL translation → `dotnet build` of the Api project → a manual run with `docker compose up` (Development) → the PR description names the failure matrix for any multi-store change.
