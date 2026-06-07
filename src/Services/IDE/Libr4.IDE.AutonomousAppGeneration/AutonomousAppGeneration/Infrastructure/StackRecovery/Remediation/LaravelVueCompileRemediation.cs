using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class LaravelVueCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[LARAVEL_VUE_FULLSTACK]]", StringComparison.Ordinal)
            && !StackPlanHeuristics.IsPhp(plan))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "AuthController", "backend/");
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "api.php", "routes/");
        changed += GoldenPathCompileRemediationBase.DedupeConcatenatedSourceFiles(files, ".php");
        changed += UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        return changed;
    }
}
