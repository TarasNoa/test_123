using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Claude Code-style repair: build console log + numbered source context → surgical edits.
/// Works on in-memory files; shadow workspace sync happens in the orchestrator after patches apply.
/// </summary>
public interface IClaudeCodeStyleRepairService
{
    Task<IReadOnlyList<GeneratedFile>> TryRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        CancellationToken ct = default);
}
