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

        var user = await db.Users.SingleAsync(x => x.Id == request.UserId, cancellationToken);
        user.EnsureCanReserve(session.TotalSize);

        await using var assembled = await chunks.OpenAssembledAsync(session, chunkIndexes, cancellationToken);
        var assembledContent = await Hashing.Sha256WithLengthAsync(assembled, cancellationToken);
        if (assembledContent.Length != session.TotalSize)
            throw new AppException("Final file size does not match the upload session.", System.Net.HttpStatusCode.BadRequest);
        if (!string.Equals(assembledContent.Checksum, session.FinalContentHash, StringComparison.OrdinalIgnoreCase))
            throw new AppException("Final file checksum mismatch.", System.Net.HttpStatusCode.BadRequest);

        assembled.Position = 0;
        var blob = await db.Blobs.SingleOrDefaultAsync(x => x.ContentHash == assembledContent.Checksum, cancellationToken);
        if (blob is null)
        {
            var key = await blobs.SaveAsync(assembledContent.Checksum, assembled, cancellationToken);
            blob = BlobObject.Create(assembledContent.Checksum, assembledContent.Length, key);
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

        var version = file.AddVersion(blob.Id, assembledContent.Length, session.MimeType, assembledContent.Checksum, request.UserId);
        var scanJob = ProcessingJob.Queue(file.Id, version.Id, ProcessingJobKind.VirusScan);
        db.ProcessingJobs.Add(scanJob);
        user.Reserve(session.TotalSize);
        session.Complete(file.Id);

        await db.SaveChangesAsync(cancellationToken);
        await chunks.DeleteSessionAsync(session.Id, cancellationToken);
        await queue.EnqueueAsync(new ProcessingMessage(file.Id, version.Id, scanJob.Id), cancellationToken);

        return new FileNodeDto(file.Id, file.ParentId, file.Name, file.Type, file.ScanStatus, session.TotalSize, session.MimeType, file.UpdatedAt, file.IsDeleted, file.ThumbnailPath);
    }
}
