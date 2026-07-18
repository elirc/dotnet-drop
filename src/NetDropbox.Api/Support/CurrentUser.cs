using System.Security.Claims;

namespace NetDropbox.Api.Support;

public static class CurrentUser
{
    public static Guid UserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user id claim.");
        return Guid.Parse(value);
    }
}

