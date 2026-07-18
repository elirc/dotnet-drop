using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDropbox.Api.Support;
using NetDropbox.Application.Dto;
using NetDropbox.Application.Shares;
using NetDropbox.Domain;

namespace NetDropbox.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/shares")]
public sealed class SharesController(ShareService shares) : ControllerBase
{
    public sealed record LinkRequest(Guid NodeId, SharePermission Permission, DateTimeOffset? ExpiresAt, string? Password);
    public sealed record DirectShareRequest(Guid NodeId, string Email, DirectShareRole Role);

    [HttpPost("links")]
    public Task<ShareLinkDto> CreateLink(LinkRequest request, CancellationToken cancellationToken) =>
        shares.CreateLinkAsync(User.UserId(), request.NodeId, request.Permission, request.ExpiresAt, request.Password, cancellationToken);

    [HttpDelete("links/{linkId:guid}")]
    public Task RevokeLink(Guid linkId, CancellationToken cancellationToken) =>
        shares.RevokeLinkAsync(User.UserId(), linkId, cancellationToken);

    [HttpPost("direct")]
    public Task DirectShare(DirectShareRequest request, CancellationToken cancellationToken) =>
        shares.DirectShareAsync(User.UserId(), request.NodeId, request.Email, request.Role, cancellationToken);

    [HttpGet("direct/shared-with-me")]
    public Task<IReadOnlyCollection<FileNodeDto>> SharedWithMe(CancellationToken cancellationToken) =>
        shares.SharedWithMeAsync(User.UserId(), cancellationToken);
}

