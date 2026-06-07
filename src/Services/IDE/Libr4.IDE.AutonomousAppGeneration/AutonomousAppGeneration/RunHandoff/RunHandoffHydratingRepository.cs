using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunHandoffHydratingRepository : IAppGenerationRepository
{
    private readonly InMemoryAppGenerationRepository _inner;
    private readonly string _runsRoot;

    public RunHandoffHydratingRepository(string runsRoot)
        : this(new InMemoryAppGenerationRepository(), runsRoot)
    {
    }

    public RunHandoffHydratingRepository(InMemoryAppGenerationRepository inner, string runsRoot)
    {
        _inner = inner;
        _runsRoot = runsRoot;
    }

    public async Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _inner.GetAsync(id, ct).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var hydrated = RunHandoffOrchestratorHydrator.TryHydrate(id, _runsRoot);
        if (hydrated is null)
            return null;

        await _inner.SaveAsync(hydrated, ct).ConfigureAwait(false);
        return hydrated;
    }

    public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
        _inner.FindLatestByFingerprintAsync(requestFingerprint, ct);

    public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default) =>
        _inner.SaveAsync(orchestrator, ct);

    public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
        _inner.ListAsync(ct);

    public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
        _inner.ListByTenantAsync(tenantId, ct);
}
