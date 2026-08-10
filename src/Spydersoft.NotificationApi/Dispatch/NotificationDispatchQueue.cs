using System.Threading.Channels;

namespace Spydersoft.NotificationApi.Dispatch;

public sealed class NotificationDispatchQueue
{
    private readonly Channel<DispatchItem> _channel = Channel.CreateUnbounded<DispatchItem>();

    public ValueTask EnqueueAsync(DispatchItem item, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    public IAsyncEnumerable<DispatchItem> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
