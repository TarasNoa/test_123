using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Routes repair work to the correct tier: Level 0 structural (deterministic) before LLM fixer.
/// </summary>
public static class RepairErrorClassifier
{
    public enum RepairTier
    {
        Level0Structural = 0,
        Level1BuildManifest = 1,
        Level2Compile = 2,
        Level3Runtime = 3,
        Level4BusinessLogic = 4
    }

    public enum RepairErrorClass
    {
        PomSyntax,
        CsprojSyntax,
        PackageJsonSyntax,
        YamlSyntax,
        RequirementsSyntax,
        ArtifactContamination,
        MissingDependency,
        CompileSymbol,
        TestFailure,
        RuntimeConfiguration,
        RuntimeDiFailure,
        Unknown
    }

    public sealed record ClassifiedError(RepairErrorClass Class, RepairTier Tier, ErrorReport Source);

    public static IReadOnlyList<ClassifiedError> Classify(
        IReadOnlyList<ErrorReport> errors,
        string? buildLog)
    {
        return errors
            .Select(e => new ClassifiedError(ClassifyOne(e, buildLog), TierFor(ClassifyOne(e, buildLog)), e))
            .ToList();
    }

    /// <summary>
    /// Applies deterministic Level 0/1 fixes and returns changed files (patch list).
    /// </summary>
    public static IReadOnlyList<GeneratedFile> ApplyLevel0Recovery(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog)
    {
        var working = currentFiles.ToList();
        var changed = ManifestRepairEngine.RepairAll(working, plan, buildLog) > 0;
        changed |= StructuralArtifactValidator.ValidateAndFix(working, plan).AutoFixesApplied > 0;
        changed |= ProjectArtifactNormalizer.Normalize(working, plan, autoFix: true).AutoFixesApplied > 0;
        changed |= StackArtifactRecoveryRouter.ApplyStructuralRecovery(working, plan, buildLog) > 0;

        if (!changed)
            return Array.Empty<GeneratedFile>();

        return DiffPatches(currentFiles, working);
    }

    public static bool ShouldSkipLlmFixer(IReadOnlyList<ClassifiedError> classified) =>
        classified.Count > 0
        && classified.All(c => c.Tier <= RepairTier.Level1BuildManifest);

    public static bool ShouldSkipLlmForRuntime(IReadOnlyList<ClassifiedError> classified) =>
        classified.Count > 0
        && classified.All(c => c.Tier == RepairTier.Level3Runtime);

    public static IReadOnlyList<GeneratedFile> ApplyLevel3Recovery(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog)
    {
        var classes = Classify(errors, buildLog)
            .Select(c => c.Class)
            .Distinct()
            .ToList();

        if (!classes.Any(c => c is RepairErrorClass.RuntimeConfiguration or RepairErrorClass.RuntimeDiFailure)
            && !RuntimeRecoveryService.IsRuntimeFailure(buildLog))
            return Array.Empty<GeneratedFile>();

        return StackArtifactRecoveryRouter.ApplyRuntimeRecovery(currentFiles, plan, errors, buildLog);
    }

    /// <summary>
    /// Level 2: compile-symbol intelligence (missing types, imports, package alignment) before LLM.
    /// </summary>
    public static IReadOnlyList<GeneratedFile> ApplyLevel2CompileRecovery(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        CompileRepairPlanner.RepairPlan repairPlan,
        string? buildLog) =>
        CompileSymbolRecovery.TryApply(currentFiles, plan, repairPlan, buildLog);

    public static bool ShouldPreferLevel2CompileRecovery(CompileRepairPlanner.RepairPlan repairPlan) =>
        CompileSymbolRecovery.ShouldPreferDeterministic(repairPlan.SymbolAnalysis);

