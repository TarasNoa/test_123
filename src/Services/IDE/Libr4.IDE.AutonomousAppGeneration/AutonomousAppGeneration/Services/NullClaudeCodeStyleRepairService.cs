using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>No-op surgical repair for lightweight handler constructors / tests.</summary>
public sealed class NullClaudeCodeStyleRepairService : IClaudeCodeStyleRepairService
{
    public Task<IReadOnlyList<GeneratedFile>> TryRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GeneratedFile>>(Array.Empty<GeneratedFile>());
}
