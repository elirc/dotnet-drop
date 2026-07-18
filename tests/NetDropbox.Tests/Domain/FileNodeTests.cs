using FluentAssertions;
using NetDropbox.Domain;

namespace NetDropbox.Tests.Domain;

public sealed class FileNodeTests
{
    [Fact]
    public void Adding_a_file_version_marks_file_pending_scan()
    {
        var ownerId = Guid.NewGuid();
        var file = FileNode.CreateFile(ownerId, null, "report.txt");

        var version = file.AddVersion(Guid.NewGuid(), 42, "text/plain", new string('a', 64), ownerId);

        file.CurrentVersionId.Should().Be(version.Id);
        file.ScanStatus.Should().Be(FileScanStatus.PendingScan);
        file.Versions.Should().ContainSingle();
    }

    [Fact]
    public void Folder_names_cannot_contain_path_separators()
    {
        var action = () => FileNode.CreateFolder(Guid.NewGuid(), null, "../bad");

        action.Should().Throw<Exception>().WithMessage("*separator*");
    }

    [Fact]
    public void Clean_scan_allows_extracted_text_and_thumbnail_metadata()
    {
        var ownerId = Guid.NewGuid();
        var file = FileNode.CreateFile(ownerId, null, "photo.png");
        file.AddVersion(Guid.NewGuid(), 100, "image/png", new string('b', 64), ownerId);

        file.MarkScanClean(null, "thumbnail-key");

        file.ScanStatus.Should().Be(FileScanStatus.Clean);
        file.ThumbnailPath.Should().Be("thumbnail-key");
    }
}

