using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class NextJsFullStackCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[NEXTJS_FULLSTACK]]", StringComparison.Ordinal)
            && !plan.TechStack.Frameworks.Any(f => f.Contains("next", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "page.tsx", "app/");
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "index.tsx", "pages/");
        changed += UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        return changed;
    }
}
