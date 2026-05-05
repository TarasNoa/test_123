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
        
        var entity = AgentEventEntity.FromDomain(evt);

        context.AgentEvents.Add(entity);
        await context.SaveChangesAsync(ct);

        _logger.LogDebug("Saved agent event {EventType} for run {RunId}", evt.Type, evt.RunId);
    }

    public async Task<AgentEvent[]> GetEventsForRunAsync(Guid runId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entities = await context.AgentEvents
            .AsNoTracking()
            .Where(e => e.RunId == runId)
            .OrderBy(e => e.Timestamp)
            .ToArrayAsync(ct);

        return entities.Select(e => e.ToDomain()).ToArray();
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
