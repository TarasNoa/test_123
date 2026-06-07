using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Groups planned manifest entries into small batches (same folder/role) to cut LLM round-trips.
/// </summary>
public static class IncrementalFileBatchGrouper
{
    private static readonly HashSet<string> AlwaysSingleFile = new(StringComparer.OrdinalIgnoreCase)
    {
        "backend/manage.py",
        "backend/requirements.txt",
        "backend/pom.xml",
        "backend/src/main/java/com/generated/banking/BankingApplication.java",
        "backend/src/main/resources/application.yml",
        "backend/src/main/resources/application-dev.yml",
        "backend/src/main/resources/logback-spring.xml",
        "frontend/package.json",
        "frontend/tsconfig.json",
        "frontend/tsconfig.node.json",
        "frontend/vite.config.ts",
        "frontend/vitest.config.ts",
        "frontend/index.html",
        "frontend/.env.example",
        "frontend/src/main.tsx",
        "frontend/src/App.tsx",
        "docker-compose.yml",
        "backend/Dockerfile",
        "frontend/Dockerfile",
        ".gitignore",
        "README.md"
    };

    public static List<IReadOnlyList<PlannedFileEntry>> GroupEntries(
        IReadOnlyList<PlannedFileEntry> entries,
        int maxFilesPerBatch,
        AgentPhase? phase = null,
        bool useFeatureScopedBatches = false,
        IRepoGraphBuilder? repoGraphBuilder = null,
        IOptions<RepoGraphOptions>? repoGraphOptions = null,
        IReadOnlyDictionary<string, string>? contentsByPath = null)
    {
        var batchSize = ResolveBatchSize(maxFilesPerBatch, phase, useFeatureScopedBatches);
        if (batchSize <= 1)
            return entries.Select(e => (IReadOnlyList<PlannedFileEntry>)new[] { e }).ToList();

        var batches = new List<IReadOnlyList<PlannedFileEntry>>();
        var current = new List<PlannedFileEntry>();
        string? currentBucket = null;

        foreach (var entry in entries)
        {
            if (MustBatchAlone(entry.Path))
            {
                Flush();
                batches.Add(new[] { entry });
                continue;
            }

            var bucket = GetBatchBucket(entry.Path, useFeatureScopedBatches);
            if (current.Count > 0
                && (current.Count >= batchSize
                    || !string.Equals(currentBucket, bucket, StringComparison.OrdinalIgnoreCase)))
            {
                Flush();
            }

            currentBucket = bucket;
            current.Add(entry);
        }

        Flush();
        if (repoGraphBuilder is not null)
            return RepoGraphBatchOrdering.OrderBatches(batches, repoGraphBuilder, repoGraphOptions, contentsByPath);
        return batches;

        void Flush()
        {
            if (current.Count == 0)
                return;

            batches.Add(current.ToList());
            current.Clear();
            currentBucket = null;
        }
    }

    private static bool MustBatchAlone(string path)
    {
        var normalized = StackArtifactCompleteness.SanitizeRelativePath(path);
        if (normalized.Length == 0)
            return true;

        if (AlwaysSingleFile.Contains(normalized))
            return true;

        if (normalized.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalized.Contains("/config/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalized.Contains("/web/", StringComparison.OrdinalIgnoreCase)
            && normalized.EndsWith("Controller.java", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalized.Contains("/pages/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/contexts/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/components/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalized.StartsWith(".github/", StringComparison.OrdinalIgnoreCase))
            return true;

        return normalized.Contains("/test/", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith("Tests.java", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith("Test.java", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith(".test.tsx", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveBatchSize(int maxFilesPerBatch, AgentPhase? phase, bool useFeatureScopedBatches)
    {
        if (phase is AgentPhase.DevOps or AgentPhase.CICD or AgentPhase.Observability or AgentPhase.Documentation)
            return 1;

        if (useFeatureScopedBatches && maxFilesPerBatch <= 1)
            return 6;

        return Math.Clamp(maxFilesPerBatch, 1, 8);
    }

    private static string GetBatchBucket(string path, bool useFeatureScopedBatches)
    {
        var normalized = StackArtifactCompleteness.SanitizeRelativePath(path);
        if (useFeatureScopedBatches)
        {
            var feature = FeatureDependencyGrouper.TryResolveFeatureBucket(normalized);
            if (!string.IsNullOrWhiteSpace(feature))
                return feature;
        }

        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash <= 0)
            return normalized;

        return normalized[..lastSlash];
    }
}
