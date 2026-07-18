# .NET Runtime Model

## Async And Threads

Most backend IO is async: EF queries in [StartUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L78), stream reads/writes in [FileSystemChunkStorage.StoreAsync](../../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs#L9-L27), and worker loops in [ProcessingWorker.ExecuteAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L18-L24). `async` does not create a new thread; it lets the request thread return to the pool while waiting for IO. Failure mode: blocking on `.Result` or `.Wait()` can starve the pool and hide cancellation. No sync-over-async was found in opened files.

## CancellationToken

The token is threaded from controllers into handlers and storage, for example [UploadsController.UploadChunk](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L31-L35) -> [UploadChunkHandler.Handle](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L29-L55) -> [FileSystemChunkStorage.StoreAsync](../../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs#L19-L21). Seniors care because cancellation defines who pays when clients disconnect during large uploads.

## Streams, Allocation, And GC

Downloads return streams instead of buffering bytes [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59). Chunk storage copies using a 128KB buffer [FileSystemChunkStorage.StoreAsync](../../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs#L14-L26). The frontend whole-file hash uses `blob.arrayBuffer()` [sha256](../../../web/src/api.ts#L160-L164), which can allocate the whole file in browser memory; that is acceptable for a lab, risky for multi-GB files.

## IDisposable And `await using`

The upload completion path disposes assembled streams with `await using` [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L45-L50). Worker image processing disposes streams and images [ProcessingWorker.GenerateThumbnailAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L94-L106). Drill: identify every stream opened in the download path and explain who disposes it.

