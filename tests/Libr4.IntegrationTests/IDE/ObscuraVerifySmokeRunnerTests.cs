using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraVerifySmokeRunnerTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly SmokeStubBrowser _browser;
    private readonly ObscuraVerifySmokeRunner _runner;

    public ObscuraVerifySmokeRunnerTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"obscura-smoke-{Guid.NewGuid():N}");
        _browser = new SmokeStubBrowser();
        var evidence = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);
        var recording = new ObscuraBrowserRecordingService(
            _browser,
            Options.Create(new ObscuraBrowserRecordingOptions
            {
                RunsRoot = _runsRoot,
                FrameIntervalMs = 50,
                MaxFrames = 2
            }),
            NullLogger<ObscuraBrowserRecordingService>.Instance,
            evidence);
        var facade = new ObscuraBrowserToolFacade(_browser, recording: recording);
        _runner = new ObscuraVerifySmokeRunner(
            facade,
            Options.Create(new VerifySubagentOptions
            {
                EnableObscuraSmokeRunner = true,
                ObscuraSmokeWaitSelector = "body",
                ObscuraSmokeWaitTimeoutMs = 2_000
            }),
            NullLogger<ObscuraVerifySmokeRunner>.Instance,
            evidence: evidence,
            markdown: new DomToMarkdownConverter());
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
    public async Task FullSmokeFlow_RecordNavigateClickScreenshot_ProducesVerifyEvidence()
    {
        var runId = Guid.NewGuid();
        var targets = new[]
        {
            new VerifySmokeTarget("frontend", "http://localhost:5173/", 5173, VerifySmokeKind.Browser)
        };

        var result = await _runner.RunBrowserTargetsAsync(runId, targets);

        result.Passed.Should().BeTrue();
        result.Targets.Should().ContainSingle();
        result.Targets[0].Summary.Should().Contain("PASS");

        _browser.ClickedSelectors.Should().NotBeEmpty();
        _browser.Navigated.Should().ContainSingle(n => n.Url.Contains("5173"));

        File.Exists(Path.Combine(_runsRoot, runId.ToString("D"), "verify", "screenshot-final.png")).Should().BeTrue();
        File.Exists(Path.Combine(_runsRoot, runId.ToString("D"), "verify", "console-errors.json")).Should().BeTrue();
        File.Exists(Path.Combine(_runsRoot, runId.ToString("D"), "verify", "dom-snapshot.md")).Should().BeTrue();
        File.Exists(Path.Combine(_runsRoot, runId.ToString("D"), "verify", "smoke.webm")).Should().BeTrue();
    }

    [Fact]
    public void TryPickClickSelector_PrefersButtonOrAnchorFromSnapshot()
    {
        const string snapshot = """
            {"url":"http://localhost:5173/","title":"App","nodes":[
              {"ref":"e1","tag":"div","text":"wrap","selector":"[data-libr4-ref=\"e1\"]"},
              {"ref":"e2","tag":"button","text":"Go","selector":"[data-libr4-ref=\"e2\"]"}
            ]}
            """;

        ObscuraVerifySmokeRunner.TryPickClickSelector(snapshot)
            .Should().Be("[data-libr4-ref=\"e2\"]");
    }

    [Fact]
    public async Task CalorieVisionRecipe_BrowserTarget_CompletesSmoke()
    {
        var runId = Guid.NewGuid();
        var recipe = VerifyRecipeCatalog.BuildAll().Single(r => r.Id == "calorie-vision");
        var browserTarget = recipe.SmokeTargets.Single(t => t.Kind == VerifySmokeKind.Browser);

        var result = await _runner.RunBrowserTargetsAsync(runId, [browserTarget]);

        result.Passed.Should().BeTrue();
        result.Summary.Should().Contain("obscura smoke passed");
    }

    [Fact]
    public async Task BankingRecipe_BrowserTarget_CompletesSmoke()
    {
        var runId = Guid.NewGuid();
        var recipe = VerifyRecipeCatalog.BuildAll().Single(r => r.Id == "banking");
        var browserTarget = recipe.SmokeTargets.Single(t => t.Kind == VerifySmokeKind.Browser);

        var result = await _runner.RunBrowserTargetsAsync(runId, [browserTarget]);

        result.Passed.Should().BeTrue();
        _browser.Navigated.Should().ContainSingle(n => n.Url.Contains("3000"));
    }

    private sealed class SmokeStubBrowser : IObscuraBrowserService
    {
        public List<(string SessionId, string Url)> Navigated { get; } = [];
        public List<string> ClickedSelectors { get; } = [];

        public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
            => LaunchBrowserAsync(new ObscuraLaunchOptions(), ct);

        public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
            => Task.FromResult("smoke-session");

        public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
        {
            Navigated.Add((sessionId, url));
            return Task.CompletedTask;
        }

        public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
        {
            if (script.Contains("data-libr4-ref", StringComparison.Ordinal))
            {
                return Task.FromResult("""
                    {"url":"http://localhost:5173/","title":"Smoke","nodes":[
                      {"ref":"e1","tag":"button","text":"Start","selector":"[data-libr4-ref=\"e1\"]"}
                    ]}
                    """);
            }

            if (script.Contains("__libr4Console", StringComparison.Ordinal))
                return Task.FromResult("[]");

            return Task.FromResult("{}");
        }

        public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult("<html><body><button>Start</button></body></html>");

        public Task ClickAsync(string sessionId, string selector, CancellationToken ct = default)
        {
            ClickedSelectors.Add(selector);
            return Task.CompletedTask;
        }

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
}
