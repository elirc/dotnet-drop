# Boundaries And Layers

| Layer | Owns | Must not own | Example |
| --- | --- | --- | --- |
| API | HTTP shape, auth attributes, streaming response | Business decisions | [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59) |
| Application | Use-case orchestration, validation, transaction-ish sequencing | Storage implementation details | [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86) |
| Domain | Invariants and state transitions | EF queries or HTTP status codes | [FileNode.AddVersion](../../../src/NetDropbox.Domain/FileNode.cs#L51-L62) |
| Infrastructure | EF mapping, storage, workers, DI | User-facing policy | [NetDropboxDbContext.OnModelCreating](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L20-L116) |
| Client | UI state, browser hashing, previews | Server authorization | [uploadOne](../../../web/src/App.tsx#L270-L290) |

Boundary leak to investigate: [UploadsController.Status](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L23-L29) directly queries `IAppDbContext`. It is small, but it teaches a pattern that can grow into business logic in controllers.

