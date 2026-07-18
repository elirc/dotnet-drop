using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class FileVersion : Entity
{
    private FileVersion() { }

    private FileVersion(Guid fileNodeId, Guid blobId, int versionNumber, long size, string mimeType, string contentHash, Guid uploaderId)
    {
        FileNodeId = fileNodeId;
        BlobId = blobId;
        VersionNumber = versionNumber;
        Size = size;
        MimeType = mimeType;
        ContentHash = contentHash;
        UploaderId = uploaderId;
    }

    public Guid FileNodeId { get; private set; }
    public Guid BlobId { get; private set; }
    public int VersionNumber { get; private set; }
    public long Size { get; private set; }
    public string MimeType { get; private set; } = "application/octet-stream";
    public string ContentHash { get; private set; } = string.Empty;
    public Guid UploaderId { get; private set; }

    public static FileVersion Create(Guid fileNodeId, Guid blobId, int versionNumber, long size, string mimeType, string contentHash, Guid uploaderId)
    {
        if (versionNumber <= 0) throw new DomainException("Version number must be positive.");
        if (size < 0) throw new DomainException("Version size cannot be negative.");
        return new FileVersion(fileNodeId, blobId, versionNumber, size, string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType, contentHash, uploaderId);
    }
}

