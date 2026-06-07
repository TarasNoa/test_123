using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Computer;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ComputerSubagentTests : IDisposable
{
    private readonly string _runsRoot;

    public ComputerSubagentTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"computer-subagent-{Guid.NewGuid():N}");
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

    [Fact]
    public void Parser_DetectsLoginFlowFromJson()
    {
        var request = ComputerFlowRequestParser.Parse(
            """{"flow":"login-flow","url":"http://localhost:3000/login","username":"demo","password":"secret"}""",
            BuildContext(Guid.NewGuid()));

        request.Flow.Should().Be(ComputerFlowNames.LoginFlow);
        request.Url.Should().Be("http://localhost:3000/login");
        request.Parameters["username"].Should().Be("demo");
        request.HasDeterministicFlow.Should().BeTrue();
    }

    [Fact]
    public void ComputerAgentYaml_DefinesBrowserTasks()
    {
        var path = ResolveSpecPath("computer.agent.yaml");
        var doc = AgentSpecLoader.LoadFromFile(path);

        doc.Browser.Should().NotBeNull();
        doc.Browser!.Tasks.Select(t => t.TaskName).Should().Contain(
        [
            ComputerFlowNames.LoginFlow,
            ComputerFlowNames.FormFill,
            ComputerFlowNames.VisualDesignCheck
        ]);
        doc.Toolset.Should().Contain("browser_navigate");
        doc.Toolset.Should().Contain("read_file");
        doc.Toolset.Should().Contain("bash");
    }

    [Fact]
    public async Task LoginFlow_OnStubBrowser_PassesWithEvidence()
    {
        var runId = Guid.NewGuid();
        var browser = new LoginStubBrowser();
        var facade = new ObscuraBrowserToolFacade(browser);
        var runner = new ComputerFlowRunner(
            facade,
            Options.Create(new ComputerSubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<ComputerFlowRunner>.Instance);

        var request = new ComputerFlowRequest(
            ComputerFlowNames.LoginFlow,
            "http://127.0.0.1:3000/login",
            new Dictionary<string, string>
            {
                ["url"] = "http://127.0.0.1:3000/login",
                ["username"] = "demo@libr4.local",
                ["password"] = "secret",
                ["success_selector"] = "#dashboard"
            },
            "login flow");

        var result = await runner.RunAsync(request, BuildContext(runId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.UsedDeterministicFlow.Should().BeTrue();
        result.EvidenceDir.Should().NotBeNullOrEmpty();
        Directory.Exists(result.EvidenceDir!).Should().BeTrue();
        File.Exists(Path.Combine(result.EvidenceDir!, "screenshot.png")).Should().BeTrue();
        browser.Typed.Should().Contain(t => t.Text == "secret");
        Assert.Single(browser.Clicked);
        Assert.Contains("submit", browser.Clicked[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VisualDesignCheck_OnStubBrowser_Passes()
    {
        var runId = Guid.NewGuid();
        var browser = new LoginStubBrowser();
        var facade = new ObscuraBrowserToolFacade(browser);
        var runner = new ComputerFlowRunner(
            facade,
            Options.Create(new ComputerSubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<ComputerFlowRunner>.Instance);

        var request = new ComputerFlowRequest(
            ComputerFlowNames.VisualDesignCheck,
            "http://127.0.0.1:5173/",
            new Dictionary<string, string> { ["url"] = "http://127.0.0.1:5173/" },
            "visual design check");

        var result = await runner.RunAsync(request, BuildContext(runId), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        result.Summary.Should().Contain("visual-design-check PASS");
    }

    private static ToolContext BuildContext(Guid runId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-computer-test-" + Guid.NewGuid().ToString("N"));
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
            ToolInput = default
        };
    }

    private static string ResolveSpecPath(string fileName) =>
        Path.Combine(ResolveSpecsDirectory(), fileName);

    private static string ResolveSpecsDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Agents", "Subagents"),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "src", "Services", "IDE", "Libr4.IDE.AutonomousAppGeneration", "Agents", "Subagents"))
        };

        return candidates.First(Directory.Exists);
    }

    private sealed class LoginStubBrowser : IObscuraBrowserService
    {
        public List<(string Selector, string Text)> Typed { get; } = [];
        public List<string> Clicked { get; } = [];

        public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
            => Task.FromResult("computer-session");

        public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
            => Task.FromResult("computer-session");

        public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default) =>
            Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
        {
            if (script.Contains("#dashboard", StringComparison.Ordinal) || script.Contains("dashboard", StringComparison.Ordinal))
                return Task.FromResult("Welcome back");
            if (script.Contains("title", StringComparison.Ordinal))
                return Task.FromResult("Libr4 App");
            if (script.Contains("__libr4Console", StringComparison.Ordinal))
                return Task.FromResult("[]");
            return Task.FromResult(string.Empty);
        }

        public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default) =>
            Task.FromResult("<html><body><main><h1>Dashboard</h1></main></body></html>");

        public Task ClickAsync(string sessionId, string selector, CancellationToken ct = default)
        {
            Clicked.Add(selector);
            return Task.CompletedTask;
        }

        public Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default)
        {
            Typed.Add((selector, text));
            return Task.CompletedTask;
        }

        public Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task CloseBrowserAsync(string sessionId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId) =>
            Task.FromResult<ObscuraSessionInfo?>(new ObscuraSessionInfo { SessionId = sessionId, IsActive = true });

        public Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync() =>
            Task.FromResult<IReadOnlyList<ObscuraSessionInfo>>(Array.Empty<ObscuraSessionInfo>());

        public Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default) =>
            Task.FromResult(new AgentBrowserResult { TaskId = task.TaskId, Success = true });
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
