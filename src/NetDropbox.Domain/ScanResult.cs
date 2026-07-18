using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class ScanResult : Entity
{
    private ScanResult() { }

    private ScanResult(Guid fileNodeId, Guid fileVersionId, FileScanStatus status, string scanner, string? signature)
    {
        FileNodeId = fileNodeId;
        FileVersionId = fileVersionId;
        Status = status;
        Scanner = scanner;
        Signature = signature;
    }

    public Guid FileNodeId { get; private set; }
    public Guid FileVersionId { get; private set; }
    public FileScanStatus Status { get; private set; }
    public string Scanner { get; private set; } = "simulated-eicar";
    public string? Signature { get; private set; }

    public static ScanResult Create(Guid fileNodeId, Guid fileVersionId, FileScanStatus status, string scanner, string? signature) =>
        new(fileNodeId, fileVersionId, status, scanner, signature);
}

