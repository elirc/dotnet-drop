using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;
using NetDropbox.Application.Dto;
using NetDropbox.Domain;

namespace NetDropbox.Application.Shares;

public sealed class ShareService(IAppDbContext db, IPasswordHasher passwordHasher, IBlobStorage blobs)
{
    public async Task<ShareLinkDto> CreateLinkAsync(Guid userId, Guid nodeId, SharePermission permission, DateTimeOffset? expiresAt, string? password, CancellationToken cancellationToken)
    {
        var node = await db.FileNodes.SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);

        var token = TokenGenerator.Create();
        var link = ShareLink.Create(node.Id, token, permission, expiresAt, string.IsNullOrWhiteSpace(password) ? null : passwordHasher.Hash(password));
        db.ShareLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return new ShareLinkDto(link.Id, link.Token, link.Permission, link.ExpiresAt, link.AccessCount, link.LastAccessedAt, link.Revoked);
    }

    public async Task RevokeLinkAsync(Guid userId, Guid linkId, CancellationToken cancellationToken)
    {
        var link = await db.ShareLinks.SingleOrDefaultAsync(x => x.Id == linkId, cancellationToken)
            ?? throw new AppException("Share link not found.", System.Net.HttpStatusCode.NotFound);
        var owned = await db.FileNodes.AnyAsync(x => x.Id == link.FileNodeId && x.OwnerId == userId, cancellationToken);
        if (!owned) throw new AppException("Share link not found.", System.Net.HttpStatusCode.NotFound);
        link.Revoke();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DirectShareAsync(Guid userId, Guid nodeId, string email, DirectShareRole role, CancellationToken cancellationToken)
    {
        var node = await db.FileNodes.SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);
        var target = await db.Users.SingleOrDefaultAsync(x => x.Email == email.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new AppException("Target user not found.", System.Net.HttpStatusCode.NotFound);
        db.DirectShares.Add(DirectShare.Create(node.Id, userId, target.Id, role));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FileNodeDto>> SharedWithMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.DirectShares.AsNoTracking()
            .Where(x => x.SharedWithUserId == userId)
            .Join(db.FileNodes, s => s.FileNodeId, n => n.Id, (_, n) => n)
            .Where(n => !n.IsDeleted)
            .Select(n => new FileNodeDto(n.Id, n.ParentId, n.Name, n.Type, n.ScanStatus, n.Versions.Where(v => v.Id == n.CurrentVersionId).Select(v => v.Size).FirstOrDefault(), n.Versions.Where(v => v.Id == n.CurrentVersionId).Select(v => v.MimeType).FirstOrDefault(), n.UpdatedAt, n.IsDeleted, n.ThumbnailPath))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<(Stream Stream, string FileName, string MimeType)> OpenPublicDownloadAsync(string token, string? password, CancellationToken cancellationToken)
    {
        var link = await db.ShareLinks.SingleOrDefaultAsync(x => x.Token == token, cancellationToken)
            ?? throw new AppException("Share link not found.", System.Net.HttpStatusCode.NotFound);
        if (!link.IsUsable(DateTimeOffset.UtcNow)) throw new AppException("Share link is not available.", System.Net.HttpStatusCode.Forbidden);
        if (link.PasswordHash is not null && (password is null || !passwordHasher.Verify(password, link.PasswordHash)))
            throw new AppException("Password is invalid.", System.Net.HttpStatusCode.Unauthorized);
        if (link.Permission < SharePermission.Download) throw new AppException("Download is not allowed.", System.Net.HttpStatusCode.Forbidden);

        var node = await db.FileNodes.Include(x => x.Versions).SingleAsync(x => x.Id == link.FileNodeId, cancellationToken);
        if (node.ScanStatus != FileScanStatus.Clean) throw new AppException("File is not downloadable.", System.Net.HttpStatusCode.Conflict);
        var version = node.Versions.Single(x => x.Id == node.CurrentVersionId);
        var blob = await db.Blobs.SingleAsync(x => x.Id == version.BlobId, cancellationToken);
        link.RecordAccess();
        await db.SaveChangesAsync(cancellationToken);
        return (await blobs.OpenReadAsync(blob.StorageKey, cancellationToken), node.Name, version.MimeType);
    }
}
