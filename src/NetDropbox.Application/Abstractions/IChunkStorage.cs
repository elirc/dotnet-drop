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
