using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraO2E2ETests : IDisposable
{
    private readonly string _runsRoot;
    private readonly FileSystemVerifyEvidenceStore _verifyEvidence;
    private readonly FileSystemObscuraEvidenceStore _obscuraEvidence;

    public ObscuraO2E2ETests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"obscura-o2-e2e-{Guid.NewGuid():N}");
        var verifyOptions = Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot });
        _verifyEvidence = new FileSystemVerifyEvidenceStore(
            verifyOptions,
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
        _obscuraEvidence = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = _runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);
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
    public async Task ShadowAppBoot_ObscuraNavigate_PersistsScreenshotToEvidenceStore()
    {
        await using var server = await StartLoopbackHttpServerAsync();
        var runId = Guid.NewGuid();

        var router = new ObscuraNetworkRouter(Options.Create(new ObscuraNetworkRouterOptions
        {
            DockerBrowserHost = "127.0.0.1",
            UseDockerHostMapping = false
        }));
        router.BindRun(runId, Guid.NewGuid());
        router.RegisterService(runId, "frontend", server.Port, "/");

        var browser = new E2EStubBrowser(server.BaseUrl);
        var recording = new ObscuraBrowserRecordingService(
            browser,
            Options.Create(new ObscuraBrowserRecordingOptions
            {
                RunsRoot = _runsRoot,
                FrameIntervalMs = 50,
                MaxFrames = 2
            }),
            NullLogger<ObscuraBrowserRecordingService>.Instance,
            _obscuraEvidence);
        var facade = new ObscuraBrowserToolFacade(browser, router, recording);
        var runner = new ObscuraVerifySmokeRunner(
            facade,
            Options.Create(new VerifySubagentOptions
            {
                EvidenceRoot = _runsRoot,
                EnableObscuraSmokeRunner = true
            }),
            NullLogger<ObscuraVerifySmokeRunner>.Instance,
            evidence: _obscuraEvidence,
            markdown: new DomToMarkdownConverter(),
            networkRouter: router);

        var target = new VerifySmokeTarget(
            "frontend",
            $"http://localhost:{server.Port}/",
            server.Port,
            VerifySmokeKind.Browser);

        var smoke = await runner.RunBrowserTargetsAsync(runId, [target]);

        smoke.Passed.Should().BeTrue();
        browser.Navigated.Should().ContainSingle(n => n.Contains($":{server.Port}"));

        var screenshotPath = Path.Combine(_runsRoot, runId.ToString("D"), "verify", "screenshot-final.png");
        File.Exists(screenshotPath).Should().BeTrue();

        var verifyBundle = _verifyEvidence.List(runId);
        verifyBundle.Artifacts.Should().Contain(a => a.Kind == VerifyEvidenceKind.Screenshot);
        verifyBundle.Artifacts.Single(a => a.Kind == VerifyEvidenceKind.Screenshot).FileName
            .Should().Be("screenshot-final.png");

        var obscuraBundle = _obscuraEvidence.List(runId);
        obscuraBundle.Artifacts.Should().Contain(a => a.Kind == ObscuraEvidenceKind.Screenshot);
    }

    [Fact]
    public async Task VerifyFail_RepairPrompt_ContainsScreenshotPathAndConsoleErrors()
    {
        var context = CreateMinimalContext();
        var runId = context.Orchestrator.Id;
        var verifyDir = _verifyEvidence.GetEvidenceDirectory(runId);
        Directory.CreateDirectory(verifyDir);

        await File.WriteAllBytesAsync(
            Path.Combine(verifyDir, "screenshot-final.png"),
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        await File.WriteAllTextAsync(
            Path.Combine(verifyDir, "console-errors.json"),
            """[{"level":"error","message":"TypeError: Cannot read properties of undefined"}]""");

        var failureStore = new VerifyFailureContextStore();
        var service = CreateFailingVerifyService(failureStore, runId);

        var result = await service.RunAsync(context);
        result.Passed.Should().BeFalse();

        failureStore.TryGet(runId, out var evidence).Should().BeTrue();
        evidence!.ReportText.Should().Contain("screenshot-final.png");
        evidence.ReportText.Should().Contain("console-errors.json");
        evidence.ReportText.Should().Contain("screenshot_evidence=");
        evidence.ReportText.Should().Contain("console_errors_evidence=");

        var manager = new ContextFragmentManager(Options.Create(new ContextFragmentOptions()));
        var assembler = new ContextFragmentRepairAssembler(manager);
        var repairPrompt = assembler.Assemble(new RepairFragmentInput(
            BuildLog: "verify gate failed after obscura smoke",
            Errors: [new ErrorReport("VerifyError", "obscura smoke failed", "", "frontend/App.tsx", 12)],
            WorkingFiles: context.Orchestrator.Files.ToList(),
            VerifyEvidence: evidence.Summary + "\n" + evidence.ReportText));

        repairPrompt.Should().Contain("screenshot-final.png");
        repairPrompt.Should().Contain("console-errors.json");
        repairPrompt.Should().Contain("[fragment:verify_evidence:");
    }

    private VerifySubagentService CreateFailingVerifyService(VerifyFailureContextStore failureStore, Guid runId)
    {
        var registry = new VerifyRecipeRegistry(
            new VerifyRecipeLlmDetector(
                new Moq.Mock<Libr4.AI.Application.Abstractions.IAIService>().Object,
                VerifyRecipeCatalog.BuildAll().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase),
                Options.Create(new VerifySubagentOptions { EnableRecipeLlmFallback = false }),
                NullLogger<VerifyRecipeLlmDetector>.Instance),
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _runsRoot }),
            NullLogger<VerifyRecipeRegistry>.Instance);

        var orchestrator = new Mock<IVerifyOrchestrator>();
        orchestrator.Setup(o => o.PrepareVerifyRun(
                It.IsAny<GenerationContext>(),
                It.IsAny<VerifyRecipeDetectionResult>(),
                It.IsAny<string>()))
            .Returns((GenerationContext ctx, VerifyRecipeDetectionResult detection, string dir) =>
                new VerifyRunPlan(
                    ctx.Orchestrator.Id,
                    detection.Recipe,
                    dir,
                    null,
                    ctx.Plan!.RuntimeImage,
                    detection.ManifestPath,
                    true,
                    detection.DetectionMethod));

        orchestrator.Setup(o => o.RunVerifyOrchestrationAsync(
                It.IsAny<GenerationContext>(),
                It.IsAny<VerifyRunPlan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyOrchestrationResult(
                ShadowPassed: true,
                ReadinessPassed: true,
                AgentPassed: false,
                AgentSummary: "obscura_smoke=fail: navigate timeout",
                ReadinessResults: [],
                Path.Combine(_runsRoot, runId.ToString("D"), "verify", "readiness.json"),
                Path.Combine(_runsRoot, runId.ToString("D"), "verify", "verify-failure-evidence.json")));

        return new VerifySubagentService(
            registry,
            orchestrator.Object,
            new VerifyGateService(),
            failureStore,
            _verifyEvidence,
            Options.Create(new VerifySubagentOptions
            {
                Enabled = true,
                EvidenceRoot = _runsRoot
            }),
            Options.Create(new AutonomousBenchmarkModeOptions()),
            PlatformUtilizationTestOptions.Production,
            NullLogger<VerifySubagentService>.Instance);
    }

    private static GenerationContext CreateMinimalContext()
    {
        var orchestrator = AppGenerationOrchestrator.Create(
            "calorie vision verify e2e",
            "fp-o2-e2e");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "CalorieVision",
            applicationDescription: "E2E",
            techStack: new TechStack(["TypeScript"], ["SolidJS"], [], [], "solidjs"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "node:20",
            buildCommands: ["npm run build"],
            testCommands: ["npm test"],
            maxIterations: 2));
        orchestrator.UpsertFile(new GeneratedFile(
            "frontend/package.json",
            "json",
            """{"dependencies":{"solid-js":"^1.8.0"}}"""));
        orchestrator.MarkCompleted();

        return new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = "calorie vision verify e2e",
            Plan = orchestrator.Plan,
            Items = { ["tests_passed"] = true }
        };
    }

    private static async Task<LoopbackServer> StartLoopbackHttpServerAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cts.Token);
                }
                catch
                {
                    break;
                }

                await using var stream = client.GetStream();
                var buffer = new byte[1024];
                _ = await stream.ReadAsync(buffer, cts.Token);
                var body = "<html><body><button id=\"boot\">Booted</button></body></html>"u8.ToArray();
                var header = Encoding.UTF8.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nContent-Length: {body.Length}\r\n\r\n");
                await stream.WriteAsync(header, cts.Token);
                await stream.WriteAsync(body, cts.Token);
            }
        }, cts.Token);

        return new LoopbackServer(listener, cts, port);
    }

    private sealed class LoopbackServer(TcpListener listener, CancellationTokenSource cts, int port) : IAsyncDisposable
    {
        public int Port { get; } = port;
        public string BaseUrl => $"http://127.0.0.1:{port}/";

        public async ValueTask DisposeAsync()
        {
            await cts.CancelAsync();
            listener.Stop();
            cts.Dispose();
        }
    }

    private sealed class E2EStubBrowser(string bootUrl) : IObscuraBrowserService
    {
        public List<string> Navigated { get; } = [];

        public Task<string> LaunchBrowserAsync(CancellationToken ct = default)
            => LaunchBrowserAsync(new ObscuraLaunchOptions(), ct);

        public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default)
            => Task.FromResult("e2e-session");

        public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
        {
            Navigated.Add(url);
            return Task.CompletedTask;
        }

        public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default)
        {
            if (script.Contains("data-libr4-ref", StringComparison.Ordinal))
            {
                return Task.FromResult("""
                    {"url":"http://127.0.0.1/","title":"ShadowBoot","nodes":[
                      {"ref":"e1","tag":"button","text":"Booted","selector":"#boot"}
                    ]}
                    """);
            }

            if (script.Contains("__libr4Console", StringComparison.Ordinal))
                return Task.FromResult("""[{"level":"error","message":"shadow boot warning"}]""");

            return Task.FromResult("Booted");
        }

        public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult($"<html><body>shadow app booted at {bootUrl}</body></html>");

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
}
