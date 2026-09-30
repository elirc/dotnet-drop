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
}

