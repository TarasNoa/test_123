using Libr4.IDE.Application.AutonomousAppGeneration.Persistence.Entities;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Persistence;

/// <summary>
/// P2-1 of audit roadmap. Hybrid persistent repository:
///   * EF Core <see cref="AutoGenDbContext"/> stores metadata projection
///     (id / fingerprint / status / updated-at) so a host restart preserves
///     idempotency lookups (<see cref="FindLatestByFingerprintAsync"/>).
///   * Full domain state is delegated to an in-process repository (default
///     <see cref="Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.InMemoryAppGenerationRepository"/>).
///
/// When <see cref="AppGenerationOrchestrator"/> grows a snapshot/rehydrate API
/// (planned), <see cref="RunRegistryEntry.PayloadJson"/> will hold the full state.
/// </summary>
public sealed class EfCoreAppGenerationRepository : IAppGenerationRepository
{
    private readonly IDbContextFactory<AutoGenDbContext> _dbFactory;
    private readonly IAppGenerationRepository _inMemory;
    private readonly ILogger<EfCoreAppGenerationRepository> _logger;

    public EfCoreAppGenerationRepository(
        IDbContextFactory<AutoGenDbContext> dbFactory,
        IAppGenerationRepository inMemoryDelegate,
        ILogger<EfCoreAppGenerationRepository> logger)
    {
        _dbFactory = dbFactory;
        _inMemory = inMemoryDelegate;
        _logger = logger;
    }

    public async Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default)
    {
        // Try in-memory first (fast path); DB only validates the run was registered.
        var hot = await _inMemory.GetAsync(id, ct).ConfigureAwait(false);
        if (hot is not null) return hot;

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var record = await db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        if (record is null) return null;

        // No full snapshot yet: just signal that the run exists in DB but state is gone.
        _logger.LogInformation(
            "Run {RunId} found in DB but full state is missing in memory (host restart). Returning null until snapshot/rehydrate API lands.",
            id);
        return null;
    }

    public async Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(
        string requestFingerprint,
        CancellationToken ct = default)
    {
        var hot = await _inMemory.FindLatestByFingerprintAsync(requestFingerprint, ct).ConfigureAwait(false);
        if (hot is not null) return hot;

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var record = await db.Runs.AsNoTracking()
            .Where(r => r.Fingerprint == requestFingerprint)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (record is null) return null;

        _logger.LogInformation(
            "Fingerprint {Fingerprint} matched DB record {RunId} but state is gone (host restart). Returning null; new run will be issued.",
            requestFingerprint, record.Id);
        return null;
    }

    public async Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
    {
        // Always cache full state in memory (fast read path).
        await _inMemory.SaveAsync(orchestrator, ct).ConfigureAwait(false);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var existing = await db.Runs.FirstOrDefaultAsync(r => r.Id == orchestrator.Id, ct).ConfigureAwait(false);
            if (existing is null)
            {
                existing = new RunRegistryEntry
                {
                    Id = orchestrator.Id,
                    Fingerprint = orchestrator.RequestFingerprint ?? string.Empty,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                db.Runs.Add(existing);
            }
            existing.Fingerprint = orchestrator.RequestFingerprint ?? existing.Fingerprint;
            existing.Status = orchestrator.Status.ToString();
            existing.FailureReason = orchestrator.FailureReason;
            existing.ApplicationName = orchestrator.Plan?.ApplicationName ?? string.Empty;
            existing.IterationCount = orchestrator.Iterations.Count;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Persistence is best-effort: never crash the orchestrator on DB hiccups.
            _logger.LogError(ex, "Failed to persist orchestrator {RunId} metadata to EF; in-memory state preserved.", orchestrator.Id);
        }
    }

    public async Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default)
    {
        return await _inMemory.ListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(
        string? tenantId,
        CancellationToken ct = default)
    {
        // Full state is in memory; delegate to in-memory repository that performs tenant filter.
        return await _inMemory.ListByTenantAsync(tenantId, ct).ConfigureAwait(false);
    }
}
