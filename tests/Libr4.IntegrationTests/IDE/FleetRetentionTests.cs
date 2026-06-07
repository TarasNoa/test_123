using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FleetRetentionTests : IDisposable
{
    private readonly string _root;

    public FleetRetentionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"fleet-retention-{Guid.NewGuid():N}");
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
    public async Task Retention_ArchivesOldTerminalRuns_SkipsPinned()
    {
        var (retention, index, _) = CreateRetention(new FleetRetentionOptions
        {
            FleetIndexArchiveAfterDays = 30,
            RunArtifactsDeleteAfterDays = 90
        });
        await index.EnsureSchemaAsync();

        var oldCompleted = Guid.NewGuid();
        var pinnedOld = Guid.NewGuid();
        var recent = Guid.NewGuid();

        await index.UpsertAsync(MakeEntry(oldCompleted, AgentFleetStatus.Completed, DateTime.UtcNow.AddDays(-40)));
        await index.UpsertAsync(MakeEntry(pinnedOld, AgentFleetStatus.Failed, DateTime.UtcNow.AddDays(-40), pinned: true));
        await index.UpsertAsync(MakeEntry(recent, AgentFleetStatus.Completed, DateTime.UtcNow.AddDays(-5)));

        var result = await retention.ApplyRetentionAsync();

        result.ArchivedCount.Should().Be(1);
        (await index.GetAsync(oldCompleted))!.Archived.Should().BeTrue();
        (await index.GetAsync(pinnedOld))!.Archived.Should().BeFalse();
        (await index.GetAsync(recent))!.Archived.Should().BeFalse();
    }

    [Fact]
    public async Task Retention_PurgesArtifactsForOldArchivedRuns()
    {
        var runId = Guid.NewGuid();
        var runDir = Path.Combine(_root, runId.ToString("D"));
        Directory.CreateDirectory(runDir);
        await File.WriteAllTextAsync(Path.Combine(runDir, "artifact.txt"), "payload");

        var (retention, index, search) = CreateRetention(new FleetRetentionOptions
        {
            FleetIndexArchiveAfterDays = 365,
            RunArtifactsDeleteAfterDays = 30
        });
        await index.EnsureSchemaAsync();
        await search.EnsureSchemaAsync();

        await index.UpsertAsync(MakeEntry(
            runId,
            AgentFleetStatus.Completed,
            DateTime.UtcNow.AddDays(-60),
            archived: true));

        await search.IndexAsync(new FleetSessionIndexDocument(
            runId,
            "Archived run",
            "request",
            null,
            null,
            null,
            "django",
            "pass",
            DateTime.UtcNow.AddDays(-60),
            true));

        var result = await retention.ApplyRetentionAsync();

        result.ArtifactsPurgedCount.Should().Be(1);
        Directory.Exists(runDir).Should().BeFalse();
        (await search.SearchAsync(new FleetSessionSearchQuery("Archived run"))).Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task GdprExport_IncludesFleetEntryAndArtifactManifest()
    {
        var runId = Guid.NewGuid();
        var runDir = Path.Combine(_root, runId.ToString("D"));
        Directory.CreateDirectory(runDir);
        await File.WriteAllTextAsync(Path.Combine(runDir, "plan.yaml"), "app: demo");

        var (_, index, _) = CreateRetention(new FleetRetentionOptions());
        await index.EnsureSchemaAsync();
        await index.UpsertAsync(MakeEntry(runId, AgentFleetStatus.Completed, DateTime.UtcNow));

        var export = new FleetGdprExportService(
            index,
            Options.Create(new AgentFleetOptions { RunsRoot = _root }),
            NullLogger<FleetGdprExportService>.Instance,
            new FleetShipStateStore(Options.Create(new AgentFleetOptions { IndexDbPath = Path.Combine(_root, "fleet.db") })));

        var bundle = await export.ExportAsync(runId);

        bundle.Should().NotBeNull();
        bundle!.RunId.Should().Be(runId);
        using var doc = JsonDocument.Parse(bundle.JsonPayload);
        doc.RootElement.GetProperty("runId").GetGuid().Should().Be(runId);
        doc.RootElement.GetProperty("fleetEntry").GetProperty("Title").GetString().Should().Be("Test run");
        doc.RootElement.GetProperty("artifactCount").GetInt32().Should().BeGreaterThan(0);
    }

    private (FleetRetentionService Retention, SqliteAgentFleetIndexStore Index, SqliteFleetSessionSearchService Search) CreateRetention(
        FleetRetentionOptions retentionOptions)
    {
        var dbPath = Path.Combine(_root, "fleet.db");
        var fleetOptions = Options.Create(new AgentFleetOptions { IndexDbPath = dbPath, RunsRoot = _root });
        var index = new SqliteAgentFleetIndexStore(fleetOptions, NullLogger<SqliteAgentFleetIndexStore>.Instance);
        var search = new SqliteFleetSessionSearchService(fleetOptions, index, NullLogger<SqliteFleetSessionSearchService>.Instance);
        var memory = new Mock<Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes.IHermesMemoryStore>();
        memory.Setup(x => x.PruneExpiredEpisodicAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var retention = new FleetRetentionService(
            index,
            search,
            Options.Create(retentionOptions),
            fleetOptions,
            NullLogger<FleetRetentionService>.Instance,
            memory.Object);

        return (retention, index, search);
    }

    private static AgentFleetEntry MakeEntry(
        Guid runId,
        AgentFleetStatus status,
        DateTime lastActivity,
        bool pinned = false,
        bool archived = false) =>
        new(
            runId,
            "Test run",
            null,
            status,
            "done",
            1,
            lastActivity.AddHours(-1),
            lastActivity,
            0,
            null,
            null,
            "django",
            pinned,
            archived,
            null);
}
