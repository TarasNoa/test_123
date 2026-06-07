using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

public static class RustAxumCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, IReadOnlyList<ErrorReport> errors)
    {
        if (!plan.ApplicationDescription.Contains("[[RUST_AXUM_BACKEND]]", StringComparison.Ordinal)
            && !StackPlanHeuristics.IsRust(plan))
            return 0;

        var changed = 0;
        changed += GoldenPathCompileRemediationBase.RemoveDuplicateByPathContains(files, "main.rs", "backend/");
        changed += GoldenPathCompileRemediationBase.DedupeConcatenatedSourceFiles(files, ".rs");
        changed += EnsureAxumDependency(files);
        return changed;
    }

    private static int EnsureAxumDependency(IList<GeneratedFile> files)
    {
        var cargo = files.FirstOrDefault(f => f.RelativePath.EndsWith("Cargo.toml", StringComparison.OrdinalIgnoreCase));
        if (cargo?.Content?.Contains("axum", StringComparison.OrdinalIgnoreCase) == true)
            return 0;
        if (cargo is null)
            return 0;

        var content = cargo.Content ?? string.Empty;
        if (!content.Contains("[dependencies]", StringComparison.Ordinal))
            content += "\n[dependencies]\n";
        if (!content.Contains("axum", StringComparison.Ordinal))
            content += "axum = \"0.7\"\ntokio = { version = \"1\", features = [\"full\"] }\n";

        cargo.Update(content);
        return 1;
    }
}
