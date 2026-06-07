using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Post-generation normalization via <see cref="StackArtifactRecoveryRouter"/> (multi-stack).
/// </summary>
public static class ProjectArtifactNormalizer
{
    public sealed record ContaminationReport(
        IReadOnlyList<string> Warnings,
        int AutoFixesApplied,
        bool HasBlockingIssues);

    public static ContaminationReport Normalize(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        bool autoFix = true)
    {
        var report = StackArtifactRecoveryRouter.Normalize(files, plan, autoFix);
        return new ContaminationReport(
            report.Warnings,
            report.FixesApplied,
            report.HasContaminationWarnings);
    }
}
