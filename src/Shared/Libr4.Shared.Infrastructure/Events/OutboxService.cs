using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Shared.Infrastructure.Events;

public interface IOutboxService
{
    Task AddAsync(DomainEvent @event);
    Task<List<DomainEvent>> GetPendingEventsAsync();
    Task MarkAsProcessedAsync(Guid eventId);
}

public class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _repository;

    public OutboxService(IOutboxRepository repository)
    {
        _repository = repository;
    }

    public async Task AddAsync(DomainEvent @event)
    {
        var outboxItem = new OutboxItem
        {
            Id = Guid.NewGuid(),
            EventId = @event.EventId,
            EventType = @event.GetType().Name,
            EventData = System.Text.Json.JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        await _repository.AddAsync(outboxItem);
    }

    public async Task<List<DomainEvent>> GetPendingEventsAsync()
    {
        return await _repository.GetPendingAsync();
    }

    public async Task MarkAsProcessedAsync(Guid eventId)
    {
        await _repository.MarkAsProcessedAsync(eventId);
    }
}

public interface IOutboxRepository
{
    Task AddAsync(OutboxItem item);
    Task<List<DomainEvent>> GetPendingAsync();
    Task MarkAsProcessedAsync(Guid eventId);
}

public class OutboxItem
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}