using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>
/// Multi-ecosystem recovery: catalog (~50 languages + ~70 frameworks) + deep handlers for Java/.NET/Node/Python.
/// </summary>
public static class StackArtifactRecoveryRouter
{
    public sealed record NormalizationReport(
        StackKind Stack,
        IReadOnlyList<EcosystemMatch> MatchedEcosystems,
        IReadOnlyList<string> Warnings,
        int FixesApplied,
        bool HasContaminationWarnings);

    public static StackKind ResolveStack(GenerationPlan plan) => StackPlanHeuristics.Classify(plan);

    public static string DescribeStack(GenerationPlan plan) => ResolveStack(plan).ToString();

    public static IReadOnlyList<EcosystemMatch> MatchEcosystems(GenerationPlan plan, IReadOnlyList<GeneratedFile> files) =>
        EcosystemMatcher.Match(plan, files);

    public static NormalizationReport Normalize(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        bool autoFix = true)
    {
        var stack = ResolveStack(plan);
        var warnings = new List<string> { $"stack={stack}" };
        var fileList = files as IReadOnlyList<GeneratedFile> ?? files.ToList();
        var matches = EcosystemMatcher.Match(plan, fileList);
        var fixes = UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        fixes += PatternBasedEcosystemRecovery.Normalize(files, plan, warnings, autoFix, matches);
        fixes += ApplyDeepHandlers(files, plan, warnings, autoFix, stack, matches);

        return new NormalizationReport(
            stack,
            matches,
            warnings,
            fixes,
            warnings.Count > 1);
    }

    public static StructuralArtifactValidator.ValidationResult ValidateStructural(IList<GeneratedFile> files, GenerationPlan plan)
    {
        var result = StructuralArtifactValidator.ValidateStackManifests(files, plan);
        var warnings = result.Findings.Select(f => f.Message).ToList();
        PatternBasedEcosystemRecovery.Normalize(files, plan, warnings, autoFix: false);
        return result;
    }

    public static int ApplyStructuralRecovery(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog)
    {
        var fileList = files as IReadOnlyList<GeneratedFile> ?? files.ToList();
        var matches = EcosystemMatcher.Match(plan, fileList);
        var warnings = new List<string>();
        var fixes = PatternBasedEcosystemRecovery.Normalize(files, plan, warnings, autoFix: true, matches);
        fixes += ApplyDeepStructuralHandlers(files, plan, buildLog, matches);
        return fixes;
    }

