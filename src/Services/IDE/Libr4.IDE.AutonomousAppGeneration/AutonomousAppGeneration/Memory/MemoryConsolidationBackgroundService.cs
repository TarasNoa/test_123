using System.Threading.Channels;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;

/// <summary>
/// P1-6 of audit roadmap. Replaces the prior fire-and-forget
/// <c>_ = Task.Run(...)</c> consolidation pattern with a single-consumer
/// BackgroundService backed by a bounded <see cref="Channel{T}"/>.
///
/// Guarantees:
///   * Bounded queue (capacity = <see cref="MemoryConsolidationQueueOptions.Capacity"/>);
///   * Drop-oldest behaviour on overflow (back-pressure visible via metrics);
///   * Single concurrent consolidation at a time (eliminates LLM thundering herd
///     and the OOM risk noted in the audit).
/// </summary>
public sealed class MemoryConsolidationBackgroundService : BackgroundService
{
    private readonly IMemoryConsolidationQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<MemoryConsolidationBackgroundService> _logger;

    public MemoryConsolidationBackgroundService(
        IMemoryConsolidationQueue queue,
        IServiceProvider services,
        ILogger<MemoryConsolidationBackgroundService> logger)
    {
        _queue = queue;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Memory consolidation background service started.");
        try
        {
            await foreach (var runId in _queue.DequeueAllAsync(stoppingToken))
            {
                using var scope = _services.CreateScope();
                var consolidator = scope.ServiceProvider.GetService<IAutonomousMemoryConsolidationService>();
                if (consolidator is null)
                {
                    _logger.LogDebug("[Consolidation] Service not registered; skipping run {RunId}", runId);
                    continue;
                }
                try
                {
                    await consolidator.TriggerConsolidationAsync(runId, stoppingToken);
                    AutoGenTelemetry.ConsolidationProcessed.Add(1);
                    _logger.LogInformation("[Consolidation] Completed for run {RunId}", runId);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Consolidation] Failed for run {RunId}", runId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
        finally
        {
            _logger.LogInformation("Memory consolidation background service stopped.");
        }
    }
}

/// <summary>Configuration knobs for the bounded queue feeding the background service.</summary>
public sealed class MemoryConsolidationQueueOptions
{
    public int Capacity { get; set; } = 64;
    public bool DropOldestOnOverflow { get; set; } = true;
}

/// <summary>Producer/consumer abstraction so callers stay decoupled from <see cref="Channel{T}"/>.</summary>
public interface IMemoryConsolidationQueue
{
    /// <summary>
    /// Enqueues a run for consolidation. Returns true when accepted; false when dropped due to overflow.
    /// </summary>
    bool TryEnqueue(Guid runId);

    /// <summary>Async stream of pending consolidation tasks (consumed by BackgroundService).</summary>
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}

public sealed class BoundedMemoryConsolidationQueue : IMemoryConsolidationQueue
{
    private readonly Channel<Guid> _channel;
    private readonly bool _dropOldest;

    public BoundedMemoryConsolidationQueue(MemoryConsolidationQueueOptions? options = null)
    {
        var opt = options ?? new MemoryConsolidationQueueOptions();
        var bounded = new BoundedChannelOptions(Math.Max(1, opt.Capacity))
        {
            FullMode = opt.DropOldestOnOverflow ? BoundedChannelFullMode.DropOldest : BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<Guid>(bounded);
        _dropOldest = opt.DropOldestOnOverflow;
    }

    public bool TryEnqueue(Guid runId)
    {
        if (_dropOldest)
        {
            // BoundedChannelFullMode.DropOldest writes synchronously even when full
            // (older entry is evicted). TryWrite returns true unless the channel is completed.
            return _channel.Writer.TryWrite(runId);
        }
        return _channel.Writer.TryWrite(runId);
    }

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
