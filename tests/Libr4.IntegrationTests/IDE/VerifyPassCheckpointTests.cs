using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class VerifyPassCheckpointTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly Guid _runId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly string _workspaceRoot;

    public VerifyPassCheckpointTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"verify-pass-{Guid.NewGuid():N}");
        _workspaceRoot = Path.Combine(Path.GetTempPath(), $"verify-ws-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspaceRoot);
        Directory.CreateDirectory(Path.Combine(_runsRoot, _runId.ToString("D")));
    }

    public void Dispose()
    {
        TryDelete(_runsRoot);
        TryDelete(_workspaceRoot);
    }

    [Fact]
    public async Task RecordVerifyPass_PersistsSnapshotAndListsCheckpoint()
    {
        var git = CreateGitService();
        await git.EnsureInitializedAsync(_workspaceRoot);
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "app.py"), "print('ok')");

        var workspaceId = Guid.NewGuid();
        var checkpoint = CreateCheckpointService(git, workspaceId);

        await checkpoint.RecordVerifyPassAsync(_runId, workspaceId);

        var listed = await checkpoint.ListCheckpointsAsync(_runId);
        listed.Should().ContainSingle(c => c.Tag == "verify-pass-1" && c.FileCount >= 1);

        var aggregator = CreateAggregator(checkpoint);

        var diffs = await aggregator.ListAsync(
            _runId,
            new RunDiffQuery(CheckpointTag: "verify-pass-1"));
        diffs.Total.Should().BeGreaterThan(0);
        diffs.Items.Should().Contain(i => i.Path == "app.py");

        var detail = await aggregator.GetDetailAsync(_runId, "app.py", "verify-pass-1");
        detail.Should().NotBeNull();
        detail!.Provenance.Should().Contain(p => p.ToolName == "verify_checkpoint");
    }

    [Fact]
    public async Task TagVerifyPass_AndGetSnapshotDiff_ReturnsInitialToTagChanges()
    {
        var git = CreateGitService();
        await git.EnsureInitializedAsync(_workspaceRoot);
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "main.py"), "v1");
        await git.TagVerifyPassAsync(_workspaceRoot, 1);
        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "main.py"), "v2");

        var diffs = await git.GetSnapshotDiffAtTagAsync(
            _workspaceRoot,
            IShadowGitCheckpointService.VerifyPassTagName(1));

        diffs.Should().ContainSingle(d => d.Path == "main.py");
        diffs[0].UnifiedDiff.Should().Contain("v1");
    }

    private VerifyPassCheckpointService CreateCheckpointService(
        ShadowGitCheckpointService git,
        Guid workspaceId) =>
        new(
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            NullLogger<VerifyPassCheckpointService>.Instance,
            git,
            new StubShadowAccessor(workspaceId, _workspaceRoot));

    private RunDiffAggregator CreateAggregator(VerifyPassCheckpointService checkpoint) =>
        new(
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            checkpoint,
            NullLogger<RunDiffAggregator>.Instance);

    private static ShadowGitCheckpointService CreateGitService() =>
        new(
            Options.Create(new ShadowGitCheckpointOptions { Enabled = true }),
            NullLogger<ShadowGitCheckpointService>.Instance);

    private sealed class StubShadowAccessor : IShadowWorkspaceAccessor
    {
        private readonly Guid _workspaceId;
        private readonly string _hostPath;

        public StubShadowAccessor(Guid workspaceId, string hostPath)
        {
            _workspaceId = workspaceId;
            _hostPath = hostPath;
        }

        public bool TryGetWorkspace(Guid workspaceId, out ShadowWorkspaceContext context)
        {
            if (workspaceId != _workspaceId)
            {
                context = default!;
                return false;
            }

            context = new ShadowWorkspaceContext(
                _workspaceId,
                _hostPath,
                string.Empty,
                NullRuntimeSession.Instance);
            return true;
        }

        public Task<ExecResult> ExecAsync(Guid workspaceId, string command, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> ReadFileAsync(Guid workspaceId, string relativePath, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task WriteFileAsync(Guid workspaceId, string relativePath, string content, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public IReadOnlyList<string> GlobFiles(Guid workspaceId, string globPattern) =>
            Array.Empty<string>();

        private sealed class NullRuntimeSession : IRuntimeSession
        {
            public static readonly NullRuntimeSession Instance = new();
            public string SessionId => "stub";
            public string ProviderName => "stub";
            public string Image => "stub";
            public string HostMountPath => string.Empty;
            public string GuestMountPath => "/workspace";

            public Task<ExecResult> ExecAsync(
                string command,
                string workingSubDirectory,
                IDictionary<string, string>? environmentVariables = null,
                TimeSpan? timeout = null,
                CancellationToken ct = default) =>
                throw new NotSupportedException();

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }
}
