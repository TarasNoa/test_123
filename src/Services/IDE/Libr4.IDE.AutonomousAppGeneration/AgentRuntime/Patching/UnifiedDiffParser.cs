using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Microsoft.FSharp.Core;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;

public static class UnifiedDiffParser
{
    public static UnifiedDiff Parse(string patch, string? fallbackPath = null)
    {
        var dto = FSharpAlgorithmsBridge.ParseUnifiedDiff(patch, fallbackPath);
        return new UnifiedDiff(
            OptionModule.IsSome(dto.TargetPath) ? dto.TargetPath.Value : null,
            dto.Hunks.Select(h => new DiffHunk(h.OldStart, h.OldCount, h.NewStart, h.NewCount, h.Lines)).ToList());
    }
}
