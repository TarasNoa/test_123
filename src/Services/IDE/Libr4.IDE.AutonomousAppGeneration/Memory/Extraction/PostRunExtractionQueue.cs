using System.Threading.Channels;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public interface IPostRunExtractionQueue
{
    bool TryEnqueue(Guid runId);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}

public sealed class BoundedPostRunExtractionQueue : IPostRunExtractionQueue
{
    private readonly Channel<Guid> _channel;

    public BoundedPostRunExtractionQueue(PostRunExtractionOptions? options = null)
    {
        var capacity = Math.Max(1, options?.QueueCapacity ?? 64);
        _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(Guid runId) => _channel.Writer.TryWrite(runId);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
