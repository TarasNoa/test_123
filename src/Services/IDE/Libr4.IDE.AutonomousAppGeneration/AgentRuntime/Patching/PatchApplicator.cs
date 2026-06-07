using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;

public static class PatchApplicator
{
    public static PatchApplyResult ApplyExact(string original, UnifiedDiff diff) =>
        FSharpAlgorithmsBridge.ToPatchResult(
            FSharpAlgorithmsBridge.ApplyExact(original, FSharpAlgorithmsBridge.ToFSharpDiff(diff)));

    public static PatchApplyResult ApplyFuzzy(string original, UnifiedDiff diff) =>
        FSharpAlgorithmsBridge.ToPatchResult(
            FSharpAlgorithmsBridge.ApplyFuzzy(original, FSharpAlgorithmsBridge.ToFSharpDiff(diff)));

    public static PatchApplyResult ApplyThreeWay(string original, string? baseContent, UnifiedDiff diff) =>
        FSharpAlgorithmsBridge.ToPatchResult(
            FSharpAlgorithmsBridge.ApplyThreeWay(original, baseContent, FSharpAlgorithmsBridge.ToFSharpDiff(diff)));

    public static GeneratedFile? ToGeneratedFile(string path, PatchApplyResult result, GeneratedFile? existing)
    {
        if (!result.Success || result.PatchedContent is null)
            return null;
        return new GeneratedFile(path, existing?.Language, result.PatchedContent);
    }
}