    public static int ApplyCompileRecovery(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog = null)
    {
        var fileList = files as IReadOnlyList<GeneratedFile> ?? files.ToList();
        var matches = EcosystemMatcher.Match(plan, fileList);
        var stack = ResolveStack(plan);
        var fixes = 0;
        if (ShouldRunJava(stack, matches))
            fixes += JavaStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunDotNet(stack, matches))
            fixes += DotNetStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunNode(stack, matches))
            fixes += NodeStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunPython(stack, matches))
            fixes += PythonStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunGo(stack, matches))
            fixes += GoStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunRust(stack, matches))
            fixes += RustStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunPhp(stack, matches))
            fixes += PhpStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        if (ShouldRunRuby(stack, matches))
            fixes += RubyStackArtifactRecovery.ApplyCompileFixes(files, plan, errors, buildLog);
        return fixes;
    }

    public static int ApplySecurityRemediation(IList<GeneratedFile> files, GenerationPlan plan)
    {
        var fileList = files as IReadOnlyList<GeneratedFile> ?? files.ToList();
        var matches = EcosystemMatcher.Match(plan, fileList);
        var stack = ResolveStack(plan);
        var fixes = 0;
        if (ShouldRunJava(stack, matches))
            fixes += JavaStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunDotNet(stack, matches))
            fixes += DotNetStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunNode(stack, matches))
            fixes += NodeStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunPython(stack, matches))
            fixes += PythonStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunGo(stack, matches))
            fixes += GoStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunRust(stack, matches))
            fixes += RustStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunPhp(stack, matches))
            fixes += PhpStackArtifactRecovery.ApplySecurityFixes(files, plan);
        if (ShouldRunRuby(stack, matches))
            fixes += RubyStackArtifactRecovery.ApplySecurityFixes(files, plan);
        return fixes;
    }

    public static IReadOnlyList<GeneratedFile> ApplyRuntimeRecovery(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog)
    {
        if (!RuntimeRecoveryService.IsRuntimeFailure(
                string.Join('\n', errors.Select(e => e.Message)) + "\n" + (buildLog ?? string.Empty)))
            return Array.Empty<GeneratedFile>();

        var working = currentFiles.ToList();
        var matches = EcosystemMatcher.Match(plan, (IReadOnlyList<GeneratedFile>)working);
        var changed = ApplyDeepRuntimeHandlers(working, plan, buildLog, matches);
        if (changed == 0)
            changed = RuntimeRecoveryService.ApplyGenericRuntimeFixes(working, buildLog) > 0 ? 1 : 0;

        if (changed == 0)
            return Array.Empty<GeneratedFile>();

        return DiffPatches(currentFiles, working);
    }

    private static int ApplyDeepHandlers(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> warnings,
        bool autoFix,
        StackKind stack,
        IReadOnlyList<EcosystemMatch> matches)
    {
        if (!autoFix)
            return 0;

        var fixes = 0;
        if (ShouldRunJava(stack, matches))
            fixes += JavaStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunDotNet(stack, matches))
            fixes += DotNetStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunNode(stack, matches))
            fixes += NodeStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunPython(stack, matches))
            fixes += PythonStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunGo(stack, matches))
            fixes += GoStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunRust(stack, matches))
            fixes += RustStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunPhp(stack, matches))
            fixes += PhpStackArtifactRecovery.Normalize(files, plan, warnings, true);
        if (ShouldRunRuby(stack, matches))
            fixes += RubyStackArtifactRecovery.Normalize(files, plan, warnings, true);
        return fixes;
    }

    private static int ApplyDeepStructuralHandlers(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        string? buildLog,
        IReadOnlyList<EcosystemMatch> matches)
    {
        var stack = ResolveStack(plan);
        var fixes = 0;
        if (ShouldRunJava(stack, matches))
            fixes += JavaStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunDotNet(stack, matches))
            fixes += DotNetStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunNode(stack, matches))
            fixes += NodeStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunPython(stack, matches))
            fixes += PythonStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunGo(stack, matches))
            fixes += GoStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunRust(stack, matches))
            fixes += RustStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunPhp(stack, matches))
            fixes += PhpStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        if (ShouldRunRuby(stack, matches))
            fixes += RubyStackArtifactRecovery.ApplyStructuralFixes(files, plan, buildLog);
        return fixes;
    }

    private static int ApplyDeepRuntimeHandlers(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        string? buildLog,
        IReadOnlyList<EcosystemMatch> matches)
    {
        var stack = ResolveStack(plan);
        var fixes = 0;
        if (ShouldRunJava(stack, matches))
            fixes += JavaStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunDotNet(stack, matches))
            fixes += DotNetStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunNode(stack, matches))
            fixes += NodeStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunPython(stack, matches))
            fixes += PythonStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunGo(stack, matches))
            fixes += GoStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunRust(stack, matches))
            fixes += RustStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunPhp(stack, matches))
            fixes += PhpStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        if (ShouldRunRuby(stack, matches))
            fixes += RubyStackArtifactRecovery.ApplyRuntimeFixes(files, plan, buildLog);
        return fixes;
    }

    private static bool ShouldRunJava(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack is StackKind.Java or StackKind.JavaReactFullStack
        || matches.Any(m => m.Profile.Id is "java" or "kotlin" or "spring-boot" or "quarkus" or "micronaut");

    private static bool ShouldRunDotNet(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack == StackKind.DotNet || matches.Any(m => m.Profile.Id is "csharp" or "aspnet-core" or "blazor" or "fsharp");

    private static bool ShouldRunNode(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack is StackKind.Node or StackKind.JavaReactFullStack
        || matches.Any(m => m.Profile.Category is EcosystemCategory.FrontendFramework or EcosystemCategory.FullStack
            || m.Profile.Id is "javascript" or "typescript" or "express" or "nestjs" or "nextjs" or "nuxt"
                or "react" or "vue" or "sveltekit" or "remix" or "tanstack-start");

    private static bool ShouldRunPython(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack == StackKind.Python
        || matches.Any(m => m.Profile.Id is "python" or "fastapi" or "django" or "flask");

    private static bool ShouldRunGo(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack is StackKind.Go or StackKind.GoReactFullStack
        || matches.Any(m => m.Profile.Id is "go" or "gin" or "echo-go" or "fiber-go" or "chi-go");

    private static bool ShouldRunRust(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack == StackKind.Rust
        || matches.Any(m => m.Profile.Id is "rust" or "axum" or "actix" or "rocket" or "warp");

    private static bool ShouldRunPhp(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack is StackKind.Php or StackKind.PhpVueFullStack
        || matches.Any(m => m.Profile.Id is "php" or "laravel" or "symfony" or "yii");

    private static bool ShouldRunRuby(StackKind stack, IReadOnlyList<EcosystemMatch> matches) =>
        stack == StackKind.Ruby
        || matches.Any(m => m.Profile.Id is "ruby" or "rails" or "sinatra" or "hanami");

    private static IReadOnlyList<GeneratedFile> DiffPatches(
        IReadOnlyList<GeneratedFile> before,
        IReadOnlyList<GeneratedFile> after) =>
        after
            .Where(candidate =>
            {
                var existing = before.FirstOrDefault(f =>
                    f.RelativePath.Equals(candidate.RelativePath, StringComparison.OrdinalIgnoreCase));
                return existing is null
                       || !string.Equals(existing.Content, candidate.Content, StringComparison.Ordinal);
            })
            .ToList();
}
