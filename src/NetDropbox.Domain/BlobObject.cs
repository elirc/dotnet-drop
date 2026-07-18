using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class BlobObject : Entity
{
    private BlobObject() { }

    private BlobObject(string contentHash, long size, string storageKey)
    {
        ContentHash = contentHash;
        Size = size;
        StorageKey = storageKey;
        RefCount = 1;
    }

    public string ContentHash { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public int RefCount { get; private set; }

    public static BlobObject Create(string contentHash, long size, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(contentHash)) throw new DomainException("Content hash is required.");
        if (size < 0) throw new DomainException("Blob size cannot be negative.");
        return new BlobObject(contentHash, size, storageKey);
    }

    public void AddReference()
    {
        RefCount++;
        Touch();
    }

    public void RemoveReference()
    {
        if (RefCount == 0) return;
        RefCount--;
        Touch();
    }
}

