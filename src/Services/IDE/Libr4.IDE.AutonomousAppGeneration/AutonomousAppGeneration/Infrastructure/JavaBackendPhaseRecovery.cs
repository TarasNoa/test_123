using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// When the Backend multi-agent phase returns no parseable JSON, inject a minimal Java backend tree from the stack safety-net.
/// </summary>
public static class JavaBackendPhaseRecovery
{
    public static List<GeneratedFile> RecoverMinimalBackend(GenerationPlan plan)
    {
        if (!StackPlanHeuristics.IsJava(plan))
            return new List<GeneratedFile>();

        var merged = GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, Array.Empty<GeneratedFile>());
        return merged
            .Where(f => f.RelativePath.StartsWith("backend/", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
