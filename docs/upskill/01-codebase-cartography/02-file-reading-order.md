# File Reading Order

## Junior Path

| Order | File | What to look for |
| --- | --- | --- |
| 1 | [README](../../../README.md#L25-L43) | How maintainers expect the app to run. |
| 2 | [Program](../../../src/NetDropbox.Api/Program.cs#L21-L96) | Request pipeline order: exceptions before auth, auth before controllers. |
| 3 | [FilesController](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L19-L68) | Route names and `User.UserId()` use. |
| 4 | [FileService](../../../src/NetDropbox.Application/Files/FileService.cs#L11-L155) | Owner filters, scan checks, and projection style. |
| 5 | [FileNode](../../../src/NetDropbox.Domain/FileNode.cs#L5-L113) | Invariants: names, versions, trash, scan status. |
| 6 | [App](../../../web/src/App.tsx#L47-L136) | State for current view, parent folder, search, selected preview. |
| 7 | [api](../../../web/src/api.ts#L67-L82) | Token and error handling. |

## Mid-Level Path

| Order | File | What to look for |
| --- | --- | --- |
| 1 | [StartUploadHandler](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L31-L78) | Idempotency and quota boundary. |
| 2 | [UploadChunkHandler](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L27-L56) | Per-chunk checksum and owner/session checks. |
| 3 | [CompleteUploadHandler](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86) | Consistency risks across DB, blob storage, and queue. |
| 4 | [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L20-L116) | Indexes, unique constraints, and concurrency tokens. |
| 5 | [ProcessingWorker](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75) | Retry and dead-letter semantics. |
| 6 | [FileSystemChunkStorage](../../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs#L9-L49) | Streaming IO and cleanup. |

## Senior Path

| Order | File | What to challenge |
| --- | --- | --- |
| 1 | [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L17-L58) | Lifetimes, storage selection, queue durability. |
| 2 | [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68) | Token auth path, password checks, analytics write. |
| 3 | [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134) | Text search scalability and index strategy. |
| 4 | [NetDropboxDbContext indexes](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L35-L111) | Which query has an index and which does not. |
| 5 | [CI](../../../.github/workflows/ci.yml#L8-L32) | What build catches and what integration gaps remain. |

