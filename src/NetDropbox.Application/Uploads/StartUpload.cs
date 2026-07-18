using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;
using NetDropbox.Application.Dto;
using NetDropbox.Domain;

namespace NetDropbox.Application.Uploads;

public sealed record StartUploadCommand(
    Guid UserId,
    Guid? ParentId,
    string FileName,
    long TotalSize,
    string MimeType,
    string FinalContentHash,
    int ChunkSize) : IRequest<UploadSessionDto>;

public sealed class StartUploadValidator : AbstractValidator<StartUploadCommand>
{
    public StartUploadValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TotalSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FinalContentHash).NotEmpty().Length(64);
        RuleFor(x => x.ChunkSize).InclusiveBetween(256 * 1024, 32 * 1024 * 1024);
    }
}

public sealed class StartUploadHandler(IAppDbContext db) : IRequestHandler<StartUploadCommand, UploadSessionDto>
{
    public async Task<UploadSessionDto> Handle(StartUploadCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new AppException("User not found.", System.Net.HttpStatusCode.Unauthorized);

        if (user.UsedBytes + request.TotalSize > user.QuotaBytes)
            throw new AppException("Storage quota exceeded.", System.Net.HttpStatusCode.Conflict);

        if (request.ParentId is not null)
        {
            var folderOk = await db.FileNodes.AnyAsync(x =>
                x.Id == request.ParentId &&
                x.OwnerId == request.UserId &&
                x.Type == FileNodeType.Folder &&
                !x.IsDeleted, cancellationToken);
            if (!folderOk) throw new AppException("Parent folder was not found.", System.Net.HttpStatusCode.NotFound);
        }

        var existing = await db.UploadSessions
            .Where(x => x.OwnerId == request.UserId &&
                        x.ParentId == request.ParentId &&
                        x.FileName == request.FileName &&
                        x.FinalContentHash == request.FinalContentHash &&
                        x.Status == UploadSessionStatus.Open)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var session = existing ?? UploadSession.Create(
            request.UserId,
            request.ParentId,
            request.FileName,
            request.TotalSize,
            request.FinalContentHash,
            request.MimeType,
            request.ChunkSize);

        if (existing is null) db.UploadSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var chunks = await db.UploadChunks
            .Where(x => x.UploadSessionId == session.Id)
            .Select(x => x.Index)
            .ToArrayAsync(cancellationToken);

        return new UploadSessionDto(session.Id, session.FileName, session.ChunkSize, session.TotalChunks, session.ExpiresAt, chunks);
    }
}

