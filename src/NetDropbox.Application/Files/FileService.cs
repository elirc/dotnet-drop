using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;
using NetDropbox.Application.Dto;
using NetDropbox.Domain;

namespace NetDropbox.Application.Files;

public sealed class FileService(IAppDbContext db, IBlobStorage blobs)
{
    public async Task<IReadOnlyCollection<FileNodeDto>> ListAsync(Guid userId, Guid? parentId, bool trash, CancellationToken cancellationToken)
    {
        var query = db.FileNodes
            .AsNoTracking()
            .Where(x => x.OwnerId == userId && x.ParentId == parentId && x.IsDeleted == trash);

        return await query
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .Select(x => new FileNodeDto(
                x.Id,
                x.ParentId,
                x.Name,
                x.Type,
                x.ScanStatus,
                x.Versions.Where(v => v.Id == x.CurrentVersionId).Select(v => v.Size).FirstOrDefault(),
                x.Versions.Where(v => v.Id == x.CurrentVersionId).Select(v => v.MimeType).FirstOrDefault(),
                x.UpdatedAt,
                x.IsDeleted,
                x.ThumbnailPath))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FileNodeDto> CreateFolderAsync(Guid userId, Guid? parentId, string name, CancellationToken cancellationToken)
    {
        var exists = await db.FileNodes.AnyAsync(x => x.OwnerId == userId && x.ParentId == parentId && x.Name == name && !x.IsDeleted, cancellationToken);
        if (exists) throw new AppException("A node with that name already exists.", System.Net.HttpStatusCode.Conflict);

        var folder = FileNode.CreateFolder(userId, parentId, name);
        db.FileNodes.Add(folder);
        await db.SaveChangesAsync(cancellationToken);
        return new FileNodeDto(folder.Id, folder.ParentId, folder.Name, folder.Type, folder.ScanStatus, 0, null, folder.UpdatedAt, folder.IsDeleted, null);
    }

    public async Task RenameAsync(Guid userId, Guid nodeId, string name, CancellationToken cancellationToken)
    {
        var node = await GetOwnedNode(userId, nodeId, cancellationToken);
        node.Rename(name);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveAsync(Guid userId, Guid nodeId, Guid? parentId, CancellationToken cancellationToken)
    {
        var node = await GetOwnedNode(userId, nodeId, cancellationToken);
        if (parentId is not null)
        {
            var parent = await GetOwnedNode(userId, parentId.Value, cancellationToken);
            if (parent.Type != FileNodeType.Folder) throw new AppException("Destination is not a folder.");
        }
        node.Move(parentId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FileNodeDto> CopyAsync(Guid userId, Guid nodeId, Guid? parentId, string? newName, CancellationToken cancellationToken)
    {
        var node = await db.FileNodes.Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId, cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);
        if (node.Type != FileNodeType.File) throw new AppException("Only file copy is implemented.");

        var current = node.Versions.Single(x => x.Id == node.CurrentVersionId);
        var blob = await db.Blobs.SingleAsync(x => x.Id == current.BlobId, cancellationToken);
        blob.AddReference();

        var copy = FileNode.CreateFile(userId, parentId, newName ?? $"Copy of {node.Name}");
        copy.AddVersion(blob.Id, current.Size, current.MimeType, current.ContentHash, userId);
        copy.MarkScanClean(node.ExtractedText, node.ThumbnailPath);
        db.FileNodes.Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        return new FileNodeDto(copy.Id, copy.ParentId, copy.Name, copy.Type, copy.ScanStatus, current.Size, current.MimeType, copy.UpdatedAt, false, copy.ThumbnailPath);
    }

    public async Task DeleteAsync(Guid userId, Guid nodeId, CancellationToken cancellationToken)
    {
        var node = await GetOwnedNode(userId, nodeId, cancellationToken);
        node.MarkDeleted();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid userId, Guid nodeId, CancellationToken cancellationToken)
    {
        var node = await GetOwnedNode(userId, nodeId, cancellationToken, includeDeleted: true);
        node.RestoreFromTrash();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FileVersionDto>> VersionsAsync(Guid userId, Guid nodeId, CancellationToken cancellationToken)
    {
        var node = await db.FileNodes.AsNoTracking().Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId, cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);
        return node.Versions
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new FileVersionDto(x.Id, x.VersionNumber, x.Size, x.MimeType, x.ContentHash, x.CreatedAt))
            .ToArray();
    }

    public async Task RestoreVersionAsync(Guid userId, Guid nodeId, Guid versionId, CancellationToken cancellationToken)
    {
        var node = await db.FileNodes.Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId, cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);
        node.RestoreVersion(versionId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(Stream Stream, string FileName, string MimeType)> OpenDownloadAsync(Guid userId, Guid nodeId, Guid? versionId, CancellationToken cancellationToken)
    {
        var node = await db.FileNodes.Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId && !x.IsDeleted, cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);
        EnsureDownloadable(node);

        var version = node.Versions.Single(x => x.Id == (versionId ?? node.CurrentVersionId));
        var blob = await db.Blobs.SingleAsync(x => x.Id == version.BlobId, cancellationToken);
        return (await blobs.OpenReadAsync(blob.StorageKey, cancellationToken), node.Name, version.MimeType);
    }

    public async Task<IReadOnlyCollection<FileNodeDto>> SearchAsync(Guid userId, string query, int skip, int take, CancellationToken cancellationToken)
    {
        query = query.Trim().ToLowerInvariant();
        return await db.FileNodes.AsNoTracking()
            .Where(x => x.OwnerId == userId && !x.IsDeleted && (x.Name.ToLower().Contains(query) || (x.ExtractedText != null && x.ExtractedText.ToLower().Contains(query))))
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(Math.Clamp(take, 1, 100))
            .Select(x => new FileNodeDto(x.Id, x.ParentId, x.Name, x.Type, x.ScanStatus, x.Versions.Where(v => v.Id == x.CurrentVersionId).Select(v => v.Size).FirstOrDefault(), x.Versions.Where(v => v.Id == x.CurrentVersionId).Select(v => v.MimeType).FirstOrDefault(), x.UpdatedAt, x.IsDeleted, x.ThumbnailPath))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<UsageDto> UsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId, cancellationToken);
        return new UsageDto(user.UsedBytes, user.QuotaBytes);
    }

    private async Task<FileNode> GetOwnedNode(Guid userId, Guid nodeId, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        return await db.FileNodes.SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId && (includeDeleted || !x.IsDeleted), cancellationToken)
            ?? throw new AppException("Node not found.", System.Net.HttpStatusCode.NotFound);
    }

    private static void EnsureDownloadable(FileNode node)
    {
        if (node.Type != FileNodeType.File) throw new AppException("Only files can be downloaded.");
        if (node.ScanStatus == FileScanStatus.PendingScan) throw new AppException("File is still being scanned.", System.Net.HttpStatusCode.Conflict);
        if (node.ScanStatus == FileScanStatus.Infected) throw new AppException("File is quarantined.", System.Net.HttpStatusCode.Forbidden);
        if (node.ScanStatus == FileScanStatus.Failed) throw new AppException("File scan failed.", System.Net.HttpStatusCode.Conflict);
    }
}

