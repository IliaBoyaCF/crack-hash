using Manager.Abstractions.Model;
using Manager.Abstractions.Options;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Manager.Service;

public class RequestQueue : IRequestQueue
{

    private readonly Channel<IRequestInfo> _channel;

    public RequestQueue(IOptions<RequestQueueOptions> options)
    {
        Capacity = options.Value.Capacity;
        _channel = Channel.CreateBounded<IRequestInfo>
        (
            new BoundedChannelOptions(options.Value.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }
        );
    }

    public int Count => _channel.Reader.Count;

    public int Capacity { get; init; }

    public IAsyncEnumerable<IRequestInfo> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask<IRequestInfo> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask EnqueueAsync(IRequestInfo request, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public bool TryDequeue(out IRequestInfo? request)
    {
        return _channel.Reader.TryRead(out request);
    }

    public bool TryEnqueue(IRequestInfo request)
    {
        return _channel.Writer.TryWrite(request);
    }
}
