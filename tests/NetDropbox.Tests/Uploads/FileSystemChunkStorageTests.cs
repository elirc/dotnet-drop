using System.Security.Cryptography;
using FluentAssertions;
using NetDropbox.Application.Abstractions;
using NetDropbox.Infrastructure.Storage;

namespace NetDropbox.Tests.Uploads;

public sealed class FileSystemChunkStorageTests
{
    [Fact]
    public async Task Invalid_duplicate_does_not_overwrite_accepted_chunk()
    {
        var root = Path.Combine(Path.GetTempPath(), $"netdrop-tests-{Guid.NewGuid():N}");
        var storage = new FileSystemChunkStorage(root);
        var accepted = "accepted"u8.ToArray();
        var acceptedHash = Hash(accepted);
        var session = NetDropbox.Domain.UploadSession.Create(
            Guid.NewGuid(), null, "file.bin", accepted.Length, acceptedHash, "application/octet-stream", accepted.Length);
        var sessionId = session.Id;

        try
        {
            var first = await storage.StoreAsync(
                sessionId,
                0,
                new MemoryStream(accepted),
                acceptedHash,
                accepted.Length,
                CancellationToken.None);
            var retry = await storage.StoreAsync(
                sessionId,
                0,
                new MemoryStream("altered!"u8.ToArray()),
                acceptedHash,
                accepted.Length,
                CancellationToken.None);

            first.Committed.Should().BeTrue();
            retry.Committed.Should().BeFalse();
            await using var assembled = await storage.OpenAssembledAsync(
                session,
                [0],
                CancellationToken.None);
            using var copy = new MemoryStream();
            await assembled.CopyToAsync(copy);
            copy.ToArray().Should().Equal(accepted);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Concurrent_different_chunks_publish_one_immutable_winner()
    {
        var root = Path.Combine(Path.GetTempPath(), $"netdrop-tests-{Guid.NewGuid():N}");
        var storage = new FileSystemChunkStorage(root);
        var first = "first-payload"u8.ToArray();
        var second = "other-payload"u8.ToArray();
        var session = NetDropbox.Domain.UploadSession.Create(
            Guid.NewGuid(), null, "file.bin", first.Length, Hash(first), "application/octet-stream", first.Length);

        try
        {
            var firstWrite = CaptureAsync(() => storage.StoreAsync(
                session.Id, 0, new MemoryStream(first), Hash(first), first.Length, CancellationToken.None));
            var secondWrite = CaptureAsync(() => storage.StoreAsync(
                session.Id, 0, new MemoryStream(second), Hash(second), second.Length, CancellationToken.None));

            var results = await Task.WhenAll(firstWrite, secondWrite);

            results.Count(result => result.Error is null).Should().Be(1);
            results.Count(result => result.Error is ChunkWriteConflictException).Should().Be(1);
            await using var assembled = await storage.OpenAssembledAsync(session, [0], CancellationToken.None);
            using var copy = new MemoryStream();
            await assembled.CopyToAsync(copy);
            var winner = copy.ToArray();
            (winner.SequenceEqual(first) || winner.SequenceEqual(second)).Should().BeTrue();

            var winnerHash = Hash(winner);
            var retry = await storage.StoreAsync(
                session.Id, 0, new MemoryStream(winner), winnerHash, winner.Length, CancellationToken.None);
            retry.Committed.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Concurrent_assembly_streams_are_independent_and_delete_on_close()
    {
        var root = Path.Combine(Path.GetTempPath(), $"netdrop-tests-{Guid.NewGuid():N}");
        var storage = new FileSystemChunkStorage(root);
        var content = "content"u8.ToArray();
        var session = NetDropbox.Domain.UploadSession.Create(
            Guid.NewGuid(), null, "file.bin", content.Length, Hash(content), "application/octet-stream", content.Length);

        try
        {
            await storage.StoreAsync(
                session.Id, 0, new MemoryStream(content), Hash(content), content.Length, CancellationToken.None);
            await using var first = await storage.OpenAssembledAsync(session, [0], CancellationToken.None);
            await using var second = await storage.OpenAssembledAsync(session, [0], CancellationToken.None);

            first.Should().NotBeSameAs(second);
            Directory.GetFiles(root, "*.assembled").Should().HaveCount(2);
            await first.DisposeAsync();
            await second.DisposeAsync();
            Directory.GetFiles(root, "*.assembled").Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Oversized_chunk_stops_after_boundary_probe_and_leaves_no_publication_or_temp_file()
    {
        var root = Path.Combine(Path.GetTempPath(), $"netdrop-tests-{Guid.NewGuid():N}");
        var storage = new FileSystemChunkStorage(root);
        var expected = "data"u8.ToArray();
        var oversized = new CountingStream(new byte[1024 * 1024]);
        var session = NetDropbox.Domain.UploadSession.Create(
            Guid.NewGuid(), null, "file.bin", expected.Length, Hash(expected), "application/octet-stream", expected.Length);

        try
        {
            var result = await storage.StoreAsync(
                session.Id, 0, oversized, Hash(expected), expected.Length, CancellationToken.None);

            result.Committed.Should().BeFalse();
            result.Size.Should().Be(expected.Length + 1);
            oversized.BytesRead.Should().Be(expected.Length + 1);
            Directory.GetFiles(root, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<(StoredChunk? Result, Exception? Error)> CaptureAsync(Func<Task<StoredChunk>> action)
    {
        try
        {
            return (await action(), null);
        }
        catch (Exception exception)
        {
            return (null, exception);
        }
    }

    private static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private sealed class CountingStream(byte[] content) : MemoryStream(content)
    {
        public int BytesRead { get; private set; }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await base.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }
    }
}
