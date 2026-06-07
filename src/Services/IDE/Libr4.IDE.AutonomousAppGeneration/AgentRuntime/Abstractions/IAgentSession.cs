using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

public interface IAgentSession
{
    Task<AgentSessionResult> RunAsync(AgentSessionRunRequest request, CancellationToken ct = default);

    Task<AgentSessionResult> RunAsync(
        string objective,
        ShadowWorkspaceContext workspace,
        IList<GeneratedFile> workingFiles,
        GenerationPlan plan,
        IShadowWorkspaceAccessor accessor,
        string? buildLog,
        CancellationToken ct = default);

    Task<AgentSessionResult> ResumeAsync(string sessionId, AgentSessionRunRequest request, CancellationToken ct = default);

    Task<string> CheckpointAsync(string sessionId, IReadOnlyList<AgentConversationTurn> turns, CancellationToken ct = default);

    Task<IReadOnlyList<AgentConversationTurn>> RewindAsync(string sessionId, string checkpointId, CancellationToken ct = default);
}

public interface IShadowAgentRepairService
{
    Task<IReadOnlyList<GeneratedFile>> RunRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        Guid workspaceId,
        string buildLog,
        IReadOnlyList<ErrorReport> errors,
        Guid? runId = null,
        int repairAttempt = 1,
        string? tenantUserId = null,
        CancellationToken ct = default);
}
