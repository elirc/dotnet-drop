# Domain Glossary

| Term | Meaning in this repo | Where it appears |
| --- | --- | --- |
| FileNode | Folder or file record, owned by a user, optionally under a parent folder. | [FileNode properties](../../../src/NetDropbox.Domain/FileNode.cs#L20-L30), [DbSet](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L9-L18) |
| FileVersion | Immutable version record attached to a file node. | [FileNode.AddVersion](../../../src/NetDropbox.Domain/FileNode.cs#L51-L62) |
| Blob | Content-addressed stored bytes, deduped by hash. | [CompleteUploadHandler blob lookup](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L50-L61), [blob index](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L59-L65) |
| UploadSession | Resumable upload contract: owner, file name, chunk size, expected hash, expiry. | [UploadSession](../../../src/NetDropbox.Domain/UploadSession.cs#L22-L32) |
| UploadChunk | Server-side record of a received chunk index and checksum. | [UploadChunkHandler](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L40-L55), [chunk index](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L77-L82) |
| ShareLink | Token-authenticated public access with permission, expiry, optional password, analytics. | [ShareService.CreateLinkAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L11-L20), [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68) |
| DirectShare | User-to-user grant by email and role. | [ShareService.DirectShareAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L33-L40), [DirectShare index](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L93-L98) |
| ScanResult | Audit record for clean/infected scan result. | [ProcessingWorker scan result writes](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L47-L65) |
| ProcessingJob | Durable-ish DB state for a processing attempt, but queued in memory. | [ProcessingWorker job updates](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L39-L75) |

