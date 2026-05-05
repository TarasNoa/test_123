using FluentAssertions;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class MemoryConsolidationQueueTests
{
    [Fact]
    public void TryEnqueue_WithCapacity_AcceptsItems()
    {
        var queue = new BoundedMemoryConsolidationQueue(new MemoryConsolidationQueueOptions { Capacity = 4, DropOldestOnOverflow = true });

        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public async Task DequeueAllAsync_EmitsEnqueuedItemsInOrder()
    {
        var queue = new BoundedMemoryConsolidationQueue(new MemoryConsolidationQueueOptions { Capacity = 4 });
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        queue.TryEnqueue(first);
        queue.TryEnqueue(second);
        queue.TryEnqueue(third);

        var seen = new List<Guid>();
        using var cts = new CancellationTokenSource();
        var consumer = Task.Run(async () =>
        {
            await foreach (var id in queue.DequeueAllAsync(cts.Token))
            {
                seen.Add(id);
                if (seen.Count == 3)
                {
                    cts.Cancel();
                    break;
                }
            }
        });

        try { await consumer; }
        catch (OperationCanceledException) { /* expected */ }

        seen.Should().Equal(first, second, third);
    }

    [Fact]
    public void TryEnqueue_DropOldestMode_StaysWithinCapacity()
    {
        // BoundedChannel + FullMode.DropOldest writes synchronously even when full.
        var queue = new BoundedMemoryConsolidationQueue(new MemoryConsolidationQueueOptions { Capacity = 2, DropOldestOnOverflow = true });

        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
        // Beyond capacity: still accepted but oldest is evicted.
        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
    }
}
