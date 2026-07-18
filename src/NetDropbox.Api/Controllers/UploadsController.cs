using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetDropbox.Api.Support;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Dto;
using NetDropbox.Application.Uploads;

namespace NetDropbox.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/uploads")]
public sealed class UploadsController(IMediator mediator, IAppDbContext db) : ControllerBase
{
    public sealed record StartUploadRequest(Guid? ParentId, string FileName, long TotalSize, string MimeType, string FinalContentHash, int ChunkSize);

    [HttpPost]
    public Task<UploadSessionDto> Start(StartUploadRequest request, CancellationToken cancellationToken) =>
        mediator.Send(new StartUploadCommand(User.UserId(), request.ParentId, request.FileName, request.TotalSize, request.MimeType, request.FinalContentHash, request.ChunkSize), cancellationToken);

    [HttpGet("{sessionId:guid}")]
    public async Task<UploadSessionDto> Status(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await db.UploadSessions.SingleAsync(x => x.Id == sessionId && x.OwnerId == User.UserId(), cancellationToken);
        var uploaded = await db.UploadChunks.Where(x => x.UploadSessionId == sessionId).Select(x => x.Index).ToArrayAsync(cancellationToken);
        return new UploadSessionDto(session.Id, session.FileName, session.ChunkSize, session.TotalChunks, session.ExpiresAt, uploaded);
    }

    [HttpPut("{sessionId:guid}/chunks/{index:int}")]
    [RequestSizeLimit(64L * 1024 * 1024)]
    public async Task<IReadOnlyCollection<int>> UploadChunk(Guid sessionId, int index, [FromHeader(Name = "X-Chunk-SHA256")] string checksum, CancellationToken cancellationToken)
    {
        return await mediator.Send(new UploadChunkCommand(User.UserId(), sessionId, index, checksum, Request.Body), cancellationToken);
    }

    [HttpPost("{sessionId:guid}/complete")]
    public Task<FileNodeDto> Complete(Guid sessionId, CancellationToken cancellationToken) =>
        mediator.Send(new CompleteUploadCommand(User.UserId(), sessionId), cancellationToken);
}

