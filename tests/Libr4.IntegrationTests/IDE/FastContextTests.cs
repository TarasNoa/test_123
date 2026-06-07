using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.CodeSearch;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FastContextTests : IDisposable
{
    private readonly string _root;

    public FastContextTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fast-context-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "backend"));
        Directory.CreateDirectory(Path.Combine(_root, "frontend", "components"));
        File.WriteAllText(Path.Combine(_root, "backend", "models.py"), """
            class User:
                id: int
                email: str
            """);
        File.WriteAllText(Path.Combine(_root, "backend", "services.py"), """
            from backend.models import User

            class UserService:
                def get(self, user_id: int) -> User:
                    return User()
            """);
        File.WriteAllText(Path.Combine(_root, "frontend", "components", "UserCard.tsx"), """
            export function UserCard() {
              return <div>User</div>;
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
    public async Task Search_UserModel_ReturnsModelsPyInTopResults()
    {
        var index = CreateIndex();
        var hits = await index.SearchAsync(_root, "class User", new CodebaseSearchOptions(Limit: 6, IncludeTests: true));

        hits.Should().NotBeEmpty();
        hits.Any(h => h.Path.Contains("models.py", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public void FusionRanker_PrefersGraphNeighborOverUnrelatedFile()
    {
        var fusion = new FastContextFusionRanker(Options.Create(new FastContextOptions()));
        var seed = new CodebaseSearchHit("backend/models.py", 1, 3, 1.0, "class User", "ripgrep");
        var neighbor = new CodebaseSearchHit("backend/services.py", 1, 3, 0.9, "UserService", "ripgrep");
        var unrelated = new CodebaseSearchHit("frontend/components/UserCard.tsx", 1, 3, 0.95, "UserCard", "ripgrep");

        var fused = fusion.Fuse(
            new[] { seed, unrelated, neighbor },
            new[] { (neighbor, 0.8), (seed, 0.2), (unrelated, 0.0) },
            3);

        fused[0].Path.Should().Be("backend/services.py");
    }

    [Fact]
    public async Task IndexAsync_WritesManifestForRun()
    {
        var runsRoot = Path.Combine(_root, "runs");
        var index = CreateIndex(runsRoot);
        var runId = Guid.NewGuid();

        await index.IndexAsync(_root, runId);

        var manifestPath = Path.Combine(runsRoot, runId.ToString("D"), "context-index", "manifest.json");
        File.Exists(manifestPath).Should().BeTrue();
    }

    [Fact]
    public async Task Prefetch_RepairTurn_InjectsFailingFileNeighbors()
    {
        var index = CreateIndex();
        var prefetcher = new FastContextPrefetcher(
            index,
            Options.Create(new FastContextOptions { Enabled = true, MaxPrefetchHits = 6 }),
            NullLogger<FastContextPrefetcher>.Instance);

        var errors = new[]
        {
            new ErrorReport("Compile", "cannot find symbol: UserService", "add import", "backend/services.py", 12)
        };

        var result = await prefetcher.PrefetchForRepairAsync(
            new FastContextPrefetchRequest(_root, "error CS0246 UserService", errors));

        result.Hits.Should().NotBeEmpty();
        result.FormattedText.Should().Contain("services.py");
        result.Queries.Should().Contain("UserService");
    }

    [Fact]
    public void Prefetch_BuildQueries_ExtractsSymbolFromError()
    {
        var queries = FastContextPrefetcher.BuildQueries(new FastContextPrefetchRequest(
            null,
            null,
            new[] { new ErrorReport("Compile", "CS0246: KanbanBoardService not found", "fix", "src/Services/Kanban.cs", 4) }));

        queries.Should().Contain("KanbanBoardService");
    }

    [Fact]
    public async Task GetSymbolAsync_FindsSymbolInFile()
    {
        var index = CreateIndex();
        var ctx = await index.GetSymbolAsync(_root, "UserService", "services.py");
        ctx.Should().NotBeNull();
        ctx!.Symbol.Should().Be("UserService");
        ctx.Path.Should().Contain("services.py");
    }

    [Fact]
    public async Task WorkspaceSyncBridge_InvalidatesIndexOnFileChange()
    {
        var index = new TrackingCodebaseIndex(CreateIndex());
        var workspaceId = Guid.NewGuid();
        var pool = new StubWorkspacePool(workspaceId, _root);
        var sync = new Libr4.IDE.Application.AutonomousAppGeneration.Runtime.FileSystemWorkspaceSyncService(
            NullLogger<Libr4.IDE.Application.AutonomousAppGeneration.Runtime.FileSystemWorkspaceSyncService>.Instance);
        var bridge = new FastContextWorkspaceSyncBridge(
            sync,
            pool,
            index,
            NullLogger<FastContextWorkspaceSyncBridge>.Instance);

        await bridge.StartAsync(CancellationToken.None);
        sync.StartWatching(new Libr4.IDE.Application.AutonomousAppGeneration.Runtime.WorkspaceHandle(
            workspaceId,
            _root,
            "/workspace",
            new NoOpRuntimeSession()));

        await File.WriteAllTextAsync(Path.Combine(_root, "backend", "touch.py"), "class Touch {}\n");
        await Task.Delay(500);

        index.InvalidateCount.Should().BeGreaterThan(0);
        sync.StopWatching(workspaceId);
        await bridge.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task EmbeddingIndex_SearchReturnsSemanticHits()
    {
        var embedRoot = Path.Combine(Path.GetTempPath(), $"fast-context-embed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(embedRoot, "backend"));
        var authPath = Path.Combine(embedRoot, "backend", "auth_service.py");
        var lines = Enumerable.Range(1, 50)
            .Select(i => i == 25 ? "class UserAuthenticationService:" : $"    step_{i} = {i}")
            .ToArray();
        await File.WriteAllTextAsync(authPath, string.Join('\n', lines));

        try
        {
            var embedding = new EmbeddingCodeIndex(
                new StubEmbeddingService(),
                new InProcessVectorMemoryStore(),
                Options.Create(new FastContextOptions { EnableEmbeddingIndex = true, EmbeddingMinChunkLines = 10, EmbeddingMaxChunkLines = 30 }),
                NullLogger<EmbeddingCodeIndex>.Instance);

            await embedding.IndexAsync(embedRoot);
            var hits = await embedding.SearchAsync(embedRoot, "UserAuthenticationService login flow", new CodebaseSearchOptions(Limit: 3));

            hits.Should().NotBeEmpty();
            hits.Should().Contain(h => h.Path.Contains("auth_service.py", StringComparison.OrdinalIgnoreCase));
            hits.First(h => h.Path.Contains("auth_service.py", StringComparison.OrdinalIgnoreCase)).MatchKind.Should().Be("embedding");
        }
        finally
        {
            if (Directory.Exists(embedRoot))
                Directory.Delete(embedRoot, recursive: true);
        }
    }

    private CodebaseIndexService CreateIndex(string? runsRoot = null)
    {
        var options = Options.Create(new FastContextOptions
        {
            RunsRoot = runsRoot ?? Path.Combine(_root, "runs")
        });
        return new CodebaseIndexService(
            new RipgrepCodeIndex(NullLogger<RipgrepCodeIndex>.Instance),
            new RepoGraphRanker(new RepoGraphBuilder()),
            new FastContextFusionRanker(options),
            options,
            NullLogger<CodebaseIndexService>.Instance);
    }

    private sealed class TrackingCodebaseIndex : ICodebaseIndex
    {
        private readonly ICodebaseIndex _inner;
        public int InvalidateCount { get; private set; }

        public TrackingCodebaseIndex(ICodebaseIndex inner) => _inner = inner;

        public Task IndexAsync(string workspaceRoot, Guid? runId = null, CancellationToken ct = default) =>
            _inner.IndexAsync(workspaceRoot, runId, ct);

        public Task<IReadOnlyList<CodebaseSearchHit>> SearchAsync(
            string workspaceRoot,
            string query,
            CodebaseSearchOptions? options = null,
            CancellationToken ct = default) =>
            _inner.SearchAsync(workspaceRoot, query, options, ct);

        public Task<CodebaseSymbolContext?> GetSymbolAsync(
            string workspaceRoot,
            string symbol,
            string? pathHint = null,
            CancellationToken ct = default) =>
            _inner.GetSymbolAsync(workspaceRoot, symbol, pathHint, ct);

        public Task InvalidateAsync(string workspaceRoot, CancellationToken ct = default)
        {
            InvalidateCount++;
            return _inner.InvalidateAsync(workspaceRoot, ct);
        }
    }

    private sealed class StubWorkspacePool : Libr4.IDE.Application.AutonomousAppGeneration.Runtime.IWorkspacePool
    {
        private readonly Guid _workspaceId;
        private readonly string _hostPath;

        public StubWorkspacePool(Guid workspaceId, string hostPath)
        {
            _workspaceId = workspaceId;
            _hostPath = hostPath;
        }

        public Task<Libr4.IDE.Application.AutonomousAppGeneration.Runtime.WorkspaceHandle> AcquireAsync(
            string runtimeImage,
            CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task ReleaseAsync(Libr4.IDE.Application.AutonomousAppGeneration.Runtime.WorkspaceHandle handle, CancellationToken ct = default) =>
            Task.CompletedTask;

        public IReadOnlyList<Libr4.IDE.Application.AutonomousAppGeneration.Runtime.WorkspaceHandle> ListActive() =>
            new[]
            {
                new Libr4.IDE.Application.AutonomousAppGeneration.Runtime.WorkspaceHandle(
                    _workspaceId,
                    _hostPath,
                    "/workspace",
                    new NoOpRuntimeSession())
            };
    }

    private sealed class NoOpRuntimeSession : Libr4.IDE.Application.AutonomousAppGeneration.Runtime.IRuntimeSession
    {
        public string ProviderName => "noop";
        public string SessionId => "noop";
        public string HostMountPath => "/";
        public string GuestMountPath => "/workspace";
        public string Image => "noop";

        public Task<Libr4.IDE.Application.AutonomousAppGeneration.Runtime.ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            Task.FromResult(new Libr4.IDE.Application.AutonomousAppGeneration.Runtime.ExecResult(
                0,
                TimeSpan.Zero,
                Array.Empty<Libr4.IDE.Domain.AutonomousAppGeneration.ConsoleLogEntry>()));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StubEmbeddingService : IEmbeddingService
    {
        public int Dimensions => 32;

        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default) =>
            Task.FromResult(Embed(text));

        public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default) =>
            Task.FromResult(texts.Select(Embed).ToArray());

        private static float[] Embed(string text)
        {
            var vec = new float[32];
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
            for (var i = 0; i < vec.Length; i++)
                vec[i] = bytes[i % bytes.Length] / 255f;
            return vec;
        }
    }
}
