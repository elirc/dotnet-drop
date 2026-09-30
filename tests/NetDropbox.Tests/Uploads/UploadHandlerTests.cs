using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Common;
using NetDropbox.Application.Uploads;
using NetDropbox.Domain;
using NetDropbox.Infrastructure.Persistence;

namespace NetDropbox.Tests.Uploads;

public sealed class UploadHandlerTests
{
    [Fact]
    public async Task Complete_wrong_assembled_length_preserves_persistent_state()
    {
        var bytes = "too-long"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: 4, content: bytes, quota: 100);

        await using (var db = fixture.CreateContext())
        {
            var handler = new CompleteUploadHandler(db, fixture.Chunks, fixture.Blobs, fixture.Queue, fixture.Clock);
            var action = () => handler.Handle(
                new CompleteUploadCommand(fixture.UserId, fixture.SessionId),
                CancellationToken.None);

            await action.Should().ThrowAsync<AppException>().WithMessage("*size*");
        }

        fixture.Blobs.SaveCount.Should().Be(0);
        await fixture.AssertUncommittedAsync();
    }

    [Fact]
    public async Task Complete_late_quota_failure_happens_before_external_write()
    {
        var bytes = "data"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: bytes.Length, content: bytes, quota: bytes.Length + 1, alreadyUsed: 2);

        await using (var db = fixture.CreateContext())
        {
            var handler = new CompleteUploadHandler(db, fixture.Chunks, fixture.Blobs, fixture.Queue, fixture.Clock);
            var action = () => handler.Handle(
                new CompleteUploadCommand(fixture.UserId, fixture.SessionId),
                CancellationToken.None);

            await action.Should().ThrowAsync<Exception>().WithMessage("*quota*");
        }

        fixture.Chunks.OpenCount.Should().Be(0);
        fixture.Blobs.SaveCount.Should().Be(0);
        await fixture.AssertUncommittedAsync(expectedUsedBytes: 2);
    }

    [Fact]
    public async Task Complete_valid_upload_commits_once_and_retry_does_not_double_charge()
    {
        var bytes = "data"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: bytes.Length, content: bytes, quota: 100);

        await using (var db = fixture.CreateContext())
        {
            var handler = new CompleteUploadHandler(db, fixture.Chunks, fixture.Blobs, fixture.Queue, fixture.Clock);
            await handler.Handle(new CompleteUploadCommand(fixture.UserId, fixture.SessionId), CancellationToken.None);
        }

        await using (var retryDb = fixture.CreateContext())
        {
            var handler = new CompleteUploadHandler(retryDb, fixture.Chunks, fixture.Blobs, fixture.Queue, fixture.Clock);
            var retry = () => handler.Handle(
                new CompleteUploadCommand(fixture.UserId, fixture.SessionId),
                CancellationToken.None);
            await retry.Should().ThrowAsync<AppException>().WithMessage("*no longer open*");
        }

        await using var verify = fixture.CreateContext();
        var user = await verify.Users.SingleAsync(x => x.Id == fixture.UserId);
        var session = await verify.UploadSessions.SingleAsync(x => x.Id == fixture.SessionId);
        user.UsedBytes.Should().Be(bytes.Length);
        session.Status.Should().Be(UploadSessionStatus.Completed);
        (await verify.FileNodes.CountAsync()).Should().Be(1);
        (await verify.FileVersions.CountAsync()).Should().Be(1);
        (await verify.Blobs.CountAsync()).Should().Be(1);
        (await verify.ProcessingJobs.CountAsync()).Should().Be(1);
        fixture.Blobs.SaveCount.Should().Be(1);
        fixture.Queue.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadChunk_changed_duplicate_is_rejected_before_storage_write()
    {
        var bytes = "data"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: bytes.Length, content: bytes, quota: 100, seedChunks: false);

        await using (var seed = fixture.CreateContext())
        {
            var acceptedHash = Hash(bytes);
            seed.UploadChunks.Add(UploadChunk.Create(fixture.SessionId, 0, acceptedHash, bytes.Length));
            await seed.SaveChangesAsync();
        }

        await using var db = fixture.CreateContext();
        var handler = new UploadChunkHandler(db, fixture.Chunks, fixture.Clock);
        var changed = "diff"u8.ToArray();
        var action = () => handler.Handle(
            new UploadChunkCommand(fixture.UserId, fixture.SessionId, 0, Hash(changed), new MemoryStream(changed)),
            CancellationToken.None);

        await action.Should().ThrowAsync<AppException>().WithMessage("*cannot be replaced*");
        fixture.Chunks.StoreCount.Should().Be(0);
        (await db.UploadChunks.SingleAsync()).Checksum.Should().Be(Hash(bytes));
    }

    [Fact]
    public async Task UploadChunk_valid_duplicate_is_idempotent_and_negative_index_is_rejected()
    {
        var bytes = "data"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: bytes.Length, content: bytes, quota: 100, seedChunks: false);
        var checksum = Hash(bytes);
        await using (var seed = fixture.CreateContext())
        {
            seed.UploadChunks.Add(UploadChunk.Create(fixture.SessionId, 0, checksum, bytes.Length));
            await seed.SaveChangesAsync();
        }

        await using var db = fixture.CreateContext();
        var handler = new UploadChunkHandler(db, fixture.Chunks, fixture.Clock);
        var indexes = await handler.Handle(
            new UploadChunkCommand(fixture.UserId, fixture.SessionId, 0, checksum, new MemoryStream(bytes)),
            CancellationToken.None);
        var invalid = () => handler.Handle(
            new UploadChunkCommand(fixture.UserId, fixture.SessionId, -1, checksum, new MemoryStream(bytes)),
            CancellationToken.None);

        indexes.Should().Equal(0);
        (await db.UploadChunks.CountAsync()).Should().Be(1);
        await invalid.Should().ThrowAsync<AppException>().WithMessage("*outside*");
        fixture.Chunks.StoreCount.Should().Be(1);
    }

    private static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private sealed class Fixture
    {
        private readonly DbContextOptions<NetDropboxDbContext> _options;

        private Fixture(
            DbContextOptions<NetDropboxDbContext> options,
            Guid userId,
            Guid sessionId,
            FakeChunkStorage chunks,
            FakeBlobStorage blobs,
            FakeProcessingQueue queue)
        {
            _options = options;
            UserId = userId;
            SessionId = sessionId;
            Chunks = chunks;
            Blobs = blobs;
            Queue = queue;
        }

        public Guid UserId { get; }
        public Guid SessionId { get; }
        public FakeChunkStorage Chunks { get; }
        public FakeBlobStorage Blobs { get; }
        public FakeProcessingQueue Queue { get; }
        public FixedClock Clock { get; } = new();

        public static async Task<Fixture> CreateAsync(
            long declaredSize,
            byte[] content,
            long quota,
            long alreadyUsed = 0,
            bool seedChunks = true)
        {
            var options = new DbContextOptionsBuilder<NetDropboxDbContext>()
                .UseInMemoryDatabase($"netdrop-{Guid.NewGuid():N}")
                .Options;
            var user = UserStorageAccount.Create("owner@example.com", "Owner", quota);
            if (alreadyUsed > 0) user.Reserve(alreadyUsed);
            var session = UploadSession.Create(
                user.Id,
                null,
                "file.bin",
                declaredSize,
                Hash(content),
                "application/octet-stream",
                Math.Max(1, (int)Math.Min(int.MaxValue, declaredSize)));

            await using (var db = new NetDropboxDbContext(options))
            {
                db.Users.Add(user);
                db.UploadSessions.Add(session);
                for (var index = 0; seedChunks && index < session.TotalChunks; index++)
                {
                    db.UploadChunks.Add(UploadChunk.Create(
                        session.Id,
                        index,
                        Hash(content),
                        session.ExpectedChunkSize(index)));
                }
                await db.SaveChangesAsync();
            }

            return new Fixture(
                options,
                user.Id,
                session.Id,
                new FakeChunkStorage(content),
                new FakeBlobStorage(),
                new FakeProcessingQueue());
        }

        public NetDropboxDbContext CreateContext() => new(_options);

        public async Task AssertUncommittedAsync(long expectedUsedBytes = 0)
        {
            await using var verify = CreateContext();
            (await verify.Users.SingleAsync(x => x.Id == UserId)).UsedBytes.Should().Be(expectedUsedBytes);
            (await verify.UploadSessions.SingleAsync(x => x.Id == SessionId)).Status.Should().Be(UploadSessionStatus.Open);
            (await verify.FileNodes.CountAsync()).Should().Be(0);
            (await verify.FileVersions.CountAsync()).Should().Be(0);
            (await verify.Blobs.CountAsync()).Should().Be(0);
            (await verify.ProcessingJobs.CountAsync()).Should().Be(0);
        }
    }

    private sealed class FakeChunkStorage(byte[] assembled) : IChunkStorage
    {
        public int StoreCount { get; private set; }
        public int OpenCount { get; private set; }

        public async Task<StoredChunk> StoreAsync(
            Guid sessionId,
            int index,
            Stream content,
            string expectedChecksum,
            long expectedSize,
            CancellationToken cancellationToken)
        {
            StoreCount++;
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            var bytes = copy.ToArray();
            var checksum = Hash(bytes);
            return new StoredChunk(
                checksum,
                bytes.Length,
                bytes.Length == expectedSize && string.Equals(checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase));
        }

        public Task<Stream> OpenAssembledAsync(
            UploadSession session,
            IReadOnlyCollection<int> chunkIndexes,
            CancellationToken cancellationToken)
        {
            OpenCount++;
            return Task.FromResult<Stream>(new MemoryStream(assembled, writable: false));
        }

        public Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBlobStorage : IBlobStorage
    {
        private readonly Dictionary<string, byte[]> _content = [];
        public int SaveCount { get; private set; }

        public Task<bool> ExistsAsync(string contentHash, CancellationToken cancellationToken) =>
            Task.FromResult(_content.ContainsKey(contentHash));

        public async Task<string> SaveAsync(string contentHash, Stream content, CancellationToken cancellationToken)
        {
            SaveCount++;
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            _content[contentHash] = copy.ToArray();
            return contentHash;
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(_content[storageKey], writable: false));

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            _content.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcessingQueue : IProcessingQueue
    {
        public List<ProcessingMessage> Messages { get; } = [];

        public ValueTask EnqueueAsync(ProcessingMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<ProcessingMessage> DequeueAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    public sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 19, 18, 0, 0, TimeSpan.Zero);
    }
}
