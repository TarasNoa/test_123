using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class MemoryToolTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteHermesMemoryStore _store;
    private readonly HermesMemoryManager _manager;
    private readonly InMemoryRolloutRecorder _rollout;

    public MemoryToolTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"memory-tools-{Guid.NewGuid():N}.db");
        _store = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        _manager = new HermesMemoryManager(
            _store,
            Options.Create(new HermesMemoryManagerOptions()),
            NullLogger<HermesMemoryManager>.Instance);
        _rollout = new InMemoryRolloutRecorder();
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task MemoryWrite_PersistsProjectScopedMemory()
    {
        var runId = Guid.NewGuid();
        var tool = new MemoryWriteTool(_store, _manager, Options.Create(new MemoryToolOptions()), _rollout);
        var context = BuildContext(runId);
        var input = JsonDocument.Parse("""
            {
              "key": "django-import-fix",
              "summary": "Use relative imports inside Django apps",
              "scope": "project",
              "kind": "procedural"
            }
            """).RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);

        result.Success.Should().BeTrue();
        context.Session.MemoryWriteCount.Should().Be(1);

        var fp = _manager.ResolveFingerprint(context.Plan!);
        var read = await _store.RetrieveAsync(new HermesMemoryQuery(fp, Keyword: "django", TopK: 5));
        read.Should().ContainSingle();
        read[0].Entry.Key.Should().Be("django-import-fix");

        _rollout.Entries.Should().ContainSingle(e => e.Type == "memory_operation");
    }

    [Fact]
    public async Task MemoryRead_ReturnsFormattedMatches()
    {
        var runId = Guid.NewGuid();
        var fp = _manager.ResolveFingerprint(SamplePlan());
        await _store.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(), runId, null, fp, MemoryKind.Semantic, "repair", "stack-note",
            "Django 5 requires ASGI for websockets", null, 20, 1.0, DateTime.UtcNow));

        var tool = new MemoryReadTool(_store, _manager, Options.Create(new MemoryToolOptions()), _rollout);
        var context = BuildContext(runId);
        var input = JsonDocument.Parse("""
            { "keyword": "django", "scope": "project", "kind": "semantic" }
            """).RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("memory_read:");
        result.Output.Should().Contain("[L2_semantic]");
        result.Output.Should().Contain("stack-note");
    }

    [Fact]
    public async Task MemoryWrite_RejectsOversizedSummary()
    {
        var runId = Guid.NewGuid();
        var tool = new MemoryWriteTool(
            _store,
            _manager,
            Options.Create(new MemoryToolOptions { MaxSummaryChars = 32 }),
            _rollout);
        var context = BuildContext(runId);
        var input = JsonDocument.Parse($$"""
            {
              "key": "too-long",
              "summary": "{{new string('x', 40)}}",
              "scope": "project"
            }
            """).RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Output.Should().Contain("summary exceeds");
    }

    [Fact]
    public async Task MemoryRead_RunScope_IsolatesFromProject()
    {
        var runId = Guid.NewGuid();
        var projectFp = _manager.ResolveFingerprint(SamplePlan());
        await _store.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(), runId, null, projectFp, MemoryKind.Episodic, "repair", "project-only",
            "project memory", null, 10, 0, DateTime.UtcNow));
        await _store.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(), runId, null, $"run:{runId:N}", MemoryKind.Episodic, "repair", "run-only",
            "run memory", null, 10, 0, DateTime.UtcNow));

        var tool = new MemoryReadTool(_store, _manager, Options.Create(new MemoryToolOptions()), _rollout);
        var context = BuildContext(runId);
        var input = JsonDocument.Parse("""{ "keyword": "memory", "scope": "run" }""").RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("run-only");
        result.Output.Should().NotContain("project-only");
    }

    private ToolContext BuildContext(Guid runId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-memory-tools-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core.FileStateCache(),
            Plan = SamplePlan(),
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    private static GenerationPlan SamplePlan() =>
        new(
            "DjangoApp",
            "Calorie tracker",
            new TechStack(["Python"], ["Django"], [], [], "django"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12-slim",
            Array.Empty<string>(),
            Array.Empty<string>());

    private sealed class StubRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "stub";
        public string SessionId => "stub";
        public string HostMountPath => string.Empty;
        public string GuestMountPath => "/workspace";
        public string Image => "stub";
        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class InMemoryRolloutRecorder : IRolloutRecorder, IRolloutReplayService
    {
        public List<RolloutEntry> Entries { get; } = new();

        public Task RecordMemoryOperationAsync(
            Guid runId, string sessionId, string operation, string scope, string? key, string? kind, int resultCount, CancellationToken ct = default)
        {
            Entries.Add(new RolloutEntry(
                "memory_operation",
                runId,
                sessionId,
                0,
                DateTime.UtcNow,
                JsonSerializer.Serialize(new { operation, scope, key, kind, resultCount })));
            return Task.CompletedTask;
        }

        public Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordToolUseAsync(Guid runId, string sessionId, int stepNumber, string toolName, string inputJson, string outputJson, bool success, long durationMs, IReadOnlyList<RolloutMediaAttachment>? media = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordStepFinishAsync(Guid runId, string sessionId, int stepNumber, string finishReason, RolloutUsage? usage = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordSkillActivationAsync(Guid runId, string sessionId, string skillName, bool firstActivation, bool consentGranted, int contentChars, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordCompactionAsync(Guid runId, string sessionId, int beforeChars, int afterChars, int beforeTurns, int afterTurns, string summaryJson, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RolloutEntry>>(Entries);
        public Task<IReadOnlyList<RolloutEntry>> ReplayAsync(Guid runId, CancellationToken ct = default) => GetRolloutAsync(runId, ct);
        public Task<IReadOnlyList<RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RolloutSearchHit>>(Array.Empty<RolloutSearchHit>());
    }
}
