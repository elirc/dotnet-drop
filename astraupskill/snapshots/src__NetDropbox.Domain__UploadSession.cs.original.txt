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
        if (string.IsNullOrWhiteSpace(fileName)) throw new DomainException("File name is required.");
        if (totalSize < 0) throw new DomainException("Total size cannot be negative.");
        if (chunkSize <= 0) throw new DomainException("Chunk size must be positive.");
        if (string.IsNullOrWhiteSpace(finalContentHash)) throw new DomainException("Final content hash is required.");
        var chunks = Math.Max(1, (int)Math.Ceiling(totalSize / (double)chunkSize));
        return new UploadSession(ownerId, parentId, fileName, totalSize, finalContentHash, mimeType, chunkSize, chunks);
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now || Status == UploadSessionStatus.Expired;

    public void Complete(Guid fileNodeId)
    {
        Status = UploadSessionStatus.Completed;
        CompletedFileNodeId = fileNodeId;
        Touch();
    }

    public void Expire()
    {
        Status = UploadSessionStatus.Expired;
        Touch();
    }
}

