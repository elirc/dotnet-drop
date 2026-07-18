using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Domain;

namespace NetDropbox.Infrastructure.Persistence;

public sealed class NetDropboxDbContext(DbContextOptions<NetDropboxDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<UserStorageAccount> Users => Set<UserStorageAccount>();
    public DbSet<FileNode> FileNodes => Set<FileNode>();
    public DbSet<FileVersion> FileVersions => Set<FileVersion>();
    public DbSet<BlobObject> Blobs => Set<BlobObject>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<UploadChunk> UploadChunks => Set<UploadChunk>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<DirectShare> DirectShares => Set<DirectShare>();
    public DbSet<ScanResult> ScanResults => Set<ScanResult>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("netdropbox");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NetDropboxDbContext).Assembly);

        modelBuilder.Entity<UserStorageAccount>(b =>
        {
            b.ToTable("users");
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.DisplayName).HasMaxLength(200);
            b.Property(x => x.PasswordHash).HasMaxLength(512);
            b.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<FileNode>(b =>
        {
            b.ToTable("file_nodes");
            b.HasIndex(x => new { x.OwnerId, x.ParentId, x.Name, x.IsDeleted });
            b.Property(x => x.Name).HasMaxLength(255);
            b.Property(x => x.ExtractedText).HasColumnType("text");
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.ScanStatus).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Version).IsConcurrencyToken();
            b.HasMany<FileVersion>("_versions")
                .WithOne()
                .HasForeignKey(x => x.FileNodeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FileVersion>(b =>
        {
            b.ToTable("file_versions");
            b.HasIndex(x => new { x.FileNodeId, x.VersionNumber }).IsUnique();
            b.Property(x => x.MimeType).HasMaxLength(255);
            b.Property(x => x.ContentHash).HasMaxLength(64);
        });

        modelBuilder.Entity<BlobObject>(b =>
        {
            b.ToTable("blobs");
            b.HasIndex(x => x.ContentHash).IsUnique();
            b.Property(x => x.ContentHash).HasMaxLength(64);
            b.Property(x => x.StorageKey).HasMaxLength(512);
        });

        modelBuilder.Entity<UploadSession>(b =>
        {
            b.ToTable("upload_sessions");
            b.HasIndex(x => new { x.OwnerId, x.Status, x.ExpiresAt });
            b.Property(x => x.FileName).HasMaxLength(255);
            b.Property(x => x.MimeType).HasMaxLength(255);
            b.Property(x => x.FinalContentHash).HasMaxLength(64);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<UploadChunk>(b =>
        {
            b.ToTable("upload_chunks");
            b.HasIndex(x => new { x.UploadSessionId, x.Index }).IsUnique();
            b.Property(x => x.Checksum).HasMaxLength(64);
        });

        modelBuilder.Entity<ShareLink>(b =>
        {
            b.ToTable("share_links");
            b.HasIndex(x => x.Token).IsUnique();
            b.Property(x => x.Token).HasMaxLength(128);
            b.Property(x => x.PasswordHash).HasMaxLength(512);
            b.Property(x => x.Permission).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<DirectShare>(b =>
        {
            b.ToTable("direct_shares");
            b.HasIndex(x => new { x.FileNodeId, x.SharedWithUserId }).IsUnique();
            b.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<ScanResult>(b =>
        {
            b.ToTable("scan_results");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Scanner).HasMaxLength(128);
            b.Property(x => x.Signature).HasMaxLength(255);
        });

        modelBuilder.Entity<ProcessingJob>(b =>
        {
            b.ToTable("processing_jobs");
            b.HasIndex(x => new { x.Status, x.CreatedAt });
            b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.LastError).HasMaxLength(2000);
        });
    }
}

