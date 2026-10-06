# Map the upload path and its state owners

UploadSession in the Domain project owns the upload declaration: owner, filename, total bytes, final digest, chunk size, count, expiry, and lifecycle status. Its ExpectedChunkSize method defines the byte boundary for an index. For ten bytes split into chunks of four, indexes zero, one, and two require four, four, and two bytes respectively. The object rejects negative or excessive indexes and prevents a completed session from transitioning again. The handler additionally checks expiry against its clock before accepting work.

UploadChunkHandler in Application loads the session using both session ID and user ID. It checks the current state, index, and any existing accepted metadata before invoking IChunkStorage. FileSystemChunkStorage implements that port in Infrastructure. It validates a temporary file before attempting publication. The database UploadChunk row records the accepted checksum and size; the handler creates it only after the storage result says the content was committed. Different bytes at an already accepted index are a conflict, not an update operation.

CompleteUploadHandler first checks that every required index exists. UserStorageAccount performs a pure quota preflight. IChunkStorage then assembles the accepted chunks in order into a unique temporary stream. Hashing.Sha256WithLengthAsync returns both the digest and observed length. Only after these checks does completion find or create a content-addressed blob, find or create the file node, add a version and processing job, reserve quota, and complete the session. SaveChanges persists that graph. Chunk cleanup and queue notification follow it.

NetDropboxDbContext maps those domain objects. The FileNode.Versions relationship now uses the public navigation with field access, avoiding two mappings to the same backing field. The tests live under NetDropbox.Tests: domain cases validate arithmetic and lifecycle, filesystem cases use real temporary directories, and upload handler cases reopen an InMemory database to inspect committed state. Trace ownership before changing code: a digest helper cannot authorize a user, a filesystem rename cannot commit database rows, and a queue acknowledgement cannot undo an already saved file version. Each layer contributes a specific guarantee.

## Line map (current `main`)

| Rule | Owner | Where |
| --- | --- | --- |
| hash must be 64 hex chars; chunk count rounds up without overflow; zero bytes = 1 chunk | `UploadSession.Create` | [`UploadSession.cs:34-46`](../src/NetDropbox.Domain/UploadSession.cs#L34-L46) |
| per-index expected length (last chunk = remainder) | `UploadSession.ExpectedChunkSize` | [`UploadSession.cs:66-73`](../src/NetDropbox.Domain/UploadSession.cs#L66-L73) |
| session lookup is owner-scoped; open + not expired; index in range | `UploadChunkHandler` | [`UploadChunk.cs:31-38`](../src/NetDropbox.Application/Uploads/UploadChunk.cs#L31-L38) |
| different checksum at an accepted index → 409 *before* storage | `UploadChunkHandler` | [`UploadChunk.cs:40-47`](../src/NetDropbox.Application/Uploads/UploadChunk.cs#L40-L47) |
| storage conflict → 409; not committed → 400 with size-vs-checksum reason | `UploadChunkHandler` | [`UploadChunk.cs:49-70`](../src/NetDropbox.Application/Uploads/UploadChunk.cs#L49-L70) |
| chunk row written only after storage committed, only if absent | `UploadChunkHandler` | [`UploadChunk.cs:72-76`](../src/NetDropbox.Application/Uploads/UploadChunk.cs#L72-L76) |
| exact index set `0..TotalChunks-1` | `CompleteUploadHandler` | [`CompleteUpload.cs:36-43`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L36-L43) |
| read-only quota preflight | `UserStorageAccount.EnsureCanReserve` | [`CompleteUpload.cs:45-46`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L45-L46), [`UserStorageAccount.cs:42-47`](../src/NetDropbox.Domain/UserStorageAccount.cs#L42-L47) |
| assembled length, then digest | `CompleteUploadHandler` + `Hashing` | [`CompleteUpload.cs:48-53`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L48-L53), [`Hashing.cs:7`](../src/NetDropbox.Application/Common/Hashing.cs#L7) |
| blob find-or-create (only external write) | `CompleteUploadHandler` | [`CompleteUpload.cs:55-66`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L55-L66) |
| file node, version, scan job, reserve, complete, then **one** save | `CompleteUploadHandler` | [`CompleteUpload.cs:68-85`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L68-L85) |
| after the save: delete chunks, enqueue (in-memory channel) | `CompleteUploadHandler` | [`CompleteUpload.cs:86-87`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L86-L87) |
| one row per `(UploadSessionId, Index)` | database | [`NetDropboxDbContext.cs:80`](../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L80) |
| temp-file publish, non-overwriting move, conflict on different bytes | `FileSystemChunkStorage` | **not in git**, see the status note in [README](README.md) |

**Exercise.** *Goal:* find where a concurrency guarantee actually comes from. Two first
requests for the same index, with *identical* bytes, both pass line 43 (`existing` is null),
both get `Committed = true` from storage, and both reach line 74. What stops a second
`UploadChunk` row? **Check:** the unique index at `NetDropboxDbContext.cs:80`. The loser's
`SaveChangesAsync` (line 75) throws a `DbUpdateException`, which the API returns as a 500
because `ExceptionMiddleware` only maps `AppException` to its own status. So the *data* is
safe and the *response* is wrong. Chapter 4 explains why the InMemory handler tests can't
show you this.

## Source excerpt

From [src/NetDropbox.Application/Abstractions/IChunkStorage.cs](../src/NetDropbox.Application/Abstractions/IChunkStorage.cs).

```cs
using NetDropbox.Domain;

namespace NetDropbox.Application.Abstractions;

public sealed record StoredChunk(string Checksum, long Size, bool Committed);

public sealed class ChunkWriteConflictException(string message) : Exception(message);

public interface IChunkStorage
{
    Task<StoredChunk> StoreAsync(
        Guid sessionId,
        int index,
        Stream content,
        string expectedChecksum,
        long expectedSize,
        CancellationToken cancellationToken);
    Task<Stream> OpenAssembledAsync(UploadSession session, IReadOnlyCollection<int> chunkIndexes, CancellationToken cancellationToken);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)
