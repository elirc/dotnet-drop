using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class UploadChunk : Entity
{
    private UploadChunk() { }

    private UploadChunk(Guid uploadSessionId, int index, string checksum, long size)
    {
        UploadSessionId = uploadSessionId;
        Index = index;
        Checksum = checksum.Trim().ToLowerInvariant();
        Size = size;
    }

    public Guid UploadSessionId { get; private set; }
    public int Index { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public long Size { get; private set; }

    public static UploadChunk Create(Guid uploadSessionId, int index, string checksum, long size)
    {
        if (index < 0) throw new DomainException("Chunk index cannot be negative.");
        if (size < 0) throw new DomainException("Chunk size cannot be negative.");
        if (string.IsNullOrWhiteSpace(checksum)) throw new DomainException("Chunk checksum is required.");
        return new UploadChunk(uploadSessionId, index, checksum, size);
    }
}

