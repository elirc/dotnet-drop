# NetDropbox flashcards (25)

> **Spoilers:** cards 1–3 and 12–19 give away review findings, and cards 7–11 give away incident causes. Use this deck only after the review and all five incidents.

Cover the right-hand column.

| # | Front | Back |
|---|---|---|
| 1 | Why doesn't `main` compile from a clean clone? | `.gitignore:8` `storage/` matched `Infrastructure/Storage/` on a case-insensitive FS; the adapters were never committed. |
| 2 | EF model error on `main`? | `HasMany<FileVersion>("_versions")` plus `Navigation(x => x.Versions)` claim the same backing field. |
| 3 | Is `Entity.Version` protecting anything? | No. A plain bigint that's never written, so `WHERE Version=0` always matches. Use `IsRowVersion()`/xmin. |
| 4 | The line that decides whether a file is ever scanned? | `CompleteUpload.cs:84` `queue.EnqueueAsync`, after commit, into an in-memory Channel. |
| 5 | Why is `processing_jobs` "write-only"? | Nothing reads it back to dispatch or recover work; the worker reads only the Channel. |
| 6 | What stops the host when the worker throws? | .NET 8 `BackgroundServiceExceptionBehavior.StopHost` (the default). The lookups at `ProcessingWorker.cs:32-34` are outside the try. |
| 7 | INC-001 in one line | The quota reservation moved into an idempotent handler, so every resume re-charged; nothing ever releases. |
| 8 | INC-002 in one line | `Task.Delay` backoff inside a single serial consumer, so poison HEIC files block the queue. |
| 9 | INC-003 in one line | The DirectShares `Any()` was missing `SharedWithUserId == userId`, so any shared file was downloadable by anyone. |
| 10 | INC-004 in one line | Projection → entity loop: N+1 queries plus loading 1 MB `extracted_text` per row. |
| 11 | INC-005 in one line | Renamed config section, env var no longer read, silent fallback to the committed dev key, forgeable JWTs. |
| 12 | Two quarantine bypasses | `CopyAsync` → `MarkScanClean` unconditionally; `download?versionId=` checks only the node's status. |
| 13 | Why does restore-version brick a file? | `RestoreVersion` sets PendingScan but no scan job is queued. |
| 14 | Who calls `UserStorageAccount.Release`? | Nobody. `used_bytes` only grows. |
| 15 | Who calls `BlobObject.RemoveReference`? | Nobody. Refcounts never drop and blobs are never GC'd. |
| 16 | Number of FKs in the EF model | One: file_versions → file_nodes. |
| 17 | Why can a folder be moved into itself? | `FileNode.Move` guards only `Type == File`; there's no ancestor check. |
| 18 | The HEAD chunk-upload integrity bug | The chunk is written to its final path *before* the checksum compare, so a bad retry overwrites an accepted chunk. |
| 19 | Why does a public link survive trash? | `OpenPublicDownloadAsync` loads the node without `!IsDeleted`. |
| 20 | Why is login brute-forceable and enumerable? | No rate limit on `/auth/login`; an unknown email returns before PBKDF2 runs (a timing gap). |
| 21 | Three replicas: what fails first? | Chunks on local disk: complete on another replica → `FileNotFoundException`. |
| 22 | Search at scale: which index for names and for content? | `pg_trgm` GIN on `lower(name)`; `tsvector` GIN for extracted text. |
| 23 | Keyset cursor for folder listing? | `(type, lower(name), id)` > last seen; opaque base64. |
| 24 | Safe way to decrement a refcount here? | Atomic SQL `SET ref_count = ref_count - 1`, and GC by mark → grace → delete. |
| 25 | Where do Redis and RabbitMQ get used? | Nowhere. Provisioned in `docker-compose.yml` only (an AI-generated smell). |
