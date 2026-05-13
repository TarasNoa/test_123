using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Shared.Infrastructure.Events;

public sealed class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly List<OutboxItem> _items = new();

    public Task AddAsync(OutboxItem item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<List<DomainEvent>> GetPendingAsync()
    {
        var pending = _items
            .Where(i => !i.IsProcessed)
            .Select(i => (DomainEvent)new SimpleDomainEvent(i.EventId))
            .ToList();
        return Task.FromResult(pending);
    }

    public Task MarkAsProcessedAsync(Guid eventId)
    {
        var item = _items.FirstOrDefault(i => i.EventId == eventId);
        if (item != null)
        {
            item.IsProcessed = true;
            item.ProcessedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    private sealed record SimpleDomainEvent(Guid EventId) : DomainEvent;
}
