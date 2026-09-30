using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class UserStorageAccount : Entity
{
    private UserStorageAccount() { }

    private UserStorageAccount(string email, string displayName, long quotaBytes)
    {
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        QuotaBytes = quotaBytes;
    }

    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public long QuotaBytes { get; private set; }
    public long UsedBytes { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;

    public static UserStorageAccount Create(string email, string displayName, long quotaBytes)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (quotaBytes <= 0) throw new DomainException("Quota must be positive.");
        return new UserStorageAccount(email, displayName, quotaBytes);
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        Touch();
    }

    public void Reserve(long bytes)
    {
        EnsureCanReserve(bytes);
        UsedBytes += bytes;
        Touch();
    }

    public void EnsureCanReserve(long bytes)
    {
        if (bytes < 0) throw new DomainException("Bytes cannot be negative.");
        if (UsedBytes > QuotaBytes || bytes > QuotaBytes - UsedBytes)
            throw new DomainException("Storage quota exceeded.");
    }

    public void Release(long bytes)
    {
        if (bytes < 0) throw new DomainException("Bytes cannot be negative.");
        UsedBytes = Math.Max(0, UsedBytes - bytes);
        Touch();
    }
}
