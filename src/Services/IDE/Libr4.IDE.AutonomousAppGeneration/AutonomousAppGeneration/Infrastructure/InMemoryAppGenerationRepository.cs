using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public sealed class InMemoryAppGenerationRepository : IAppGenerationRepository
{
    private readonly List<AppGenerationOrchestrator> _orchestrators = new();
    private readonly object _lock = new();

    public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);

        lock (_lock)
        {
            var existingIndex = _orchestrators.FindIndex(o => o.Id == orchestrator.Id);
            if (existingIndex >= 0)
            {
                _orchestrators[existingIndex] = orchestrator;
            }
            else
            {
                _orchestrators.Add(orchestrator);
            }
        }

        return Task.CompletedTask;
    }

    public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_orchestrators.FirstOrDefault(o => o.Id == id));
        }
    }

    public Task<AppGenerationOrchestrator?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => GetAsync(id, ct);

    public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(
                _orchestrators
                    .Where(o => string.Equals(o.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                    .OrderByDescending(o => o.StartedAt)
                    .FirstOrDefault());
        }
    }

    public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(_orchestrators.ToList());
        }
    }

    public Task<IReadOnlyList<AppGenerationOrchestrator>> GetAllAsync(CancellationToken ct = default)
        => ListAsync(ct);

    public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var result = tenantId is null
                ? _orchestrators.ToList()
                : _orchestrators.Where(o => string.Equals(o.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)).ToList();

            return Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(result);
        }
    }
}
