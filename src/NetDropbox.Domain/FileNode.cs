using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class FileNode : Entity
{
    private readonly List<FileVersion> _versions = [];

    private FileNode() { }

    private FileNode(Guid ownerId, Guid? parentId, string name, FileNodeType type)
    {
        OwnerId = ownerId;
        ParentId = parentId;
        Name = CleanName(name);
        Type = type;
        ScanStatus = type == FileNodeType.Folder ? FileScanStatus.Clean : FileScanStatus.PendingScan;
    }

    public Guid OwnerId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public FileNodeType Type { get; private set; }
    public Guid? CurrentVersionId { get; private set; }
    public FileScanStatus ScanStatus { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public string? ThumbnailPath { get; private set; }
    public string? ExtractedText { get; private set; }
    public IReadOnlyCollection<FileVersion> Versions => _versions;

    public static FileNode CreateFolder(Guid ownerId, Guid? parentId, string name) =>
        new(ownerId, parentId, name, FileNodeType.Folder);

    public static FileNode CreateFile(Guid ownerId, Guid? parentId, string name) =>
        new(ownerId, parentId, name, FileNodeType.File);

    public void Rename(string name)
    {
        Name = CleanName(name);
        Touch();
    }

    public void Move(Guid? parentId)
    {
        if (Type == FileNodeType.File && parentId == Id) throw new DomainException("A file cannot contain itself.");
        ParentId = parentId;
        Touch();
    }

    public FileVersion AddVersion(Guid blobId, long size, string mimeType, string contentHash, Guid uploaderId)
    {
        if (Type != FileNodeType.File) throw new DomainException("Folders cannot have file versions.");

        var next = _versions.Count == 0 ? 1 : _versions.Max(v => v.VersionNumber) + 1;
        var version = FileVersion.Create(Id, blobId, next, size, mimeType, contentHash, uploaderId);
        _versions.Add(version);
        CurrentVersionId = version.Id;
        ScanStatus = FileScanStatus.PendingScan;
        Touch();
        return version;
    }

    public void RestoreVersion(Guid versionId)
    {
        if (_versions.All(v => v.Id != versionId)) throw new DomainException("Version does not belong to this file.");
        CurrentVersionId = versionId;
        ScanStatus = FileScanStatus.PendingScan;
        Touch();
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RestoreFromTrash()
    {
        IsDeleted = false;
        DeletedAt = null;
        Touch();
    }

    public void MarkScanClean(string? extractedText, string? thumbnailPath)
    {
        ScanStatus = FileScanStatus.Clean;
        ExtractedText = extractedText;
        ThumbnailPath = thumbnailPath;
        Touch();
    }

    public void MarkInfected()
    {
        ScanStatus = FileScanStatus.Infected;
        Touch();
    }

    public void MarkScanFailed()
    {
        ScanStatus = FileScanStatus.Failed;
        Touch();
    }

    private static string CleanName(string name)
    {
        var cleaned = name.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) throw new DomainException("Name is required.");
        if (cleaned.Contains('/') || cleaned.Contains('\\')) throw new DomainException("Name cannot contain path separators.");
        return cleaned;
    }
}

