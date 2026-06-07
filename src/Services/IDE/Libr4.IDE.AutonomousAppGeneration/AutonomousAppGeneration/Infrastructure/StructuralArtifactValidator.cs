using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Fast structural validation (Level 0) before build. Stack-specific checks are gated by <see cref="StackPlanHeuristics"/>.
/// </summary>
public static class StructuralArtifactValidator
{
    public sealed record Finding(string Code, string Message, string? FilePath, bool AutoFixable);

    public sealed record ValidationResult(
        IReadOnlyList<Finding> Findings,
        int AutoFixesApplied);

    public static ValidationResult ValidateAndFix(IList<GeneratedFile> files, GenerationPlan plan) =>
        ValidateStackManifests(files, plan, applyFix: true);

    public static ValidationResult ValidateOnly(IList<GeneratedFile> files, GenerationPlan plan) =>
        ValidateStackManifests(files, plan, applyFix: false);

    public static ValidationResult ValidateStackManifests(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        List<Finding>? findings = null,
        bool applyFix = true)
    {
        findings ??= new List<Finding>();
        var fixes = UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        var stack = StackArtifactRecoveryRouter.ResolveStack(plan);

        if (stack is StackKind.Java or StackKind.JavaReactFullStack)
        {
            fixes += ValidateMavenPom(files, plan, findings, applyFix);
            fixes += CountSpringBootMains(files, findings, applyFix);
        }

        if (stack is StackKind.DotNet)
            fixes += ValidateDuplicateCsprojRoots(files, findings, applyFix);

        if (stack is StackKind.Python)
            CountDuplicateRequirements(files, findings);

        if (stack is StackKind.Node or StackKind.JavaReactFullStack)
            CountDuplicatePackageJsonRoots(files, findings);

        return new ValidationResult(findings, fixes);
    }

    private static int ValidateMavenPom(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        List<Finding> findings,
        bool applyFix)
    {
        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        var buildCount = Regex.Matches(content, "<build>", RegexOptions.IgnoreCase).Count;
        if (buildCount <= 1)
            return 0;

        findings.Add(new Finding(
            "POM_DUPLICATE_BUILD_TAG",
            $"Duplicate <build> tag in {files[idx].RelativePath} ({buildCount} occurrences).",
            files[idx].RelativePath,
            AutoFixable: true));

        return applyFix && JavaStackArtifactRecovery.ApplyStructuralFixes(files, plan, null) > 0 ? 1 : 0;
    }

    private static int CountSpringBootMains(IList<GeneratedFile> files, List<Finding> findings, bool applyFix)
    {
        var mains = files
            .Where(f => f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase)
                        && (f.Content?.Contains("@SpringBootApplication", StringComparison.Ordinal) ?? false))
            .Select(f => f.RelativePath)
            .ToList();

        if (mains.Count <= 1)
            return 0;

        findings.Add(new Finding(
            "JAVA_MULTIPLE_SPRING_BOOT_MAIN",
            $"Multiple @SpringBootApplication classes: {string.Join(", ", mains)}",
            mains.FirstOrDefault(),
            AutoFixable: true));

        return 0;
    }

    private static int ValidateDuplicateCsprojRoots(IList<GeneratedFile> files, List<Finding> findings, bool applyFix)
    {
        var projects = files
            .Where(f => f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.RelativePath)
            .ToList();
        if (projects.Count <= 1)
            return 0;

        findings.Add(new Finding(
            "DOTNET_MULTIPLE_CSPROJ",
            $"Multiple .csproj roots: {string.Join(", ", projects)}",
            projects.FirstOrDefault(),
            AutoFixable: true));

        return 0;
    }

    private static void CountDuplicateRequirements(IList<GeneratedFile> files, List<Finding> findings)
    {
        var req = files.Where(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)).ToList();
        if (req.Count <= 1)
            return;

        findings.Add(new Finding(
            "PYTHON_MULTIPLE_REQUIREMENTS",
            $"Multiple requirements.txt: {string.Join(", ", req.Select(r => r.RelativePath))}",
            req.FirstOrDefault()?.RelativePath,
            AutoFixable: true));
    }

    private static void CountDuplicatePackageJsonRoots(IList<GeneratedFile> files, List<Finding> findings)
    {
        var pkgs = files.Where(f => f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)).ToList();
        if (pkgs.Count <= 1)
            return;

        findings.Add(new Finding(
            "NODE_MULTIPLE_PACKAGE_JSON",
            $"Multiple package.json files: {string.Join(", ", pkgs.Select(p => p.RelativePath))}",
            pkgs.FirstOrDefault()?.RelativePath,
            AutoFixable: true));
    }
}
