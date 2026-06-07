using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class GoGinReactCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[GO_GIN_REACT_FULLSTACK]]", StringComparison.Ordinal)
            && !StackPlanHeuristics.IsGo(plan))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "main.go", "backend/");
        changed += GoldenPathCompileRemediationBase.DedupeConcatenatedSourceFiles(files, ".go");
        changed += GoldenPathCompileRemediationBase.RemoveBrokenTestFiles(files, "backend/");
        return changed;
    }
}
