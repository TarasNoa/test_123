using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class FastApiReactCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[FASTAPI_REACT_FULLSTACK]]", StringComparison.Ordinal)
            && !StackPlanHeuristics.IsPython(plan))
            return 0;

        var warnings = new List<string>();
        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "main.py", "backend/");
        changed += GoldenPathCompileRemediationBase.DedupeConcatenatedSourceFiles(files, ".py");
        changed += GoldenPathCompileRemediationBase.RemoveBrokenTestFiles(files, "backend/");
        changed += UniversalManifestFixes.FixRequirementsDuplicates(files, warnings, autoFix: true);
        return changed;
    }
}
