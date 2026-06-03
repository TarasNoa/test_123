using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public static class UpstreamSemanticAdaptationService
{
    public static async Task<UpstreamSemanticAdaptationResult> TryAdaptAsync(
        ICodeGenerationService codeGen,
        GenerationPlan plan,
        IList<GeneratedFile> files,
        ILogger logger,
        CancellationToken ct = default)
    {
        var hasUpstream = files.Any(f =>
            f.RelativePath.Replace('\\', '/').StartsWith("upstream/", StringComparison.OrdinalIgnoreCase));
        if (!hasUpstream || !StackPlanHeuristics.IsDotNet(plan))
            return UpstreamSemanticAdaptationResult.Skipped();

        var deterministic = UpstreamSemanticAdaptationEnricher.Apply(plan, files);
        var llmPatches = 0;

        var digest = UpstreamSemanticAdaptationEnricher.BuildUpstreamDigestForLlm(
            files as IReadOnlyList<GeneratedFile> ?? files.ToList());
        var errors = new[]
        {
            new ErrorReport(
                "UpstreamSemanticAdaptation",
                "Map upstream repository domain (board/columns/tasks) into existing C# product files. " +
                "Preserve JWT auth and HTTP routes. Do not delete upstream/ snapshot.",
                digest,
                filePath: "ADAPTATION_BRIDGE.md")
        };

        try
        {
            var patches = await codeGen.ApplyFixesAsync(plan, files.ToList(), errors, ct).ConfigureAwait(false);
            llmPatches = MergePatches(files, patches);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM upstream semantic adaptation failed; deterministic extract remains applied.");
        }

        var total = deterministic + llmPatches;
        return total > 0
            ? UpstreamSemanticAdaptationResult.Completed(deterministic, llmPatches)
            : UpstreamSemanticAdaptationResult.Skipped();
    }

    private static int MergePatches(IList<GeneratedFile> files, IReadOnlyList<GeneratedFile> patches)
    {
        var changed = 0;
        foreach (var patch in patches)
        {
            if (patch.RelativePath.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
                continue;

            var idx = -1;
            for (var i = 0; i < files.Count; i++)
            {
                if (!files[i].RelativePath.Equals(patch.RelativePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                idx = i;
                break;
            }

            if (idx < 0)
            {
                files.Add(patch);
                changed++;
            }
            else if (!string.Equals(files[idx].Content, patch.Content, StringComparison.Ordinal))
            {
                files[idx] = patch;
                changed++;
            }
        }

        return changed;
    }
}

public readonly record struct UpstreamSemanticAdaptationResult(
    bool Attempted,
    bool Succeeded,
    int DeterministicFiles,
    int LlmFiles)
{
    public static UpstreamSemanticAdaptationResult Skipped() => new(false, false, 0, 0);

    public static UpstreamSemanticAdaptationResult Completed(int deterministic, int llm) =>
        new(true, true, deterministic, llm);
}
