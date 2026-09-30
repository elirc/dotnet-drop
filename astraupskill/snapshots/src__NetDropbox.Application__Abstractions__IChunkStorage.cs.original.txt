using NetDropbox.Domain;

namespace NetDropbox.Application.Abstractions;

public sealed record StoredChunk(string Checksum, long Size);

public interface IChunkStorage
{
    Task<StoredChunk> StoreAsync(Guid sessionId, int index, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenAssembledAsync(UploadSession session, IReadOnlyCollection<int> chunkIndexes, CancellationToken cancellationToken);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}

