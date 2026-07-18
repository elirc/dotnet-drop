# Side Effects, Async, And Reliability

## Side Effects

Upload completion touches database, disk/S3 blob storage, chunk cleanup, and processing queue [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L45-L84). That is the main consistency boundary.

## Queue Semantics

The queue abstraction is application-level [IProcessingQueue](../../../src/NetDropbox.Application/Abstractions/IProcessingQueue.cs#L3-L10). The implementation is an in-memory channel registered as singleton [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L24-L28). This is simple but not durable.

## Retry And Dead Letter

Worker retry happens by marking failure and immediately re-enqueuing unless dead-lettered [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L67-L75). Failure visibility exists in `ProcessingJob`, but there is no backoff schedule or admin UI.

## Cleanup

Chunk cleanup after completion happens in [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L82-L84). Janitor cleanup of expired sessions and old trash is in [JanitorWorker](../../../src/NetDropbox.Infrastructure/Processing/JanitorWorker.cs#L11-L54).
