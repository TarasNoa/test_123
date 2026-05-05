using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Storage abstraction for <see cref="AppGenerationOrchestrator"/> aggregates.
/// An in-memory implementation is provided for the first iteration; it can be
/// replaced with an EF Core persistence layer later without touching handlers.
/// </summary>
public interface IAppGenerationRepository
{
    Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default);
    Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default);
    Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// P2-3: returns runs belonging to a specific tenant, most recent first.
    /// When <paramref name="tenantId"/> is null this is equivalent to <see cref="ListAsync"/>.
    /// </summary>
    Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(
        string? tenantId,
        CancellationToken ct = default);
}
