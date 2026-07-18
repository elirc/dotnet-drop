using System.Threading.Channels;
using NetDropbox.Application.Abstractions;

namespace NetDropbox.Infrastructure.Processing;

public sealed class ChannelProcessingQueue : IProcessingQueue
{
    private readonly Channel<ProcessingMessage> _channel = Channel.CreateUnbounded<ProcessingMessage>();

    public ValueTask EnqueueAsync(ProcessingMessage message, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<ProcessingMessage> DequeueAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

