using System.Security.Cryptography;

namespace NetDropbox.Application.Common;

public static class Hashing
{
    public static async Task<HashedContent> Sha256WithLengthAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var buffer = new byte[128 * 1024];
        long length = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            sha.TransformBlock(buffer, 0, read, null, 0);
            length = checked(length + read);
        }

        sha.TransformFinalBlock([], 0, 0);
        return new HashedContent(Convert.ToHexString(sha.Hash!).ToLowerInvariant(), length);
    }
}

public sealed record HashedContent(string Checksum, long Length);
