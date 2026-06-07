namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IAgentFleetRegistry
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AgentFleetSummary>> ListAsync(AgentFleetListQuery query, CancellationToken ct = default);
    Task<AgentFleetRunDetail?> GetSummaryAsync(Guid runId, CancellationToken ct = default);
    Task UpsertFromRunAsync(Guid runId, CancellationToken ct = default);
    Task PatchAsync(Guid runId, AgentFleetPatchRequest patch, CancellationToken ct = default);
    Task BulkArchiveAsync(AgentFleetBulkArchiveRequest request, CancellationToken ct = default);
    Task WriteAuditAsync(string action, Guid runId, string? actor, CancellationToken ct = default);
    Task<int> RebuildIndexAsync(CancellationToken ct = default);
    event Func<AgentFleetStatusEvent, Task>? StatusChanged;
}
