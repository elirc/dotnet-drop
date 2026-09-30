using FluentAssertions;
using NetDropbox.Domain;

namespace NetDropbox.Tests.Domain;

public sealed class UploadSessionTests
{
    [Fact]
    public void Upload_session_calculates_total_chunks()
    {
        var session = UploadSession.Create(Guid.NewGuid(), null, "big.bin", 10_000_001, new string('c', 64), "application/octet-stream", 4_000_000);

        session.TotalChunks.Should().Be(3);
        session.Status.Should().Be(UploadSessionStatus.Open);
    }

    [Fact]
    public void Completed_upload_remembers_file_node()
    {
        var session = UploadSession.Create(Guid.NewGuid(), null, "big.bin", 10, new string('d', 64), "application/octet-stream", 4);
        var fileId = Guid.NewGuid();

        session.Complete(fileId);

        session.Status.Should().Be(UploadSessionStatus.Completed);
        session.CompletedFileNodeId.Should().Be(fileId);
    }

    [Theory]
    [InlineData("not-a-sha256")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void Upload_session_rejects_invalid_sha256(string hash)
    {
        var action = () => UploadSession.Create(Guid.NewGuid(), null, "big.bin", 10, hash, "application/octet-stream", 4);

        action.Should().Throw<Exception>().WithMessage("*SHA-256*");
    }

    [Fact]
    public void Upload_session_rejects_chunk_count_that_cannot_be_represented()
    {
        var action = () => UploadSession.Create(
            Guid.NewGuid(),
            null,
            "huge.bin",
            (long)int.MaxValue + 1,
            new string('a', 64),
            "application/octet-stream",
            1);

        action.Should().Throw<Exception>().WithMessage("*too many chunks*");
    }

    [Fact]
    public void Expected_chunk_size_uses_full_chunks_and_exact_final_remainder()
    {
        var session = UploadSession.Create(Guid.NewGuid(), null, "big.bin", 10, new string('a', 64), "application/octet-stream", 4);

        session.ExpectedChunkSize(0).Should().Be(4);
        session.ExpectedChunkSize(1).Should().Be(4);
        session.ExpectedChunkSize(2).Should().Be(2);
        var action = () => session.ExpectedChunkSize(-1);
        action.Should().Throw<Exception>().WithMessage("*outside*");
    }

    [Fact]
    public void Terminal_session_cannot_transition_again()
    {
        var session = UploadSession.Create(Guid.NewGuid(), null, "big.bin", 10, new string('a', 64), "application/octet-stream", 4);
        session.Complete(Guid.NewGuid());

        var completeAgain = () => session.Complete(Guid.NewGuid());
        Action expireCompleted = session.Expire;

        completeAgain.Should().Throw<Exception>().WithMessage("*no longer open*");
        expireCompleted.Should().Throw<Exception>().WithMessage("*no longer open*");
    }
}
