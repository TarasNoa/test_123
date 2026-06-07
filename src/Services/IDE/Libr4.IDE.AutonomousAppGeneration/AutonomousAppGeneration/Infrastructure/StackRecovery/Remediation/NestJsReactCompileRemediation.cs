using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class NestJsReactCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[NESTJS_REACT_FULLSTACK]]", StringComparison.Ordinal)
            && !plan.TechStack.Frameworks.Any(f => f.Contains("nestjs", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "main.ts", "backend/");
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "app.module.ts", "backend/");
        changed += UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        return changed;
    }
}
