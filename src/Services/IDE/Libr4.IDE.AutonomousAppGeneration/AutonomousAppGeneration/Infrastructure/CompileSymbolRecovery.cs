using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using static Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.CompileErrorAnalyzer;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Applies compile-symbol intelligence before LLM fixer (Level 2 deterministic path).
/// Routes by active stack via <see cref="StackArtifactRecoveryRouter"/>.
/// </summary>
public static class CompileSymbolRecovery
{
    public static bool ShouldPreferDeterministic(CompileErrorAnalysis? analysis) =>
        analysis?.Kind is CompileFixKind.MissingClass
            or CompileFixKind.MissingInterface
            or CompileFixKind.MissingImport
            or CompileFixKind.WrongImport
            or CompileFixKind.PackageMismatch
            or CompileFixKind.MissingBean;

    public static IReadOnlyList<GeneratedFile> TryApply(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        CompileRepairPlanner.RepairPlan repairPlan,
        string? buildLog)
    {
        var analysis = repairPlan.SymbolAnalysis
                       ?? CompileErrorAnalyzer.Analyze(
                           repairPlan.RootCause,
                           buildLog ?? string.Empty,
                           currentFiles,
                           plan);

        if (!ShouldPreferDeterministic(analysis))
        {
            var fallback = currentFiles.ToList();
            var fallbackChanged = StackArtifactRecoveryRouter.ApplyStructuralRecovery(fallback, plan, buildLog);
            fallbackChanged += StackArtifactRecoveryRouter.ApplyCompileRecovery(
                fallback, plan, repairPlan.FixerErrors, buildLog);
            if (fallbackChanged == 0)
                return Array.Empty<GeneratedFile>();

            return RepairErrorClassifier.DiffPatches(currentFiles, fallback);
        }

        var working = currentFiles.ToList();
        var changed = 0;
        var stack = StackArtifactRecoveryRouter.ResolveStack(plan);
        var matches = StackArtifactRecoveryRouter.MatchEcosystems(plan, currentFiles);

        if (analysis is not null)
        {
            if (stack is StackKind.Java or StackKind.JavaReactFullStack
                || matches.Any(m => m.Profile.Id is "java" or "kotlin" or "spring-boot"))
                changed += JavaCompileSymbolRemediation.Apply(working, plan, analysis);

            if (stack is StackKind.Node or StackKind.JavaReactFullStack
                || matches.Any(m => m.Profile.Category is EcosystemCategory.FrontendFramework
                    or EcosystemCategory.FullStack))
                changed += NodeTsCompileSymbolRemediation.Apply(working, plan, analysis);

            if (stack is StackKind.Python
                || matches.Any(m => m.Profile.Id is "python-fastapi" or "python"))
                changed += PythonCompileSymbolRemediation.Apply(working, plan, analysis, buildLog);
        }

        changed += StackArtifactRecoveryRouter.ApplyStructuralRecovery(working, plan, buildLog);
        changed += StackArtifactRecoveryRouter.ApplyCompileRecovery(
            working, plan, repairPlan.FixerErrors, buildLog);

        if (changed == 0)
            return Array.Empty<GeneratedFile>();

        return RepairErrorClassifier.DiffPatches(currentFiles, working);
    }
}
