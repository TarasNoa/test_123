using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Stack-aware deterministic repair pass (pytest/import/dependency sync) used in the repair loop.
/// </summary>
public static class StackDeterministicRepairPass
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog)
    {
        var changed = Tier1CompileRemediationRouter.ApplyNormalize(files, plan);
        var stack = StackPlanHeuristics.Classify(plan);

        switch (stack)
        {
            case StackKind.Python:
                changed += PythonPytestImportRemediation.Apply(files, buildLog);
                changed += PythonDependencyGraphNormalizer.Normalize(files, buildLog);
                changed += PythonDependencySyncEngine.Sync(files, buildLog);
                break;

            case StackKind.Node:
                changed += DependencySyncEngine.SyncFromBuildLog(files, buildLog);
                changed += NodeJestImportRemediation.Apply(files, plan, buildLog);
                break;

            case StackKind.DotNet:
                changed += DotNetTestCompileRemediation.Apply(files, plan, buildLog);
                break;

            case StackKind.Java:
                changed += JavaSpringCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
                changed += JavaStructuralCompileRemediation.ApplyStructuralFixes(files, plan, buildLog);
                break;

            case StackKind.JavaReactFullStack:
                changed += DependencySyncEngine.SyncFromBuildLog(files, buildLog);
                changed += NodeJestImportRemediation.Apply(files, plan, buildLog);
                changed += ReactFrontendRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
                changed += JavaSpringCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
                changed += JavaStructuralCompileRemediation.ApplyStructuralFixes(files, plan, buildLog);
                break;

            case StackKind.Go:
            case StackKind.GoReactFullStack:
                changed += GoGinReactCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
                changed += NativeManifestSyncEngines.SyncGoMod(files);
                break;

            case StackKind.Rust:
                changed += RustAxumCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
                changed += NativeManifestSyncEngines.SyncCargoToml(files);
                break;
        }

        return changed;
    }
}
