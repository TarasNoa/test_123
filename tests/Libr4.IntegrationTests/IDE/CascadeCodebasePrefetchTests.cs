using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CascadeCodebasePrefetchTests : IDisposable
{
    private readonly string _root;

    public CascadeCodebasePrefetchTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"cascade-codebase-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        File.WriteAllText(Path.Combine(_root, "src", "KanbanBoard.cs"), """
            namespace Demo;

            public class KanbanBoard
            {
                public string Name { get; set; } = "board";
            }
            """);
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

    [Fact]
    public void UpstreamCloneUrlResolver_ExtractsGithubCloneUrl()
    {
        var urls = UpstreamCloneUrlResolver.ExtractCloneUrls(
            "Adapt https://github.com/acme/calorie-app with SolidJS UI");

        urls.Should().ContainSingle();
        urls[0].Should().Be("https://github.com/acme/calorie-app.git");
    }

    [Fact]
    public async Task CascadeCodebasePrefetchService_ReturnsSearchCodebaseSummary()
    {
        var cloneProvider = new FakeUpstreamCloneProvider(_root);
        var prefetcher = new FastContextPrefetcher(
            CreateIndex(),
            Options.Create(new FastContextOptions { Enabled = true, MaxPrefetchHits = 6 }),
            NullLogger<FastContextPrefetcher>.Instance);
        var service = new CascadeCodebasePrefetchService(
            prefetcher,
            cloneProvider,
            NullLogger<CascadeCodebasePrefetchService>.Instance);

        var summary = await service.BuildPrefetchContextAsync(
            "Bootstrap KanbanBoard from https://github.com/acme/kanban-demo with JWT auth",
            maxChars: 1200,
            CancellationToken.None);

        summary.Should().NotBeNullOrWhiteSpace();
        summary.Should().Contain("search_codebase");
        summary.Should().Contain("cascade_prefetch");
        summary.Should().Contain("KanbanBoard");
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

    private sealed class FakeUpstreamCloneProvider : IUpstreamCloneProvider
    {
        private readonly string _workspaceRoot;

        public FakeUpstreamCloneProvider(string workspaceRoot) => _workspaceRoot = workspaceRoot;

        public Task<UpstreamCloneHandle?> TryShallowCloneAsync(string cloneUrl, CancellationToken ct = default) =>
            Task.FromResult<UpstreamCloneHandle?>(new UpstreamCloneHandle(_workspaceRoot, cloneUrl, ownsPath: false));
    }
}
