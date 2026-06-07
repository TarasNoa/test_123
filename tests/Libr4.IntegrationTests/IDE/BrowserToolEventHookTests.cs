using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BrowserToolEventHookTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly AgentEventEmitter _emitter = new(NullLogger<AgentEventEmitter>.Instance);
    private readonly FileRolloutRecorder _rollout;
    private readonly NdjsonEventWriter _ndjson;

    public BrowserToolEventHookTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"libr4-browser-events-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_runsRoot);
        var options = Options.Create(new AgentRuntimeOptions
        {
            RunsRoot = _runsRoot,
            RolloutDbPath = Path.Combine(_runsRoot, "rollout.db"),
            EnableNdjsonEvents = true
        });
        _rollout = new FileRolloutRecorder(options);
        _ndjson = new NdjsonEventWriter(options, new AgentRuntimeEventHub());
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
    public async Task Hook_EmitsAgentEvent_AndRollout_ForBrowserNavigate()
    {
        var runId = Guid.NewGuid();
        var hook = new BrowserToolEventHook(_emitter, _rollout, _ndjson, Options.Create(new AgentRuntimeOptions
        {
            RunsRoot = _runsRoot,
            RolloutDbPath = Path.Combine(_runsRoot, "rollout.db"),
            EnableNdjsonEvents = true
        }));

        var context = BuildContext(runId, """{ "session_id": "abc123", "url": "https://example.com" }""");
        var result = new ToolExecutionResult(
            BrowserToolNames.Navigate,
            true,
            "navigated session=abc123 url=https://example.com",
            Array.Empty<GeneratedFile>());

        await hook.OnAfterToolAsync(new StubNamedTool(BrowserToolNames.Navigate), context, result, CancellationToken.None);

        var events = await _emitter.GetEventsForRun(runId);
        events.Should().ContainSingle();
        events[0].Type.Should().Be(AgentEventType.BrowserNavigate);

        var rolloutPath = Path.Combine(_runsRoot, runId.ToString("D"), "rollout.jsonl");
        File.Exists(rolloutPath).Should().BeTrue();
        var line = File.ReadAllText(rolloutPath).Trim();
        line.Should().Contain("tool_use");
        line.Should().Contain("browser_navigate");
    }

    [Fact]
    public async Task Hook_Screenshot_PersistsMedia_ToRolloutAndNdjson()
    {
        var runId = Guid.NewGuid();
        var hook = new BrowserToolEventHook(_emitter, _rollout, _ndjson, Options.Create(new AgentRuntimeOptions
        {
            RunsRoot = _runsRoot,
            RolloutDbPath = Path.Combine(_runsRoot, "rollout.db"),
            EnableNdjsonEvents = true
        }));

        var png = Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var context = BuildContext(runId, """{ "session_id": "shot1" }""");
        context.Session.CurrentStepNumber = 3;
        var result = new ToolExecutionResult(
            BrowserToolNames.Screenshot,
            true,
            $"session=shot1\ncontent_type=image/png\nbase64={png}",
            Array.Empty<GeneratedFile>());

        await hook.OnAfterToolAsync(new StubNamedTool(BrowserToolNames.Screenshot), context, result, CancellationToken.None);

        var screenshotPath = Path.Combine(_runsRoot, runId.ToString("D"), "obscura", "screenshot-step3.png");
        File.Exists(screenshotPath).Should().BeTrue();

        var rollout = File.ReadAllText(Path.Combine(_runsRoot, runId.ToString("D"), "rollout.jsonl"));
        rollout.Should().Contain("screenshot-step3.png");
        rollout.Should().Contain("screenshot");

        var eventsPath = Path.Combine(_runsRoot, runId.ToString("D"), "events.jsonl");
        File.Exists(eventsPath).Should().BeTrue();
        File.ReadAllText(eventsPath).Should().Contain("\"media\"");
    }

    private ToolContext BuildContext(Guid runId, string inputJson)
    {
        var tempDir = Path.Combine(_runsRoot, "ws");
        Directory.CreateDirectory(tempDir);
        using var doc = JsonDocument.Parse(inputJson);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core.FileStateCache(),
            Plan = null,
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId, LastToolInputJson = inputJson },
            ToolInput = doc.RootElement.Clone()
        };
    }

    private sealed class StubNamedTool(string name) : IAgentTool
    {
        public string Name => name;
        public string Description => name;
        public bool IsReadOnly => true;
        public bool IsConcurrencySafe(JsonElement input) => false;
        public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
            => Task.FromResult(new ToolExecutionResult(name, true, "ok", Array.Empty<GeneratedFile>()));
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
}
