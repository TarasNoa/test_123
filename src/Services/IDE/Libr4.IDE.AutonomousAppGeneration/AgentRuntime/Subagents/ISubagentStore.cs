namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public sealed record SubagentRecord(
    string Id,
    Guid RunId,
    string Name,
    string Task,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? OutputPreview,
    string? Error);

public interface ISubagentStore
{
    Task<SubagentRecord> CreateAsync(Guid runId, string name, string task, AgentSpec? spec, CancellationToken ct = default);
    Task AppendMessageAsync(Guid runId, string subagentId, string role, string content, CancellationToken ct = default);
    Task CompleteAsync(Guid runId, string subagentId, string output, CancellationToken ct = default);
    Task FailAsync(Guid runId, string subagentId, string error, CancellationToken ct = default);
    Task<IReadOnlyList<SubagentRecord>> ListAsync(Guid runId, CancellationToken ct = default);
    Task<SubagentRecord?> GetAsync(Guid runId, string subagentId, CancellationToken ct = default);
}
