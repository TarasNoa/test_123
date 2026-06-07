using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Post-generation gate: structural validation then artifact normalization before any build.
/// </summary>
public static class ProjectValidationPipeline
{
    public sealed record PipelineResult(
        IReadOnlyList<string> Warnings,
        int StructuralFixes,
        int NormalizationFixes,
        bool HasContaminationWarnings);

    /// <summary>Normalization before safety-net merge to reduce Pass A + Pass B contamination.</summary>
    public static PipelineResult RunPreSafetyMerge(IList<GeneratedFile> files, GenerationPlan plan)
    {
        var structural = StructuralArtifactValidator.ValidateAndFix(files, plan);
        var norm = ProjectArtifactNormalizer.Normalize(files, plan, autoFix: true);
        return BuildResult(structural, norm);
    }

    public static PipelineResult RunPostGeneration(IList<GeneratedFile> files, GenerationPlan plan)
    {
        var structural = StructuralArtifactValidator.ValidateAndFix(files, plan);
        var norm = ProjectArtifactNormalizer.Normalize(files, plan, autoFix: true);

        return BuildResult(structural, norm);
    }

    private static PipelineResult BuildResult(
        StructuralArtifactValidator.ValidationResult structural,
        ProjectArtifactNormalizer.ContaminationReport norm)
    {
        var warnings = structural.Findings
            .Select(f => $"{f.Code}: {f.Message}")
            .Concat(norm.Warnings)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PipelineResult(
            warnings,
            structural.AutoFixesApplied,
            norm.AutoFixesApplied,
            norm.HasBlockingIssues);
    }
}
