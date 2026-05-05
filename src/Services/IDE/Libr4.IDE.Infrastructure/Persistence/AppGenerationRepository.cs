using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of App Generation Repository.
/// Replaces InMemoryAppGenerationRepository for production.
/// </summary>
public class AppGenerationRepository : IAppGenerationRepository
{
    private readonly IDbContextFactory<IdeDbContext> _contextFactory;
    private readonly ILogger<AppGenerationRepository> _logger;

    public AppGenerationRepository(
        IDbContextFactory<IdeDbContext> contextFactory,
        ILogger<AppGenerationRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<AppGeneration?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        return await context.AppGenerations
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task SaveAsync(AppGeneration generation, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var existing = await context.AppGenerations
            .FirstOrDefaultAsync(a => a.Id == generation.Id, ct);
        
        if (existing == null)
        {
            context.AppGenerations.Add(generation);
            _logger.LogInformation("Created new app generation {GenerationId}", generation.Id);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(generation);
            existing.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Updated app generation {GenerationId}", generation.Id);
        }
        
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AppGeneration>> GetByUserAsync(
        Guid userId, 
        int skip = 0, 
        int take = 20,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        return await context.AppGenerations
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var generation = await context.AppGenerations
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        
        if (generation != null)
        {
            context.AppGenerations.Remove(generation);
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted app generation {GenerationId}", id);
        }
    }
}

/// <summary>
/// Entity for App Generation persistence.
/// </summary>
public class AppGenerationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