    private static RepairErrorClass ClassifyOne(ErrorReport error, string? buildLog)
    {
        var signal = $"{error.ErrorType} {error.Message} {error.FilePath} {buildLog}";
        if (signal.Contains("Duplicated tag", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Non-parseable POM", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("unrecognized tag", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("groupid", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("artifactid", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("modelversion", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(error.FilePath, "backend/pom.xml", StringComparison.OrdinalIgnoreCase)
                && signal.Contains("pom", StringComparison.OrdinalIgnoreCase)))
            return RepairErrorClass.PomSyntax;

        if (signal.Contains(".csproj", StringComparison.OrdinalIgnoreCase)
            && (signal.Contains("error", StringComparison.OrdinalIgnoreCase)
                || signal.Contains("duplicate", StringComparison.OrdinalIgnoreCase)))
            return RepairErrorClass.CsprojSyntax;

        if (signal.Contains("package.json", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("JSON parse", StringComparison.OrdinalIgnoreCase))
            return RepairErrorClass.PackageJsonSyntax;

        if (signal.Contains("ERROR collecting", StringComparison.OrdinalIgnoreCase)
            || (signal.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
                && error.FilePath?.Contains("test", StringComparison.OrdinalIgnoreCase) == true))
            return RepairErrorClass.CompileSymbol;

        if (signal.Contains("requirements.txt", StringComparison.OrdinalIgnoreCase)
            && (error.FilePath?.Contains("requirements.txt", StringComparison.OrdinalIgnoreCase) == true
                || string.Equals(error.ErrorType, "RequirementsSyntax", StringComparison.OrdinalIgnoreCase)))
            return RepairErrorClass.RequirementsSyntax;

        if ((signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
             || signal.Contains("No module named", StringComparison.OrdinalIgnoreCase))
            && error.FilePath?.Contains("requirements.txt", StringComparison.OrdinalIgnoreCase) == true)
            return RepairErrorClass.RequirementsSyntax;

        if (signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("No module named", StringComparison.OrdinalIgnoreCase))
            return RepairErrorClass.CompileSymbol;

        if (signal.Contains("multiple application roots", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("SpringBootApplication", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Multiple .NET entry", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Multiple Python app", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Multiple Node server", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("JWT stack", StringComparison.OrdinalIgnoreCase))
            return RepairErrorClass.ArtifactContamination;

        if (string.Equals(error.ErrorType, "CompileError", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("compileerror", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("package does not exist", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("error CS", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("error TS", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("SyntaxError", StringComparison.OrdinalIgnoreCase))
            return RepairErrorClass.CompileSymbol;

        if (RuntimeRecoveryService.IsRuntimeFailure(signal))
        {
            if (signal.Contains("BeanCreation", StringComparison.OrdinalIgnoreCase)
                || signal.Contains("UnsatisfiedDependency", StringComparison.OrdinalIgnoreCase)
                || signal.Contains("NoSuchBeanDefinition", StringComparison.OrdinalIgnoreCase))
                return RepairErrorClass.RuntimeDiFailure;
            return RepairErrorClass.RuntimeConfiguration;
        }

        if (signal.Contains("test", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("assert", StringComparison.OrdinalIgnoreCase))
            return RepairErrorClass.TestFailure;

        if (signal.Contains("CS0246", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Cannot find module", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Module not found", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("ERR_MODULE_NOT_FOUND", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("npm ERR!", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("dependency", StringComparison.OrdinalIgnoreCase))
            return RepairErrorClass.MissingDependency;

        return RepairErrorClass.Unknown;
    }

    private static RepairTier TierFor(RepairErrorClass cls) => cls switch
    {
        RepairErrorClass.PomSyntax or RepairErrorClass.CsprojSyntax or RepairErrorClass.PackageJsonSyntax
            or RepairErrorClass.YamlSyntax or RepairErrorClass.RequirementsSyntax
            or RepairErrorClass.ArtifactContamination => RepairTier.Level0Structural,
        RepairErrorClass.MissingDependency => RepairTier.Level1BuildManifest,
        RepairErrorClass.CompileSymbol => RepairTier.Level2Compile,
        RepairErrorClass.RuntimeConfiguration or RepairErrorClass.RuntimeDiFailure => RepairTier.Level3Runtime,
        RepairErrorClass.TestFailure => RepairTier.Level4BusinessLogic,
        RepairErrorClass.Unknown => RepairTier.Level4BusinessLogic,
        _ => RepairTier.Level4BusinessLogic
    };

    public static IReadOnlyList<GeneratedFile> DiffPatches(
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
