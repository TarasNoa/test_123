using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of agent event repository.
/// Replaces InMemoryAgentEventRepository for production use.
/// </summary>
public class EfAgentEventRepository : IAgentEventRepository
{
    private readonly IDbContextFactory<IdeDbContext> _contextFactory;
    private readonly ILogger<EfAgentEventRepository> _logger;

    public EfAgentEventRepository(
        IDbContextFactory<IdeDbContext> contextFactory,
        ILogger<EfAgentEventRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SaveAsync(AgentEvent evt, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = new AgentEventEntity
        {
            Id = evt.Id,
            RunId = evt.RunId,
            Type = evt.Type,
            Timestamp = evt.Timestamp,
            Data = System.Text.Json.JsonSerializer.Serialize(evt.Data),
            CreatedAt = DateTime.UtcNow
        };

        context.AgentEvents.Add(entity);
        await context.SaveChangesAsync(ct);

        _logger.LogDebug("Saved agent event {EventType} for run {RunId}", evt.Type, evt.RunId);
    }

    public async Task<AgentEvent[]> GetEventsForRunAsync(Guid runId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entities = await context.AgentEvents
            .Where(e => e.RunId == runId)
            .OrderBy(e => e.Timestamp)
            .ToArrayAsync(ct);

        return entities.Select(e => new AgentEvent
        {
            Id = e.Id,
            RunId = e.RunId,
            Type = e.Type,
            Timestamp = e.Timestamp,
            Data = System.Text.Json.JsonSerializer.Deserialize<object>(e.Data) ?? new()
        }).ToArray();
    }

    public async Task ClearEventsForRunAsync(Guid runId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var events = await context.AgentEvents
            .Where(e => e.RunId == runId)
            .ToListAsync(ct);

        context.AgentEvents.RemoveRange(events);
        await context.SaveChangesAsync(ct);

        _logger.LogDebug("Cleared {Count} events for run {RunId}", events.Count, runId);
    }
}

/// <summary>
/// EF Core entity for agent events.
/// </summary>
public class AgentEventEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Type { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Data { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
