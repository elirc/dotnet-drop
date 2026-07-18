using System.Security.Cryptography;

namespace NetDropbox.Application.Common;

public static class Hashing
{
    public static async Task<string> Sha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

