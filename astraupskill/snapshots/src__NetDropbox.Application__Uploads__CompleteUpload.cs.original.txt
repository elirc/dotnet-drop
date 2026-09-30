using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;
using NetDropbox.Application.Dto;
using NetDropbox.Domain;

namespace NetDropbox.Application.Uploads;

public sealed record CompleteUploadCommand(Guid UserId, Guid SessionId) : IRequest<FileNodeDto>;

public sealed class CompleteUploadValidator : AbstractValidator<CompleteUploadCommand>
{
    public CompleteUploadValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public sealed class CompleteUploadHandler(
    IAppDbContext db,
    IChunkStorage chunks,
    IBlobStorage blobs,
    IProcessingQueue queue,
    IClock clock) : IRequestHandler<CompleteUploadCommand, FileNodeDto>
{
    public async Task<FileNodeDto> Handle(CompleteUploadCommand request, CancellationToken cancellationToken)
    {
        var session = await db.UploadSessions.SingleOrDefaultAsync(x => x.Id == request.SessionId && x.OwnerId == request.UserId, cancellationToken)
            ?? throw new AppException("Upload session not found.", System.Net.HttpStatusCode.NotFound);

        if (session.Status != UploadSessionStatus.Open || session.IsExpired(clock.UtcNow))
            throw new AppException("Upload session is no longer open.", System.Net.HttpStatusCode.Conflict);

        var chunkIndexes = await db.UploadChunks
            .Where(x => x.UploadSessionId == session.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Index)
            .ToArrayAsync(cancellationToken);

        if (chunkIndexes.Length != session.TotalChunks || !chunkIndexes.SequenceEqual(Enumerable.Range(0, session.TotalChunks)))
            throw new AppException("Upload is missing chunks.", System.Net.HttpStatusCode.Conflict);

        await using var assembled = await chunks.OpenAssembledAsync(session, chunkIndexes, cancellationToken);
        var computed = await Hashing.Sha256Async(assembled, cancellationToken);
        if (!string.Equals(computed, session.FinalContentHash, StringComparison.OrdinalIgnoreCase))
            throw new AppException("Final file checksum mismatch.", System.Net.HttpStatusCode.BadRequest);

        assembled.Position = 0;
        var blob = await db.Blobs.SingleOrDefaultAsync(x => x.ContentHash == computed, cancellationToken);
        if (blob is null)
        {
            var key = await blobs.SaveAsync(computed, assembled, cancellationToken);
            blob = BlobObject.Create(computed, session.TotalSize, key);
            db.Blobs.Add(blob);
        }
        else
        {
            blob.AddReference();
        }

        var file = await db.FileNodes
            .Include(x => x.Versions)
            .Where(x => x.OwnerId == request.UserId && x.ParentId == session.ParentId && x.Name == session.FileName && x.Type == FileNodeType.File && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (file is null)
        {
            file = FileNode.CreateFile(request.UserId, session.ParentId, session.FileName);
            db.FileNodes.Add(file);
        }

        var version = file.AddVersion(blob.Id, session.TotalSize, session.MimeType, computed, request.UserId);
        var scanJob = ProcessingJob.Queue(file.Id, version.Id, ProcessingJobKind.VirusScan);
        db.ProcessingJobs.Add(scanJob);
        session.Complete(file.Id);

        var user = await db.Users.SingleAsync(x => x.Id == request.UserId, cancellationToken);
        user.Reserve(session.TotalSize);

        await db.SaveChangesAsync(cancellationToken);
        await chunks.DeleteSessionAsync(session.Id, cancellationToken);
        await queue.EnqueueAsync(new ProcessingMessage(file.Id, version.Id, scanJob.Id), cancellationToken);

        return new FileNodeDto(file.Id, file.ParentId, file.Name, file.Type, file.ScanStatus, session.TotalSize, session.MimeType, file.UpdatedAt, file.IsDeleted, file.ThumbnailPath);
    }
}
