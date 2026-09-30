using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;
using NetDropbox.Domain;

namespace NetDropbox.Application.Uploads;

public sealed record UploadChunkCommand(
    Guid UserId,
    Guid SessionId,
    int Index,
    string Checksum,
    Stream Content) : IRequest<IReadOnlyCollection<int>>;

public sealed class UploadChunkValidator : AbstractValidator<UploadChunkCommand>
{
    public UploadChunkValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Index).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Checksum).NotEmpty().Length(64);
    }
}

public sealed class UploadChunkHandler(IAppDbContext db, IChunkStorage chunks, IClock clock) : IRequestHandler<UploadChunkCommand, IReadOnlyCollection<int>>
{
    public async Task<IReadOnlyCollection<int>> Handle(UploadChunkCommand request, CancellationToken cancellationToken)
    {
        var session = await db.UploadSessions.SingleOrDefaultAsync(x => x.Id == request.SessionId && x.OwnerId == request.UserId, cancellationToken)
            ?? throw new AppException("Upload session not found.", System.Net.HttpStatusCode.NotFound);

        if (session.Status != UploadSessionStatus.Open || session.IsExpired(clock.UtcNow))
            throw new AppException("Upload session is no longer open.", System.Net.HttpStatusCode.Conflict);

        if (request.Index >= session.TotalChunks)
            throw new AppException("Chunk index is outside the session range.");

        var stored = await chunks.StoreAsync(session.Id, request.Index, request.Content, cancellationToken);
        if (!string.Equals(stored.Checksum, request.Checksum, StringComparison.OrdinalIgnoreCase))
            throw new AppException("Chunk checksum mismatch.", System.Net.HttpStatusCode.BadRequest);

        var existing = await db.UploadChunks.SingleOrDefaultAsync(x => x.UploadSessionId == session.Id && x.Index == request.Index, cancellationToken);
        if (existing is null)
        {
            db.UploadChunks.Add(UploadChunk.Create(session.Id, request.Index, stored.Checksum, stored.Size));
            await db.SaveChangesAsync(cancellationToken);
        }

        return await db.UploadChunks
            .Where(x => x.UploadSessionId == session.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Index)
            .ToArrayAsync(cancellationToken);
    }
}

