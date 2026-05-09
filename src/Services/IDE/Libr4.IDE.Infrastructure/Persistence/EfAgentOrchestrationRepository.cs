using Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of agent orchestration repository.
/// Replaces InMemoryAgentOrchestrationRepository for production.
/// </summary>
public class EfAgentOrchestrationRepository : IAgentOrchestrationRepository
{
    private readonly IDbContextFactory<IdeDbContext> _contextFactory;
    private readonly ILogger<EfAgentOrchestrationRepository> _logger;

    public EfAgentOrchestrationRepository(
        IDbContextFactory<IdeDbContext> contextFactory,
        ILogger<EfAgentOrchestrationRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SaveAsync(AgentOrchestrationEvent evt, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = new AgentOrchestrationEntity
        {
            RunId = evt.RunId,
            JsonData = System.Text.Json.JsonSerializer.Serialize(evt),
            UpdatedAt = DateTime.UtcNow
        };

        var existing = await context.AgentOrchestrations
            .FirstOrDefaultAsync(o => o.RunId == evt.RunId, ct);

        if (existing == null)
        {
            entity.CreatedAt = DateTime.UtcNow;
            context.AgentOrchestrations.Add(entity);
            _logger.LogDebug("Created orchestration for run {RunId}", evt.RunId);
        }
        else
        {
            existing.JsonData = entity.JsonData;
            existing.UpdatedAt = DateTime.UtcNow;
            _logger.LogDebug("Updated orchestration for run {RunId}", evt.RunId);
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<AgentOrchestrationEvent?> GetOrchestrationAsync(Guid runId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = await context.AgentOrchestrations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.RunId == runId, ct);

        if (entity == null) return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<AgentOrchestrationEvent>(entity.JsonData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize orchestration for run {RunId}", runId);
            return null;
        }
    }

    public async Task ClearOrchestrationAsync(Guid runId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = await context.AgentOrchestrations
            .FirstOrDefaultAsync(o => o.RunId == runId, ct);

        if (entity != null)
        {
            context.AgentOrchestrations.Remove(entity);
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Cleared orchestration for run {RunId}", runId);
        }
    }
}
