# Map the upload path and its state owners

UploadSession in the Domain project owns the upload declaration: owner, filename, total bytes, final digest, chunk size, count, expiry, and lifecycle status. Its ExpectedChunkSize method defines the byte boundary for an index. For ten bytes split into chunks of four, indexes zero, one, and two require four, four, and two bytes respectively. The object rejects negative or excessive indexes and prevents a completed session from transitioning again. The handler additionally checks expiry against its clock before accepting work.

UploadChunkHandler in Application loads the session using both session ID and user ID. It checks the current state, index, and any existing accepted metadata before invoking IChunkStorage. FileSystemChunkStorage implements that port in Infrastructure. It validates a temporary file before attempting publication. The database UploadChunk row records the accepted checksum and size; the handler creates it only after the storage result says the content was committed. Different bytes at an already accepted index are a conflict, not an update operation.

CompleteUploadHandler first checks that every required index exists. UserStorageAccount performs a pure quota preflight. IChunkStorage then assembles the accepted chunks in order into a unique temporary stream. Hashing.Sha256WithLengthAsync returns both the digest and observed length. Only after these checks does completion find or create a content-addressed blob, find or create the file node, add a version and processing job, reserve quota, and complete the session. SaveChanges persists that graph. Chunk cleanup and queue notification follow it.

NetDropboxDbContext maps those domain objects. The FileNode.Versions relationship now uses the public navigation with field access, avoiding two mappings to the same backing field. The tests live under NetDropbox.Tests: domain cases validate arithmetic and lifecycle, filesystem cases use real temporary directories, and upload handler cases reopen an InMemory database to inspect committed state. Trace ownership before changing code: a digest helper cannot authorize a user, a filesystem rename cannot commit database rows, and a queue acknowledgement cannot undo an already saved file version. Each layer contributes a specific guarantee.

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
