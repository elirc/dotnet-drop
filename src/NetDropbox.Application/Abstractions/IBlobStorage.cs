namespace NetDropbox.Application.Abstractions;

public interface IBlobStorage
{
    Task<bool> ExistsAsync(string contentHash, CancellationToken cancellationToken);
    Task<string> SaveAsync(string contentHash, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

