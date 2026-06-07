using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class DjangoCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[DJANGO_BACKEND]]", StringComparison.Ordinal)
            && !plan.ApplicationDescription.Contains("[[DJANGO_SOLIDJS_FULLSTACK]]", StringComparison.Ordinal)
            && !plan.TechStack.Frameworks.Any(f => f.Contains("django", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "settings.py", "backend/");
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "urls.py", "backend/");
        changed += GoldenPathCompileRemediationBase.DedupeConcatenatedSourceFiles(files, ".py");
        return changed;
    }
}
