# Interview Prep From This Repo

## Runtime Questions

**Q:** Why use streams for downloads?

- Junior: to download files.
- Mid-level: avoids loading full file into memory; see [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59).
- Senior: also discuss range processing, disposal ownership, backpressure, and failure behavior.

**Q:** What does `CancellationToken` buy in uploads?

- Junior: cancels work.
- Mid-level: propagates disconnects through [UploadChunkHandler.Handle](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L29-L55).
- Senior: cancellation is resource ownership; storage writes and DB rows still need consistency cleanup.

## ASP.NET / EF / React Questions

**Q:** Defend controllers over minimal APIs here.

Use [UploadsController](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L12-L40) and comparison 2. Senior answer: controllers are fine for grouped REST resources; switch when feature-sliced endpoint modules reduce coupling.

**Q:** Why not a repository layer?

Use [FileService.ListAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L11-L31) and comparison 1. Senior answer: direct DbContext keeps query shape visible; add repositories only when they encode meaningful policy, not ceremony.

**Q:** Why React Query?

Use [Drive query](../../../web/src/App.tsx#L56-L60). Senior answer: server state needs cache keys, invalidation, and refetch semantics; hand-rolled state scales poorly.

## Debugging Questions

**Q:** Upload completed but file is never clean. Where do you look?

Junior: worker.

Mid-level: [CompleteUploadHandler enqueue](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L74-L84), [ChannelProcessingQueue](../../../src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs#L6-L14), [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75).

Senior: identify non-durable queue, crash window, missing replay, and observability gap.

## System Design Questions

**Q:** Move this from in-memory queue to durable processing.

Senior answer must include outbox, idempotency, backoff, dead-letter observability, migration, rollback, and tests around the DB-save/enqueue gap.

**Q:** How would you scale search?

Use [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134). Senior answer compares full-text, trigram, external search, and migration cost.

## Code Review Questions

**Q:** A PR removes final hash verification. What do you say?

Block it. Per-chunk checks do not prove correct full-file assembly; [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L48) protects ordering/completeness.

## Behavioral Prompts

- Tell me about a time you reduced risk without rewriting the system. Use the outbox migration plan.
- Tell me about a security review you performed. Use public share token/password/permission checks.
- Tell me about a performance tradeoff. Use browser hashing and search.

