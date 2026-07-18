# System Map

## Project Graph

| Area | Owns | Anchors |
| --- | --- | --- |
| API | HTTP routes, middleware, JWT, rate limiting, Swagger, health checks | [Program](../../../src/NetDropbox.Api/Program.cs#L21-L96), [FilesController](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L19-L68) |
| Application | Use-case orchestration, validation, DTOs, abstractions | [StartUploadHandler](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L31-L78), [CompleteUploadHandler](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L21-L86) |
| Domain | Entities, invariants, state transitions | [FileNode](../../../src/NetDropbox.Domain/FileNode.cs#L5-L113), [UploadSession](../../../src/NetDropbox.Domain/UploadSession.cs#L5-L58) |
| Infrastructure | EF Core, storage adapters, DI, hosted workers | [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L7-L117), [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L17-L58) |
| React client | Login, browsing, upload queue, preview, query cache | [Drive](../../../web/src/App.tsx#L47-L136), [api](../../../web/src/api.ts#L84-L158) |
| Tests | Domain behavior currently only | [FileNodeTests](../../../tests/NetDropbox.Tests/Domain/FileNodeTests.cs#L8-L40) |

## Public vs Private

Public contracts are controllers and client API methods. Example: `POST /api/v1/uploads` maps to [UploadsController.Start](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L19-L21), and the frontend calls it through [api.startUpload](../../../web/src/api.ts#L130-L132). Private implementation details are domain methods like [FileNode.AddVersion](../../../src/NetDropbox.Domain/FileNode.cs#L51-L62) and storage paths in [FileSystemChunkStorage](../../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs#L52-L53).

## Boundary Rule

Controllers should be thin; business rules belong in application/domain. This is mostly followed: upload completion is one controller line [UploadsController.Complete](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L38-L40), while assembly and dedupe live in [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86). A learning exercise is to identify where [UploadsController.Status](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L23-L29) leaks query logic back into API.

