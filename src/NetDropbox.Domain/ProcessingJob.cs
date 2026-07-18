using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class ProcessingJob : Entity
{
    private ProcessingJob() { }

    private ProcessingJob(Guid fileNodeId, Guid fileVersionId, ProcessingJobKind kind)
    {
        FileNodeId = fileNodeId;
        FileVersionId = fileVersionId;
        Kind = kind;
    }

    public Guid FileNodeId { get; private set; }
    public Guid FileVersionId { get; private set; }
    public ProcessingJobKind Kind { get; private set; }
    public ProcessingJobStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public static ProcessingJob Queue(Guid fileNodeId, Guid fileVersionId, ProcessingJobKind kind) =>
        new(fileNodeId, fileVersionId, kind);

    public void MarkRunning()
    {
        Status = ProcessingJobStatus.Running;
        Attempts++;
        Touch();
    }

    public void MarkCompleted()
    {
        Status = ProcessingJobStatus.Completed;
        LastError = null;
        Touch();
    }

    public void MarkFailed(string error)
    {
        LastError = error;
        Status = Attempts >= 3 ? ProcessingJobStatus.DeadLettered : ProcessingJobStatus.Failed;
        Touch();
    }
}
