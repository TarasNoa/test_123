using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class ExpressCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[EXPRESS_BACKEND]]", StringComparison.Ordinal)
            && !plan.TechStack.Frameworks.Any(f => f.Contains("express", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "server", "backend/");
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "index.ts", "backend/");
        changed += UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        return changed;
    }
}
