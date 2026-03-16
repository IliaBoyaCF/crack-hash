using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface IRequestQueue
{
    int Count { get; }

    int Capacity { get; }

    bool TryEnqueue(IRequestInfo request);

    ValueTask EnqueueAsync(IRequestInfo request, CancellationToken cancellationToken = default);

    bool TryDequeue(out IRequestInfo? request);

    ValueTask<IRequestInfo> DequeueAsync(CancellationToken cancellationToken = default);

    public IAsyncEnumerable<IRequestInfo> DequeueAllAsync(CancellationToken cancellationToken = default);
}
