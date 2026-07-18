# Fast Track

Goal: get one weekend win without pretending you understand the whole system.

## Verified Setup Commands

| Command | Status | Notes |
| --- | --- | --- |
| `& "$HOME\.dotnet\dotnet.exe" build src/NetDropbox.Api/NetDropbox.Api.csproj --no-restore` | Verified pass | Full output in [verification-log](08-reference/verification-log.md). |
| `& "$HOME\.dotnet\dotnet.exe" test tests/NetDropbox.Tests/NetDropbox.Tests.csproj --no-restore` | Verified pass | 6 tests passed. |
| `cd web; npm run build` | Verified pass | Vite production build passed. |
| `docker compose up --build` | Inferred | Documented in [README](../../README.md#L25-L43), but not run during this pass. |

## First 10 Files

1. [Program](../../src/NetDropbox.Api/Program.cs#L21-L96) - middleware, auth, rate limiting, Swagger, health checks.
2. [UploadsController](../../src/NetDropbox.Api/Controllers/UploadsController.cs#L19-L40) - HTTP shape of upload sessions.
3. [StartUploadHandler](../../src/NetDropbox.Application/Uploads/StartUpload.cs#L31-L78) - quota, parent folder check, idempotent session.
4. [UploadChunkHandler](../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L27-L56) - chunk ownership and checksum gate.
5. [CompleteUploadHandler.Handle](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86) - assembly, dedupe, version, queue.
6. [FileNode](../../src/NetDropbox.Domain/FileNode.cs#L5-L113) - core aggregate and invariants.
7. [NetDropboxDbContext.OnModelCreating](../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L20-L116) - schema, indexes, relationships.
8. [ProcessingWorker.ProcessAsync](../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75) - scan pipeline and retry behavior.
9. [App uploadOne](../../web/src/App.tsx#L270-L290) - client hashing and parallel chunk upload.
10. [api request](../../web/src/api.ts#L67-L82) - token headers and problem response handling.

## Trace These Two Flows

1. Upload a file: [uploadOne](../../web/src/App.tsx#L270-L290) -> [UploadsController](../../src/NetDropbox.Api/Controllers/UploadsController.cs#L19-L40) -> [CompleteUploadHandler](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86) -> [ProcessingWorker](../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75).
2. Download a clean file: [FilesController.Download](../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59) -> [FileService.OpenDownloadAsync](../../src/NetDropbox.Application/Files/FileService.cs#L114-L122) -> [FileService.EnsureDownloadable](../../src/NetDropbox.Application/Files/FileService.cs#L149-L155).

## One Safe Change

Add a domain unit test for `FileNode.RestoreVersion` using the style in [FileNodeTests](../../tests/NetDropbox.Tests/Domain/FileNodeTests.cs#L8-L40). It is safe because it touches tests only and exercises an existing invariant in [FileNode.RestoreVersion](../../src/NetDropbox.Domain/FileNode.cs#L64-L70).

## Weekend Teach-Back

Explain, without notes:

- Why upload start checks quota before bytes are accepted [StartUploadHandler.Handle](../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L48).
- Why final hash verification still matters after per-chunk checks [CompleteUploadHandler.Handle](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L48).
- Why downloads are blocked until scan status is clean [FileService.EnsureDownloadable](../../src/NetDropbox.Application/Files/FileService.cs#L149-L155).

## What This Skips

This fast path skips schema migration discipline, integration tests, tenant isolation beyond owner filters, production queue durability, frontend memory cleanup, and Docker runtime verification.

