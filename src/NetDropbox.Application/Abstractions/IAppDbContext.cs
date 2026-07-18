using Microsoft.EntityFrameworkCore;
using NetDropbox.Domain;

namespace NetDropbox.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<UserStorageAccount> Users { get; }
    DbSet<FileNode> FileNodes { get; }
    DbSet<FileVersion> FileVersions { get; }
    DbSet<BlobObject> Blobs { get; }
    DbSet<UploadSession> UploadSessions { get; }
    DbSet<UploadChunk> UploadChunks { get; }
    DbSet<ShareLink> ShareLinks { get; }
    DbSet<DirectShare> DirectShares { get; }
    DbSet<ScanResult> ScanResults { get; }
    DbSet<ProcessingJob> ProcessingJobs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

