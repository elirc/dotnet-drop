using NetDropbox.Domain;

namespace NetDropbox.Application.Dto;

public sealed record FileNodeDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    FileNodeType Type,
    FileScanStatus ScanStatus,
    long Size,
    string? MimeType,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    string? ThumbnailPath);

public sealed record FileVersionDto(
    Guid Id,
    int VersionNumber,
    long Size,
    string MimeType,
    string ContentHash,
    DateTimeOffset CreatedAt);

public sealed record UploadSessionDto(
    Guid Id,
    string FileName,
    int ChunkSize,
    int TotalChunks,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<int> UploadedChunks);

public sealed record ShareLinkDto(
    Guid Id,
    string Token,
    SharePermission Permission,
    DateTimeOffset? ExpiresAt,
    long AccessCount,
    DateTimeOffset? LastAccessedAt,
    bool Revoked);

public sealed record UsageDto(long UsedBytes, long QuotaBytes);

