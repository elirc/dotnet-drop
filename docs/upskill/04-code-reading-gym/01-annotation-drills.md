# Annotation Drills

For each drill, annotate: inputs, outputs, dependencies, invariants, side effects, failure modes.

## Drill 1: Complete Upload

Annotate [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86).

Checklist:

- Inputs: `UserId`, `SessionId`, cancellation token.
- Dependencies: `IAppDbContext`, `IChunkStorage`, `IBlobStorage`, `IProcessingQueue`, `IClock`.
- Invariants: all chunks present, final SHA matches, session open, quota reserve.
- Side effects: blob write, DB save, chunk delete, queue enqueue.
- Failure modes: checksum mismatch, missing chunks, crash between DB save and enqueue.

Self-grade: Basic identifies happy path. Solid identifies all side effects. Strong proposes a transaction/outbox test.

## Drill 2: Public Share Download

Annotate [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68).

Focus: token validity, password, permission level, scan status, access count. Strong answer notes that password in query string is a security concern.

## Drill 3: React Upload

Annotate [uploadOne](../../../web/src/App.tsx#L270-L290).

Focus: where memory is allocated, how parallelism is bounded, what is retried, what progress means. Strong answer notices `sha256(file)` reads the whole file before upload.

## Drill 4: EF Model

Annotate [NetDropboxDbContext.OnModelCreating](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L20-L116).

Focus: unique indexes, conversion to strings, relationships, concurrency tokens. Strong answer identifies which production queries are not well-supported by indexes.

## Drill 5: Worker Retry

Annotate [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75).

Focus: scoped services, scan result writes, retry loop, dead-letter state. Strong answer asks how operators observe dead-lettered jobs.

## Drill 6: Download Gate

Annotate [FileService.EnsureDownloadable](../../../src/NetDropbox.Application/Files/FileService.cs#L149-L155).

Focus: why each blocked status exists, and which other download paths must enforce equivalent checks.

