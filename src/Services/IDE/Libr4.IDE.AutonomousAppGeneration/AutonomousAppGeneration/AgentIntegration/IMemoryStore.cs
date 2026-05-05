namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IMemoryStore
{
    Task IngestAsync(MemoryRecord record, CancellationToken ct);

    Task<IReadOnlyList<MemoryRetrievalResult>> RetrieveAsync(MemoryQuery query, CancellationToken ct);

    Task PruneAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct);
}
