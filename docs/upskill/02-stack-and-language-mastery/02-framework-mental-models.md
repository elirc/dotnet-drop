# Framework Mental Models

## ASP.NET Core Pipeline

`Program.cs` is ordered deliberately: custom correlation and exception middleware first [Program](../../../src/NetDropbox.Api/Program.cs#L78-L80), request logging and CORS next [Program](../../../src/NetDropbox.Api/Program.cs#L88-L90), then authentication/authorization before controller mapping [Program](../../../src/NetDropbox.Api/Program.cs#L91-L94). Pipeline order is a runtime contract. Put authorization after controllers and every controller becomes accidentally public.

## Dependency Injection Lifetimes

EF `DbContext` is scoped [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L22-L23). The processing queue is singleton [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L24-L28). Workers create scopes before using scoped services [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L30), avoiding a captive dependency bug. Drill: explain why injecting `NetDropboxDbContext` directly into `ProcessingWorker` would be wrong.

## Model Binding And Validation

Controllers bind request records, then MediatR sends commands [UploadsController.Start](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L17-L21). FluentValidation validates commands [StartUploadValidator](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L20-L28). Domain invariants still exist in [FileNode.CleanName](../../../src/NetDropbox.Domain/FileNode.cs#L106-L112). Senior lesson: request validation is user feedback; domain invariants are correctness.

## EF Core Mental Model

`DbSet`s represent aggregate tables [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L9-L18). LINQ remains `IQueryable` until `ToArrayAsync` or similar [FileService.ListAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L13-L31). `Include` eagerly loads versions for download [FileService.OpenDownloadAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L114-L122). Failure mode: moving from `IQueryable` to `IEnumerable` too early pulls data into memory and bypasses database filtering.

## React Render Model

`Drive` stores UI state in hooks [Drive](../../../web/src/App.tsx#L47-L60). Query state is declarative via TanStack Query [Drive query](../../../web/src/App.tsx#L56-L60). The preview side effect runs after render [PreviewPanel effect](../../../web/src/App.tsx#L221-L238). Failure mode: effects that create object URLs need cleanup; this one does not revoke preview URLs when node changes.

