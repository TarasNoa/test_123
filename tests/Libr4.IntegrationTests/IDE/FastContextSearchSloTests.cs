using System.Diagnostics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Phase 7.3.4 SLO gate: P95 search_codebase &lt; 800ms warm, &lt; 3s cold (workspace &lt; 500 files).
/// </summary>
public sealed class FastContextSearchSloTests : IDisposable
{
    private const int FileCount = 200;
    private const int SampleCount = 20;
    private static readonly TimeSpan WarmP95Budget = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan ColdP95Budget = TimeSpan.FromSeconds(3);

    private readonly string _root;

    public FastContextSearchSloTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fast-context-slo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);

        for (var i = 0; i < FileCount; i++)
        {
            var dir = Path.Combine(_root, "src", $"pkg{i / 40}");
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, $"module_{i}.py"),
                $"""
                # module {i}
                class Entity{i}:
                    token_{i % 17} = {i}
                """);
        }
    }

    [Fact]
    public async Task Search_WarmIndex_P95UnderEightHundredMs()
    {
        var index = CreateIndex();
        await index.IndexAsync(_root, Guid.NewGuid());

        var durations = await SampleSearchDurationsAsync(index, "Entity");
        var p95 = Percentile(durations, 0.95);

        p95.Should().BeLessThan(WarmP95Budget,
            $"warm P95 search over {FileCount} files was {p95.TotalMilliseconds:F0}ms");
    }

    [Fact]
    public async Task Search_ColdIndex_P95UnderThreeSeconds()
    {
        var index = CreateIndex();
        await index.InvalidateAsync(_root);

        var durations = await SampleSearchDurationsAsync(index, "Entity");
        var p95 = Percentile(durations, 0.95);

        p95.Should().BeLessThan(ColdP95Budget,
            $"cold P95 search over {FileCount} files was {p95.TotalMilliseconds:F0}ms");
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

    private async Task<List<TimeSpan>> SampleSearchDurationsAsync(CodebaseIndexService index, string query)
    {
        var durations = new List<TimeSpan>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            var hits = await index.SearchAsync(
                _root,
                $"{query}{i % 5}",
                new CodebaseSearchOptions(Limit: 8, IncludeTests: true));
            sw.Stop();
            hits.Should().NotBeNull();
            durations.Add(sw.Elapsed);
        }

        return durations;
    }

    private static TimeSpan Percentile(IReadOnlyList<TimeSpan> values, double percentile)
    {
        var ordered = values.OrderBy(v => v.Ticks).ToList();
        var rank = (int)Math.Ceiling(percentile * ordered.Count) - 1;
        rank = Math.Clamp(rank, 0, ordered.Count - 1);
        return ordered[rank];
    }

    private CodebaseIndexService CreateIndex()
    {
        var options = Options.Create(new FastContextOptions
        {
            RunsRoot = Path.Combine(_root, "runs"),
            EnableEmbeddingIndex = false,
        });
        return new CodebaseIndexService(
            new RipgrepCodeIndex(NullLogger<RipgrepCodeIndex>.Instance),
            new RepoGraphRanker(new RepoGraphBuilder()),
            new FastContextFusionRanker(options),
            options,
            NullLogger<CodebaseIndexService>.Instance);
    }
}
