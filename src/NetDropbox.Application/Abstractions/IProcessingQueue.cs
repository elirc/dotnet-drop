namespace NetDropbox.Application.Abstractions;

public sealed record ProcessingMessage(Guid FileNodeId, Guid FileVersionId, Guid JobId);

public interface IProcessingQueue
{
    ValueTask EnqueueAsync(ProcessingMessage message, CancellationToken cancellationToken);
    IAsyncEnumerable<ProcessingMessage> DequeueAsync(CancellationToken cancellationToken);
}

