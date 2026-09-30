using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class UploadSession : Entity
{
    private UploadSession() { }

    private UploadSession(Guid ownerId, Guid? parentId, string fileName, long totalSize, string finalContentHash, string mimeType, int chunkSize, int totalChunks)
    {
        OwnerId = ownerId;
        ParentId = parentId;
        FileName = fileName.Trim();
        TotalSize = totalSize;
        FinalContentHash = finalContentHash.Trim().ToLowerInvariant();
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType;
        ChunkSize = chunkSize;
        TotalChunks = totalChunks;
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(12);
    }

    public Guid OwnerId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long TotalSize { get; private set; }
    public string FinalContentHash { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = "application/octet-stream";
    public int ChunkSize { get; private set; }
    public int TotalChunks { get; private set; }
    public UploadSessionStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? CompletedFileNodeId { get; private set; }

    public static UploadSession Create(Guid ownerId, Guid? parentId, string fileName, long totalSize, string finalContentHash, string mimeType, int chunkSize)
    {
        if (ownerId == Guid.Empty) throw new DomainException("Upload owner is required.");
        if (string.IsNullOrWhiteSpace(fileName)) throw new DomainException("File name is required.");
        if (totalSize < 0) throw new DomainException("Total size cannot be negative.");
        if (chunkSize <= 0) throw new DomainException("Chunk size must be positive.");
        if (!IsSha256(finalContentHash)) throw new DomainException("Final content hash must be a 64-character hexadecimal SHA-256 value.");

        var requiredChunks = totalSize == 0 ? 1 : ((totalSize - 1) / chunkSize) + 1;
        if (requiredChunks > int.MaxValue) throw new DomainException("Upload requires too many chunks.");
        var chunks = (int)requiredChunks;
        return new UploadSession(ownerId, parentId, fileName, totalSize, finalContentHash, mimeType, chunkSize, chunks);
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now || Status == UploadSessionStatus.Expired;

    public void Complete(Guid fileNodeId)
    {
        EnsureOpen();
        if (fileNodeId == Guid.Empty) throw new DomainException("Completed file is required.");
        Status = UploadSessionStatus.Completed;
        CompletedFileNodeId = fileNodeId;
        Touch();
    }

    public void Expire()
    {
        EnsureOpen();
        Status = UploadSessionStatus.Expired;
        Touch();
    }

    public long ExpectedChunkSize(int index)
    {
        if (index < 0 || index >= TotalChunks) throw new DomainException("Chunk index is outside the session range.");
        if (TotalSize == 0) return 0;

        var offset = (long)index * ChunkSize;
        return Math.Min(ChunkSize, TotalSize - offset);
    }

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private void EnsureOpen()
    {
        if (Status != UploadSessionStatus.Open) throw new DomainException("Upload session is no longer open.");
    }
}
