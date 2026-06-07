using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SessionSearchTests : IDisposable
{
    private readonly string _memoryDbPath;
    private readonly string _rolloutDbPath;
    private readonly string _runsRoot;
    private readonly SqliteHermesMemoryStore _memoryStore;
    private readonly FileRolloutRecorder _rolloutRecorder;

    public SessionSearchTests()
    {
        var suffix = Guid.NewGuid().ToString("N");
        _memoryDbPath = Path.Combine(Path.GetTempPath(), $"session-search-mem-{suffix}.db");
        _rolloutDbPath = Path.Combine(Path.GetTempPath(), $"session-search-rollout-{suffix}.db");
        _runsRoot = Path.Combine(Path.GetTempPath(), $"session-search-runs-{suffix}");
        Directory.CreateDirectory(_runsRoot);

        _memoryStore = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _memoryDbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);

        _rolloutRecorder = new FileRolloutRecorder(Options.Create(new AgentRuntimeOptions
        {
            RolloutDbPath = _rolloutDbPath,
            RunsRoot = _runsRoot
        }));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_runsRoot))
                Directory.Delete(_runsRoot, recursive: true);
            if (File.Exists(_memoryDbPath))
                File.Delete(_memoryDbPath);
            if (File.Exists(_rolloutDbPath))
                File.Delete(_rolloutDbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task HermesStore_FtsSearch_FindsSummaryByKeyword()
    {
        var runId = Guid.NewGuid();
        await _memoryStore.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            "fp-search",
            MemoryKind.Procedural,
            "repair",
            "manage.py-fix",
            "invalid json in manage.py settings block",
            null,
            32,
            0.8,
            DateTime.UtcNow));

        var hits = await _memoryStore.SearchSummariesAsync("manage.py invalid json");
        hits.Should().NotBeEmpty();
        hits[0].RunId.Should().Be(runId);
        hits[0].Key.Should().Be("manage.py-fix");
    }

    [Fact]
    public async Task RolloutRecorder_FtsSearch_FindsToolOutput()
    {
        var runId = Guid.NewGuid();
        await _rolloutRecorder.RecordToolUseAsync(
            runId,
            "session-1",
            stepNumber: 2,
            toolName: "apply_patch",
            inputJson: "{}",
            outputJson: "patched manage.py invalid json syntax",
            success: true,
            durationMs: 120);

        var hits = await _rolloutRecorder.SearchAsync("\"manage.py\" AND \"invalid\" AND \"json\"");
        hits.Should().NotBeEmpty();
        hits[0].RunId.Should().Be(runId);
        hits[0].ToolName.Should().Be("apply_patch");
    }

    [Fact]
    public async Task CompositeSearch_MergesRolloutAndMemoryHits()
    {
        var runId = Guid.NewGuid();
        await _memoryStore.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            "fp-composite",
            MemoryKind.Episodic,
            "build",
            "django-import",
            "manage.py invalid json after settings merge",
            null,
            24,
            0.5,
            DateTime.UtcNow));
        await _rolloutRecorder.RecordToolUseAsync(
            runId,
            "session-2",
            stepNumber: 4,
            toolName: "run_build",
            inputJson: "{}",
            outputJson: "build failed: manage.py invalid json",
            success: false,
            durationMs: 900);

        var search = new CompositeSessionSearchService(_rolloutRecorder, _memoryStore);
        var hits = await search.SearchAsync("manage.py invalid json", limit: 10);

        hits.Should().HaveCountGreaterOrEqualTo(2);
        hits.Select(hit => hit.Source).Should().Contain("rollout");
        hits.Select(hit => hit.Source).Should().Contain("memory");
    }
}
