using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;

namespace NetDropbox.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAppDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration) : ControllerBase
{
    public sealed record LoginRequest(string Email, string Password);
    public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<TokenResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new AppException("Invalid email or password.", System.Net.HttpStatusCode.Unauthorized);
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AppException("Invalid email or password.", System.Net.HttpStatusCode.Unauthorized);

        var expires = DateTimeOffset.UtcNow.AddHours(8);
        var jwt = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"] ?? "local-development-signing-key-change-me-32chars"));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"] ?? "NetDropbox",
            audience: jwt["Audience"] ?? "NetDropboxClient",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName)
            ],
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

