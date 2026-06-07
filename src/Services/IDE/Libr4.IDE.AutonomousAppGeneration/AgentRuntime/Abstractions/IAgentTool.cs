using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

public sealed class ToolContext
{
    public required ShadowWorkspaceContext Workspace { get; init; }
    public required IShadowWorkspaceAccessor Accessor { get; init; }
    public required IList<GeneratedFile> WorkingFiles { get; init; }
    public required IFileStateCache FileState { get; init; }
    public GenerationPlan? Plan { get; init; }
    public string? BuildLog { get; init; }
    public AgentSessionMode Mode { get; init; } = AgentSessionMode.Repair;
    public required AgentSessionState Session { get; init; }
    public JsonElement ToolInput { get; init; }
    public IReadOnlyList<string>? AllowedTools { get; init; }
}

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    bool IsReadOnly { get; }
    bool IsConcurrencySafe(JsonElement input);
    Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct);
}

public interface IAgentToolRegistry
{
    IReadOnlyList<IAgentTool> All { get; }
    IAgentTool? TryGet(string name);
    string BuildToolCatalog();
}

public interface IFileStateCache
{
    bool HasRead(string relativePath);
    void RecordRead(string relativePath, string content, DateTime? lastWriteUtc = null);
    bool IsStale(string relativePath, DateTime lastWriteUtc);
}

public interface IPermissionGate
{
    ValueTask<PermissionDecision> EvaluateAsync(IAgentTool tool, JsonElement input, ToolContext context, CancellationToken ct);
}

public enum PermissionDecisionKind
{
    Allow,
    Deny
}

public sealed record PermissionDecision(PermissionDecisionKind Kind, string? Reason = null);

public interface IContextCompactor
{
    Task<IReadOnlyList<AgentConversationTurn>> CompactAsync(
        IReadOnlyList<AgentConversationTurn> turns,
        int charBudget,
        CompactionRequest? request = null,
        CancellationToken ct = default);
}
