using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class AspNetReactCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[ASPNET_REACT_FULLSTACK]]", StringComparison.Ordinal)
            && StackPlanHeuristics.Classify(plan) != StackKind.DotNet)
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "Program.cs", "backend/");
        changed += GoldenPathCompileRemediationBase.DedupeConcatenatedSourceFiles(files, ".cs");
        changed += GoldenPathCompileRemediationBase.RemoveBrokenTestFiles(files, "backend/");
        changed += CsprojPackageReconciler.ReconcilePackages(files) > 0 ? 1 : 0;
        return changed;
    }
}
