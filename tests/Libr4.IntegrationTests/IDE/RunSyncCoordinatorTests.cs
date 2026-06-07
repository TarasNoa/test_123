using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunSyncCoordinatorTests : IDisposable
{
    private readonly string _root;
    private readonly RunSyncCoordinator _coordinator;

    public RunSyncCoordinatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"run-sync-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _coordinator = new RunSyncCoordinator(
            Options.Create(new RunSyncOptions { Enabled = true, MaxInlineBytes = 1024 * 1024 }),
            Options.Create(new AgentRuntimeOptions { RunsRoot = _root }),
            NullLogger<RunSyncCoordinator>.Instance);
    }

    [Fact]
    public async Task ApplyDelta_WritesFileWithLastWriteWins()
    {
        var runId = Guid.NewGuid();
        Directory.CreateDirectory(Path.Combine(_root, runId.ToString("D"), "handoff"));
        _coordinator.RegisterSession(runId, _root, "cloud");

        var older = new WorkspaceSyncDelta(
            runId,
            "src/app.py",
            WorkspaceSyncDeltaKind.Modified,
            DateTime.UtcNow.AddMinutes(-5),
            "local",
            Convert.ToBase64String("v1"u8.ToArray()),
            "hash-v1");

        var newer = new WorkspaceSyncDelta(
            runId,
            "src/app.py",
            WorkspaceSyncDeltaKind.Modified,
            DateTime.UtcNow,
            "local",
            Convert.ToBase64String("v2"u8.ToArray()),
            "hash-v2");

        (await _coordinator.ApplyDeltaAsync(newer)).Status.Should().Be(RunSyncApplyStatus.Applied);
        (await _coordinator.ApplyDeltaAsync(older)).Status.Should().Be(RunSyncApplyStatus.SkippedOlder);

        var content = await File.ReadAllTextAsync(Path.Combine(_root, "src", "app.py"));
        content.Should().Be("v2");
    }

    [Fact]
    public async Task ApplyDelta_RecordsConflictOnSameTimestampDifferentHash()
    {
        var runId = Guid.NewGuid();
        Directory.CreateDirectory(Path.Combine(_root, runId.ToString("D"), "handoff"));
        _coordinator.RegisterSession(runId, _root, "cloud");
        var ts = DateTime.UtcNow;

        var first = new WorkspaceSyncDelta(
            runId,
            "src/conflict.py",
            WorkspaceSyncDeltaKind.Modified,
            ts,
            "local",
            Convert.ToBase64String("local-content"u8.ToArray()),
            "hash-local");

        var second = new WorkspaceSyncDelta(
            runId,
            "src/conflict.py",
            WorkspaceSyncDeltaKind.Modified,
            ts,
            "cloud",
            Convert.ToBase64String("cloud-content"u8.ToArray()),
            "hash-cloud");

        (await _coordinator.ApplyDeltaAsync(first)).Status.Should().Be(RunSyncApplyStatus.Applied);
        var conflict = await _coordinator.ApplyDeltaAsync(second);
        conflict.Status.Should().Be(RunSyncApplyStatus.ConflictRecorded);
        conflict.ConflictRelativePath.Should().NotBeNullOrWhiteSpace();

        var pending = await _coordinator.GetPendingConflictsAsync(runId);
        pending.Should().HaveCount(1);
        pending[0].RelativePath.Should().Be("src/conflict.py");
        pending[0].WinnerSource.Should().Be("local");
        pending[0].LoserSource.Should().Be("cloud");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
