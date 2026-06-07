using System.Diagnostics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Phase 7.3.5 SLO: cold index of 300 source files completes within 10s on CI runners.
/// </summary>
public sealed class FastContextBenchmarkTests : IDisposable
{
    private const int FileCount = 300;
    private static readonly TimeSpan IndexBudget = TimeSpan.FromSeconds(10);

    private readonly string _root;

    public FastContextBenchmarkTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fast-context-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);

        for (var i = 0; i < FileCount; i++)
        {
            var dir = Path.Combine(_root, "src", $"pkg{i / 50}");
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, $"module_{i}.py"),
                $"""
                # module {i}
                class Entity{i}:
                    id: int = {i}
                """);
        }
    }

    [Fact]
    public async Task IndexAsync_300Files_CompletesWithinTenSeconds()
    {
        var index = CreateIndex();
        var runId = Guid.NewGuid();

        var sw = Stopwatch.StartNew();
        await index.IndexAsync(_root, runId);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(IndexBudget, $"indexing {FileCount} files should stay under {IndexBudget.TotalSeconds}s");

        var manifestPath = Path.Combine(_root, "runs", runId.ToString("D"), "context-index", "manifest.json");
        File.Exists(manifestPath).Should().BeTrue();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    private CodebaseIndexService CreateIndex()
    {
        var options = Options.Create(new FastContextOptions
        {
            RunsRoot = Path.Combine(_root, "runs")
        });
        return new CodebaseIndexService(
            new RipgrepCodeIndex(NullLogger<RipgrepCodeIndex>.Instance),
            new RepoGraphRanker(new RepoGraphBuilder()),
            new FastContextFusionRanker(options),
            options,
            NullLogger<CodebaseIndexService>.Instance);
    }
}
