using Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

public enum IncrementalSeedMode
{
    /// <summary>No bootstrap files — LLM creates the repository from the manifest/tasks.</summary>
    None,
    /// <summary>Full safety-net (~25 banking files) — most manifest tasks skip LLM.</summary>
    FullSafetyNet,
    /// <summary>Only bootstrap files; manifest drives LLM for the rest of the repo.</summary>
    MinimalSpine
}

public static class IncrementalGenerationSeedPolicy
{
    public static IncrementalSeedMode ResolveEffectiveSeedMode(GenerationPlan plan, AgentOrchestrationOptions options)
    {
        if (StrictStackContractEnforcer.HasActiveContract(plan))
            return IncrementalSeedMode.None;

        return options.IncrementalSeedMode;
    }

    public static bool ShouldUseStackSafetyNet(GenerationPlan plan, AgentOrchestrationOptions options) =>
        options.UseIncrementalFileScopedGeneration
        && ResolveEffectiveSeedMode(plan, options) != IncrementalSeedMode.None;

    public static IReadOnlyList<DomainGeneratedFile> ResolveSeedFiles(
        GenerationPlan plan,
        IReadOnlyList<DomainGeneratedFile> existingWorkspace,
        AgentOrchestrationOptions options)
    {
        if (!options.UseIncrementalFileScopedGeneration)
            return Array.Empty<DomainGeneratedFile>();

        if (ResolveEffectiveSeedMode(plan, options) == IncrementalSeedMode.None)
            return Array.Empty<DomainGeneratedFile>();

        var libr4Seed = Libr4MdManifest.SeedContentForPlan(plan);
        if (ResolveEffectiveSeedMode(plan, options) == IncrementalSeedMode.FullSafetyNet
            || !options.UseExpandedJavaReactManifest
            || StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
        {
            return GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, existingWorkspace);
        }

        var merged = GenerationStackSafetyNet.MergeWithStackSafetyNet(plan, existingWorkspace);
        var spine = JavaReactExpandedFileManifest.MinimalSpinePaths;
        return merged
            .Where(f => spine.Contains(
                StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath),
                StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<DomainGeneratedFile> MergeLibr4Seed(
        IReadOnlyList<DomainGeneratedFile> files,
        IReadOnlyList<DomainGeneratedFile> libr4Seed)
    {
        if (libr4Seed.Count == 0)
            return files;

        var map = files.ToDictionary(f => f.RelativePath, f => f, StringComparer.OrdinalIgnoreCase);
        foreach (var seed in libr4Seed)
            map[seed.RelativePath] = seed;
        return map.Values.ToList();
    }
}
