using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDropbox.Api.Support;
using NetDropbox.Application.Dto;
using NetDropbox.Application.Files;

namespace NetDropbox.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/files")]
public sealed class FilesController(FileService files) : ControllerBase
{
    public sealed record FolderRequest(Guid? ParentId, string Name);
    public sealed record RenameRequest(string Name);
    public sealed record MoveRequest(Guid? ParentId);
    public sealed record CopyRequest(Guid? ParentId, string? Name);

    [HttpGet]
    public Task<IReadOnlyCollection<FileNodeDto>> List([FromQuery] Guid? parentId, [FromQuery] bool trash, CancellationToken cancellationToken) =>
        files.ListAsync(User.UserId(), parentId, trash, cancellationToken);

    [HttpPost("folders")]
    public Task<FileNodeDto> CreateFolder(FolderRequest request, CancellationToken cancellationToken) =>
        files.CreateFolderAsync(User.UserId(), request.ParentId, request.Name, cancellationToken);

    [HttpPut("{id:guid}/name")]
    public Task Rename(Guid id, RenameRequest request, CancellationToken cancellationToken) =>
        files.RenameAsync(User.UserId(), id, request.Name, cancellationToken);

    [HttpPut("{id:guid}/move")]
    public Task Move(Guid id, MoveRequest request, CancellationToken cancellationToken) =>
        files.MoveAsync(User.UserId(), id, request.ParentId, cancellationToken);

    [HttpPost("{id:guid}/copy")]
    public Task<FileNodeDto> Copy(Guid id, CopyRequest request, CancellationToken cancellationToken) =>
        files.CopyAsync(User.UserId(), id, request.ParentId, request.Name, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id, CancellationToken cancellationToken) =>
        files.DeleteAsync(User.UserId(), id, cancellationToken);

    [HttpPost("{id:guid}/restore")]
    public Task Restore(Guid id, CancellationToken cancellationToken) =>
        files.RestoreAsync(User.UserId(), id, cancellationToken);

    [HttpGet("{id:guid}/versions")]
    public Task<IReadOnlyCollection<FileVersionDto>> Versions(Guid id, CancellationToken cancellationToken) =>
        files.VersionsAsync(User.UserId(), id, cancellationToken);

    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    public Task RestoreVersion(Guid id, Guid versionId, CancellationToken cancellationToken) =>
        files.RestoreVersionAsync(User.UserId(), id, versionId, cancellationToken);

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await files.OpenDownloadAsync(User.UserId(), id, versionId, cancellationToken);
        return File(result.Stream, result.MimeType, result.FileName, enableRangeProcessing: true);
    }

    [HttpGet("search")]
    public Task<IReadOnlyCollection<FileNodeDto>> Search([FromQuery] string q, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        files.SearchAsync(User.UserId(), q ?? string.Empty, skip, take, cancellationToken);

    [HttpGet("usage")]
    public Task<UsageDto> Usage(CancellationToken cancellationToken) =>
        files.UsageAsync(User.UserId(), cancellationToken);
}

