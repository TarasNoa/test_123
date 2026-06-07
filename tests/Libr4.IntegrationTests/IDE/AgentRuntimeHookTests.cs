using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentRuntimeHookTests : IDisposable
{
    private readonly string _runsRoot;

    public AgentRuntimeHookTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"hooks-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_runsRoot))
                Directory.Delete(_runsRoot, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task MemoryPrefetchHook_RepairTool_SetsActiveContext()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"hermes-hook-{Guid.NewGuid():N}.db");
        var store = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        var manager = new HermesMemoryManager(
            store,
            Options.Create(new HermesMemoryManagerOptions { EnablePrefetch = true, PrefetchTopK = 3 }),
            NullLogger<HermesMemoryManager>.Instance);
        var hook = new MemoryPrefetchToolHook(
            manager,
            Options.Create(new HermesMemoryManagerOptions { EnablePrefetch = true }),
            new HookRolloutRecorder());

        var runId = Guid.NewGuid();
        var fingerprint = HermesMemoryScopeResolver.ResolveProjectFingerprint(SamplePlan());
        await store.UpsertAsync(new HermesMemoryEntry(
            Guid.NewGuid(),
            runId,
            null,
            fingerprint,
            MemoryKind.Procedural,
            "fixing",
            "tool:run_build",
            "CS0246 missing type Foo in Program.cs",
            null,
            32,
            1.0,
            DateTime.UtcNow));

        var session = new AgentSessionState { RunId = runId, LastErrors = ["CS0246 missing type"] };
        var context = BuildContext(session, AgentSessionMode.Repair, "CS0246 missing type Foo");
        var tool = new StubTool("apply_patch");

        await hook.OnBeforeToolAsync(tool, context, CancellationToken.None);

        context.Session.ActiveLibr4Context.Should().Contain("relevant_memory");
    }

    [Fact]
    public async Task EvidenceCaptureHook_RunBuild_PersistsVerifyArtifact()
    {
        var verify = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
        var hook = new EvidenceCaptureToolHook(
            Options.Create(new AgentRuntimeOptions { EnableEvidenceCaptureHook = true }),
            NullLogger<EvidenceCaptureToolHook>.Instance,
            verify);

        var runId = Guid.NewGuid();
        var session = new AgentSessionState { RunId = runId, CurrentStepNumber = 2 };
        var context = BuildContext(session, AgentSessionMode.Repair, buildLog: null);
        var tool = new StubTool("run_build");
        var result = new ToolExecutionResult("run_build", false, "BUILD FAILED: error CS1002", Array.Empty<GeneratedFile>());

        await hook.OnAfterToolAsync(tool, context, result, CancellationToken.None);

        var bundle = verify.List(runId);
        bundle.Artifacts.Should().ContainSingle(a => a.FileName.Contains("run_build"));
    }

    [Fact]
    public void HermesScopeResolver_SpaceFingerprint_RoundTrips()
    {
        var spaceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var fp = HermesMemoryScopeResolver.BuildSpaceFingerprint(spaceId);
        HermesMemoryScopeResolver.TryParseSpaceFingerprint(fp, out var parsed).Should().BeTrue();
        parsed.Should().Be(spaceId);
        HermesMemoryScopeResolver.IsValidScope(fp).Should().BeTrue();
    }

    private static ToolContext BuildContext(AgentSessionState session, AgentSessionMode mode, string? buildLog)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-hooks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Plan = SamplePlan(),
            BuildLog = buildLog,
            Mode = mode,
            Session = session,
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    private static GenerationPlan SamplePlan() =>
        new(
            "Demo",
            "demo",
            new TechStack(["C#"], [".NET"], [], [], "dotnet"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            Array.Empty<string>(),
            Array.Empty<string>());

    private sealed class StubTool(string name) : IAgentTool
    {
        public string Name { get; } = name;
        public string Description => name;
        public bool IsReadOnly => false;
        public bool IsConcurrencySafe(JsonElement input) => false;
        public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct) =>
            Task.FromResult(new ToolExecutionResult(Name, true, "ok", Array.Empty<GeneratedFile>()));
    }

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

    private sealed class HookRolloutRecorder : IRolloutRecorder
    {
        public Task RecordMemoryOperationAsync(
            Guid runId,
            string sessionId,
            string operation,
            string scope,
            string? key,
            string? kind,
            int resultCount,
            CancellationToken ct = default) => Task.CompletedTask;

        public Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordToolUseAsync(Guid runId, string sessionId, int stepNumber, string toolName, string inputJson, string outputJson, bool success, long durationMs, IReadOnlyList<RolloutMediaAttachment>? media = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordStepFinishAsync(Guid runId, string sessionId, int stepNumber, string finishReason, RolloutUsage? usage = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordSkillActivationAsync(Guid runId, string sessionId, string skillName, bool firstActivation, bool consentGranted, int contentChars, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordCompactionAsync(Guid runId, string sessionId, int beforeChars, int afterChars, int beforeTurns, int afterTurns, string summaryJson, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RolloutEntry>>(Array.Empty<RolloutEntry>());
        public Task<IReadOnlyList<RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RolloutSearchHit>>(Array.Empty<RolloutSearchHit>());
    }
}
