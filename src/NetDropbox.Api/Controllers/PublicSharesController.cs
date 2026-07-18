using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NetDropbox.Application.Shares;

namespace NetDropbox.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/shares")]
public sealed class PublicSharesController(ShareService shares) : ControllerBase
{
    [HttpGet("{token}/download")]
    [EnableRateLimiting("public")]
    public async Task<IActionResult> Download(string token, [FromQuery] string? password, CancellationToken cancellationToken)
    {
        var result = await shares.OpenPublicDownloadAsync(token, password, cancellationToken);
        return File(result.Stream, result.MimeType, result.FileName, enableRangeProcessing: true);
    }
}
