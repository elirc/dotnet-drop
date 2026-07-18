# Mid-Level Feature Tickets

## Ticket 1: Durable Processing Outbox

**Difficulty:** Hard **Skills:** EF Core, hosted workers, idempotency.

**Story:** As an operator, I want upload processing to survive API restarts.

**Read first:** [CompleteUploadHandler enqueue](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L74-L84), [ChannelProcessingQueue](../../../src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs#L6-L14), [ProcessingWorker](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75).

Acceptance: persisted outbox row, worker drains rows, duplicate delivery safe, tests for crash window.

## Ticket 2: Full-Text Search

Read first: [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134), [NetDropboxDbContext FileNode mapping](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L35-L49). Add PostgreSQL search strategy with migration plan and benchmark notes.

## Ticket 3: Share Password Exchange

Read first: [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68). Replace query-string password with short-lived access token exchange.

## Ticket 4: Upload Quota Reservation

Read first: [StartUploadHandler quota check](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L40), [CompleteUploadHandler reserve](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L79-L80). Design reservation to avoid concurrent oversubscription.

## Ticket 5: API Integration Test Harness

Read first: [Program](../../../src/NetDropbox.Api/Program.cs#L21-L96), [CI tests](../../../.github/workflows/ci.yml#L16-L18). Add WebApplicationFactory/Testcontainers style tests.

