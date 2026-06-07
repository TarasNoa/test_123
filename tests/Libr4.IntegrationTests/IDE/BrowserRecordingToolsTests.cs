using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BrowserRecordingToolsTests : IDisposable
{
    private readonly string _runsRoot;

    public BrowserRecordingToolsTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"libr4-recording-{Guid.NewGuid():N}");
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
    public async Task RecordStartStop_PersistsSmokeWebm_ToVerifyDirectory()
    {
        var browser = new RecordingStubBrowser();
        var evidence = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);
        var recording = new ObscuraBrowserRecordingService(
            browser,
            Options.Create(new ObscuraBrowserRecordingOptions
            {
                RunsRoot = _runsRoot,
                FrameIntervalMs = 50,
                MaxFrames = 3
            }),
            NullLogger<ObscuraBrowserRecordingService>.Instance,
            evidence);
        var facade = new ObscuraBrowserToolFacade(browser, recording: recording);
        var start = new BrowserRecordStartTool(facade);
        var stop = new BrowserRecordStopTool(facade);
        var runId = Guid.NewGuid();
        var context = BuildContext(runId, sessionId: "sess1");

        var startResult = await start.ExecuteAsync(
            JsonDocument.Parse("""{ "session_id": "sess1" }""").RootElement,
            context,
            CancellationToken.None);
        startResult.Success.Should().BeTrue();

        await Task.Delay(120);
        context.Session.CurrentStepNumber = 2;
        var stopResult = await stop.ExecuteAsync(
            JsonDocument.Parse("""{ "session_id": "sess1" }""").RootElement,
            context,
            CancellationToken.None);

        stopResult.Success.Should().BeTrue();
        stopResult.Output.Should().Contain("smoke.webm");

        var verifyPath = Path.Combine(_runsRoot, runId.ToString("D"), "verify", "smoke.webm");
        File.Exists(verifyPath).Should().BeTrue();
        browser.ScreenshotCount.Should().BeGreaterThan(0);

        var bundle = evidence.List(runId);
        bundle.Artifacts.Should().Contain(a => a.Kind == ObscuraEvidenceKind.Video);
        File.Exists(Path.Combine(evidence.GetObscuraDirectory(runId), "manifest.json")).Should().BeTrue();
    }

    private static ToolContext BuildContext(Guid runId, string sessionId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-recording-ctx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Plan = null,
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId, SessionId = sessionId },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    private sealed class RecordingStubBrowser : IObscuraBrowserService
    {
        public int ScreenshotCount { get; private set; }

        public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
            => Task.FromResult("sess1");

        public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
            => Task.FromResult("sess1");

        public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default) => Task.CompletedTask;

        public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
        {
            ScreenshotCount++;
            return Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        }

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
