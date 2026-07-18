using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class ShareLink : Entity
{
    private ShareLink() { }

    private ShareLink(Guid nodeId, string token, SharePermission permission, DateTimeOffset? expiresAt, string? passwordHash)
    {
        FileNodeId = nodeId;
        Token = token;
        Permission = permission;
        ExpiresAt = expiresAt;
        PasswordHash = passwordHash;
    }

    public Guid FileNodeId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public SharePermission Permission { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool Revoked { get; private set; }
    public long AccessCount { get; private set; }
    public DateTimeOffset? LastAccessedAt { get; private set; }

    public static ShareLink Create(Guid nodeId, string token, SharePermission permission, DateTimeOffset? expiresAt, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Share token is required.");
        return new ShareLink(nodeId, token, permission, expiresAt, passwordHash);
    }

    public bool IsUsable(DateTimeOffset now) => !Revoked && (ExpiresAt is null || ExpiresAt > now);

    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Revoke()
    {
        Revoked = true;
        Touch();
    }
}

