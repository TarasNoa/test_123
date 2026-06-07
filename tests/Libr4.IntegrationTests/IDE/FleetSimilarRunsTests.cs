using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FleetSimilarRunsTests : IDisposable
{
    private readonly string _root;

    public FleetSimilarRunsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fleet-similar-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
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
    public async Task FindSimilar_ReturnsMatchingRun_AndExcludesSelf()
    {
        var (similar, index, runA, runB, runC) = CreateService();
        var sharedError = "CS0246 UserService missing import backend/services.py";

        await similar.IndexAsync(MakeDoc(runA, "Kanban app", sharedError));
        await similar.IndexAsync(MakeDoc(runB, "Kanban app", sharedError));
        await similar.IndexAsync(MakeDoc(runC, "Banking app", "Spring Boot actuator health failed"));

        await SeedFleetEntry(index, runA, "Kanban app", sharedError);
        await SeedFleetEntry(index, runB, "Kanban app", sharedError);
        await SeedFleetEntry(index, runC, "Banking app", "Spring Boot actuator health failed");

        var result = await similar.FindSimilarAsync(runA);

        result.Hits.Should().ContainSingle(h => h.RunId == runB);
        result.Hits.Should().NotContain(h => h.RunId == runA);
        result.Hits.Should().NotContain(h => h.RunId == runC);
        result.Method.Should().Be("embedding");
    }

    [Fact]
    public async Task RemoveAsync_DropsRunFromSimilarResults()
    {
        var (similar, index, runA, runB, _) = CreateService();
        var sharedError = "JSON parse error manage.py settings";

        await similar.IndexAsync(MakeDoc(runA, "Django A", sharedError));
        await similar.IndexAsync(MakeDoc(runB, "Django B", sharedError));
        await SeedFleetEntry(index, runA, "Django A", sharedError);
        await SeedFleetEntry(index, runB, "Django B", sharedError);

        await similar.RemoveAsync(runB);
        var result = await similar.FindSimilarAsync(runA);

        result.Hits.Should().BeEmpty();
    }

    private (FleetSimilarRunsService Service, SqliteAgentFleetIndexStore Index, Guid RunA, Guid RunB, Guid RunC) CreateService()
    {
        var dbPath = Path.Combine(_root, "fleet.db");
        var options = Options.Create(new AgentFleetOptions { IndexDbPath = dbPath, RunsRoot = _root });
        var index = new SqliteAgentFleetIndexStore(options, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var similar = new FleetSimilarRunsService(
            new InProcessVectorMemoryStore(),
            new HashEmbeddingService(),
            index,
            Options.Create(new FleetSimilarRunsOptions { Enabled = true, MinScore = 0.9 }),
            NullLogger<FleetSimilarRunsService>.Instance);

        return (similar, index, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    private static FleetSessionIndexDocument MakeDoc(Guid runId, string title, string error) =>
        new(
            runId,
            title,
            UserRequest: null,
            ErrorSignature: error,
            FilesTouched: null,
            SpaceName: null,
            StackTags: "django",
            Outcome: "fail",
            DateTime.UtcNow,
            Pinned: false);

    private static async Task SeedFleetEntry(
        SqliteAgentFleetIndexStore index,
        Guid runId,
        string title,
        string failureReason)
    {
        await index.EnsureSchemaAsync();
        await index.UpsertAsync(new AgentFleetEntry(
            RunId: runId,
            Title: title,
            SpaceId: null,
            Status: AgentFleetStatus.Failed,
            Stage: "repairing",
            AgentCount: 1,
            StartedAtUtc: DateTime.UtcNow.AddHours(-1),
            LastActivityAtUtc: DateTime.UtcNow,
            CostUsd: 0,
            ModelProfile: null,
            VerifyStatus: null,
            Stack: "django",
            Pinned: false,
            Archived: false,
            FailureReason: failureReason), CancellationToken.None);
    }

    private sealed class HashEmbeddingService : IEmbeddingService
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
            var norm = MathF.Sqrt(vec.Sum(v => v * v));
            if (norm > 0)
            {
                for (var i = 0; i < vec.Length; i++)
                    vec[i] /= norm;
            }

            return vec;
        }
    }
}
