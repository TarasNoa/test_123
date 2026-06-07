using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BrowserAgentToolsTests
{
    [Fact]
    public async Task BrowserLaunch_AndNavigate_ReturnSessionAndUrl()
    {
        var browser = new StubObscuraBrowserService();
        var facade = new ObscuraBrowserToolFacade(browser);
        var launch = new BrowserLaunchTool(facade);
        var navigate = new BrowserNavigateTool(facade);
        var runId = Guid.NewGuid();
        var context = BuildContext(runId);

        var launchResult = await launch.ExecuteAsync(JsonDocument.Parse("{}").RootElement, context, CancellationToken.None);
        launchResult.Success.Should().BeTrue();
        var sessionId = launchResult.Output.Split('=')[1];

        var navInput = JsonDocument.Parse($$"""{ "session_id": "{{sessionId}}", "url": "https://example.com" }""").RootElement;
        var navResult = await navigate.ExecuteAsync(navInput, context, CancellationToken.None);

        navResult.Success.Should().BeTrue();
        navResult.Output.Should().Contain("https://example.com");
        browser.Navigated.Should().ContainSingle();
    }

    [Fact]
    public async Task BrowserLaunch_WithSameRunId_ReusesSession()
    {
        var browser = new StubObscuraBrowserService();
        var facade = new ObscuraBrowserToolFacade(browser);
        var launch = new BrowserLaunchTool(facade);
        var runId = Guid.NewGuid();
        var context = BuildContext(runId);

        var first = await launch.ExecuteAsync(JsonDocument.Parse("{}").RootElement, context, CancellationToken.None);
        var second = await launch.ExecuteAsync(JsonDocument.Parse("{}").RootElement, context, CancellationToken.None);

        first.Output.Should().Be(second.Output);
        browser.LaunchCount.Should().Be(2);
        browser.LaunchedRunIds.Should().OnlyContain(r => r == runId.ToString("D"));
    }

    [Fact]
    public void FilteredRegistry_IncludesOnlyVerifyBrowserToolset()
    {
        var tools = BrowserToolNames.All
            .Select(name => (IAgentTool)new StubNamedTool(name))
            .Concat([new StubNamedTool("bash"), new StubNamedTool("mcp")])
            .ToList();

        var inner = new AgentToolRegistry(tools);
        var filtered = new FilteredAgentToolRegistry(inner,
        [
            "browser_launch", "browser_navigate", "browser_screenshot", "bash"
        ]);

        filtered.All.Select(t => t.Name).Should().BeEquivalentTo(
            ["bash", "browser_launch", "browser_navigate", "browser_screenshot"]);
        filtered.TryGet("browser_close").Should().BeNull();
        filtered.TryGet("browser_launch").Should().NotBeNull();
    }

    private static ToolContext BuildContext(Guid runId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-browser-tools-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Plan = null,
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    private sealed class StubObscuraBrowserService : IObscuraBrowserService
    {
        private readonly Dictionary<string, string> _runSessions = new(StringComparer.Ordinal);
        public int LaunchCount { get; private set; }
        public List<string> LaunchedRunIds { get; } = [];
        public List<(string SessionId, string Url)> Navigated { get; } = [];

        public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
            => LaunchBrowserAsync(new ObscuraLaunchOptions(), ct);

        public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
        {
            LaunchCount++;
            if (!string.IsNullOrWhiteSpace(options.RunId))
            {
                LaunchedRunIds.Add(options.RunId);
                if (_runSessions.TryGetValue(options.RunId, out var existing))
                    return Task.FromResult(existing);
            }

            var id = Guid.NewGuid().ToString("N")[..12];
            if (!string.IsNullOrWhiteSpace(options.RunId))
                _runSessions[options.RunId] = id;
            return Task.FromResult(id);
        }

        public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
        {
            Navigated.Add((sessionId, url));
            return Task.CompletedTask;
        }

        public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47]);

        public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
            => Task.FromResult("{}");

        public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult("<html></html>");

        public Task ClickAsync(string sessionId, string selector, CancellationToken ct = default) => Task.CompletedTask;
        public Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default) => Task.CompletedTask;

        public Task CloseBrowserAsync(string sessionId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId)
            => Task.FromResult<ObscuraSessionInfo?>(new ObscuraSessionInfo { SessionId = sessionId, IsActive = true });

        public Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync()
            => Task.FromResult<IReadOnlyList<ObscuraSessionInfo>>(Array.Empty<ObscuraSessionInfo>());

        public Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default)
            => Task.FromResult(new AgentBrowserResult { TaskId = task.TaskId, Success = true });
    }

    private sealed class StubNamedTool(string name) : IAgentTool
    {
        public string Name => name;
        public string Description => name;
        public bool IsReadOnly => true;
        public bool IsConcurrencySafe(JsonElement input) => true;
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
