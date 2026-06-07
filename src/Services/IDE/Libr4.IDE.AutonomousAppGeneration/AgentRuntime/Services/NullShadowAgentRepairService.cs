using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Services;

public sealed class NullShadowAgentRepairService : IShadowAgentRepairService
{
    public Task<IReadOnlyList<GeneratedFile>> RunRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        Guid workspaceId,
        string buildLog,
        IReadOnlyList<ErrorReport> errors,
        Guid? runId = null,
        int repairAttempt = 1,
        string? tenantUserId = null,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GeneratedFile>>(Array.Empty<GeneratedFile>());
}
