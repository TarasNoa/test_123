using Libr4.IDE.AutonomousAppGeneration.Agents;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;

public static class RepoGraphBatchOrdering
{
    public static List<IReadOnlyList<PlannedFileEntry>> OrderBatches(
        List<IReadOnlyList<PlannedFileEntry>> batches,
        IRepoGraphBuilder graphBuilder,
        IOptions<RepoGraphOptions>? options = null,
        IReadOnlyDictionary<string, string>? contentsByPath = null)
    {
        if (options?.Value.UseRepoGraphOrdering == false)
            return batches;

        var result = new List<IReadOnlyList<PlannedFileEntry>>(batches.Count);
        foreach (var batch in batches)
        {
            if (batch.Count <= 1)
            {
                result.Add(batch);
                continue;
            }

            var paths = batch.Select(e => e.Path).ToList();
            var ordered = graphBuilder.OrderForGeneration(paths, contentsByPath);
            var byPath = batch.ToDictionary(e => e.Path, e => e, StringComparer.OrdinalIgnoreCase);
            var sorted = ordered.Where(byPath.ContainsKey).Select(p => byPath[p]).ToList();
            foreach (var entry in batch)
            {
                if (!sorted.Any(e => e.Path.Equals(entry.Path, StringComparison.OrdinalIgnoreCase)))
                    sorted.Add(entry);
            }

            result.Add(sorted);
        }

        return result;
    }
}
