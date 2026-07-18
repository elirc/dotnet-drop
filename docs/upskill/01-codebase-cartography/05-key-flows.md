# Key Flows

## Flow: Chunked Upload To Scan Queue

**Why it matters:** This is the core product promise: large uploads must be resumable, verified, deduped, versioned, and scan-gated.

**Open first:** [uploadOne](../../../web/src/App.tsx#L270-L290) - it shows the client-side contract.

| Step | Owner | File | What happens | Data shape | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | React | [uploadOne](../../../web/src/App.tsx#L270-L290) | Whole-file hash, session start, missing chunks, parallel send, complete. | `File`, `UploadSession`, chunk indexes | Hashing whole file loads it into memory. |
| 2 | API | [UploadsController.Start](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L19-L21) | Converts JWT user into command. | JSON request -> command | Thin controller; auth depends on JWT claim. |
| 3 | App | [StartUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L78) | Checks quota and parent folder, reuses open session. | User, folder, session | Quota reservation happens later, so concurrent starts can oversubscribe. |
| 4 | App/Infra | [UploadChunkHandler.Handle](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L31-L55) | Verifies session owner, expiry, index, checksum. | Chunk stream + SHA-256 | No transactional relation between disk chunk write and DB chunk row. |
| 5 | App | [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L84) | Assembles, verifies final hash, dedupes blob, creates version, enqueues scan. | Blob, FileNode, FileVersion, ProcessingJob | DB commit and queue enqueue are not atomic. |

**Validation & authorization:** [StartUploadValidator](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L20-L28), [UploadChunkValidator](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L17-L24), [UploadsController Authorize](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L12-L15).

**Persistence & side effects:** [NetDropboxDbContext upload indexes](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L67-L82), [FileSystemChunkStorage](../../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs#L9-L49).

**Tests:** No direct application upload tests found. Domain chunk count is covered by [UploadSessionTests](../../../tests/NetDropbox.Tests/Domain/UploadSessionTests.cs#L8-L24).

**Juniors miss:** per-chunk checksum is not enough; final assembled hash still protects ordering and missing data.

**Seniors notice:** the queue is in-memory, so process crash after `SaveChangesAsync` but before worker handles the message leaves a pending file without guaranteed replay.

**Drill:** Draw a table with file size `10MB`, chunk size `4MB`, and an interrupted upload after chunk `1`. Show which endpoint tells the client chunk `0` and `1` are already present.

**Self-grade:** Basic: names endpoints. Solid: explains idempotency. Strong: identifies atomicity and quota race risks.

## Flow: Clean File Download

**Why it matters:** Download is the security gate where scan status, ownership, streaming, and MIME type meet.

**Open first:** [FileService.OpenDownloadAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L114-L122).

| Step | Owner | File | What happens | Data shape | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | API | [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59) | Calls service and returns streaming file result. | `Stream`, file name, MIME | Correct: stream, not byte array. |
| 2 | App | [FileService.OpenDownloadAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L114-L122) | Loads owner-scoped node and version. | FileNode + Versions | Include can grow if many versions. |
| 3 | App | [FileService.EnsureDownloadable](../../../src/NetDropbox.Application/Files/FileService.cs#L149-L155) | Blocks folder, pending, infected, failed. | Scan status enum | Error paths need API tests. |

**Tests:** No direct download test found.

**Drill:** Write the missing test names for pending, infected, failed, and clean cases before writing any code.

## Flow: Share Link Download

**Why it matters:** Share links bypass normal user JWT auth and must be treated as their own auth path.

**Open first:** [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68).

| Step | Owner | File | What happens | Data shape | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | API | [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17) | Anonymous token route with public rate limit. | Token + password query | Password in query string can leak into logs. |
| 2 | App | [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L55-L68) | Checks token, expiry, password, permission, scan status, records access. | ShareLink -> Blob stream | Analytics write and stream open are coupled. |
| 3 | API setup | [Program rate limiter](../../../src/NetDropbox.Api/Program.cs#L60-L70) | Fixed window public limit. | IP partition | Behind proxies this needs forwarded headers. |

**Tests:** No share-link tests found.

**Seniors notice:** token auth needs the same rigor as JWT auth: expiry, revocation, permission, scan status, and audit semantics.

## Flow: React Query Browse And Preview

**Why it matters:** The client shows how server state is cached and invalidated after mutations.

**Open first:** [Drive](../../../web/src/App.tsx#L47-L136).

| Step | Owner | File | What happens | Data shape | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | React | [Drive query](../../../web/src/App.tsx#L56-L60) | Query key includes view, parent, search. | `FileNode[]` | Query key does not include auth user. |
| 2 | React | [refresh](../../../web/src/App.tsx#L62-L65) | Invalidates files and usage after upload. | Query cache | Broad invalidation is simple but less targeted. |
| 3 | React | [PreviewPanel effect](../../../web/src/App.tsx#L221-L238) | Fetches blob, creates object URL, chooses preview type. | Blob URL/text | Object URLs are not revoked on panel changes. |

**Drill:** Explain why `queryKey: ['files', view, parentId, query]` is better than `['files']`, then identify what is still missing for multi-user browser sessions.

## Flow: Background Scan Pipeline

**Why it matters:** Upload success does not equal file availability; background work controls file status.

**Open first:** [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75).

| Step | Owner | File | What happens | Data shape | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | App | [CompleteUploadHandler enqueue](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L74-L84) | Creates DB job and enqueues message. | ProcessingJob + message | No outbox. |
| 2 | Worker | [ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L32-L65) | Loads job/node/version/blob, scans sample, writes result. | Blob stream + FileNode | Only first 1MB sampled as text. |
| 3 | Worker | [catch retry](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L67-L75) | Marks failed/dead-lettered and requeues. | Attempts/status | Immediate retry can loop hot. |

**Tests:** No worker tests found.

**Drill:** Design the smallest integration test that proves infected files cannot be downloaded.

