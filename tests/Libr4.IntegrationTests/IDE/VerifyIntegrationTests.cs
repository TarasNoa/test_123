using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class VerifyIntegrationTests : IDisposable
{
    private readonly string _evidenceRoot;
    private readonly VerifyFailureContextStore _failureStore = new();
    private readonly FileSystemVerifyEvidenceStore _evidenceStore;

    public VerifyIntegrationTests()
    {
        _evidenceRoot = Path.Combine(Path.GetTempPath(), $"verify-integration-{Guid.NewGuid():N}");
        _evidenceStore = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions
            {
                EvidenceRoot = _evidenceRoot,
                ReadinessMaxAttempts = 5,
                ReadinessPollIntervalMs = 100,
                ReadinessRequestTimeoutSeconds = 2
            }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_evidenceRoot))
                Directory.Delete(_evidenceRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task DjangoBoot_ReadinessProbeSucceeds_AndScreenshotCaptured()
    {
        await using var server = await StartLoopbackHttpServerAsync();
        var runId = Guid.NewGuid();
        var evidenceDir = _evidenceStore.GetEvidenceDirectory(runId);
        Directory.CreateDirectory(evidenceDir);

        var probe = new VerifyReadinessProbe(
            Options.Create(new VerifySubagentOptions
            {
                EvidenceRoot = _evidenceRoot,
                ReadinessMaxAttempts = 8,
                ReadinessPollIntervalMs = 100,
                ReadinessRequestTimeoutSeconds = 2
            }),
            NullLogger<VerifyReadinessProbe>.Instance);

        var target = new VerifySmokeTarget("django-backend", server.BaseUrl, server.Port);
        var readiness = await probe.ProbeAsync(target, shadowWorkspaceId: null, evidenceDir);

        readiness.Ready.Should().BeTrue();
        File.Exists(Path.Combine(evidenceDir, "readiness-django-backend.json")).Should().BeTrue();

        await using (var screenshot = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            await _evidenceStore.PersistAsync(runId, VerifyEvidenceKind.Screenshot, screenshot);
        }

        var bundle = _evidenceStore.List(runId);
        bundle.Artifacts.Should().Contain(a => a.Kind == VerifyEvidenceKind.Screenshot);
        bundle.ThumbnailUrl.Should().NotBeNullOrWhiteSpace();
        bundle.Artifacts.Should().Contain(a => a.Kind == VerifyEvidenceKind.Readiness);
    }

    [Fact]
    public async Task VerifyFailure_RepairReceivesEvidenceInContext()
    {
        var runId = Guid.NewGuid();
        var service = CreateVerifyService(orchestrationPassed: false);
        var context = CreateCalorieContext(testsPassed: true);

        var result = await service.RunAsync(context);
        result.Passed.Should().BeFalse();

        _failureStore.TryGet(context.Orchestrator.Id, out var evidence).Should().BeTrue();
        evidence!.Summary.Should().Contain("verify gate failed");

        var manager = new ContextFragmentManager(Options.Create(new ContextFragmentOptions()));
        var assembler = new ContextFragmentRepairAssembler(manager);
        var fragments = assembler.Assemble(new RepairFragmentInput(
            BuildLog: "django manage.py test failed",
            Errors: [new ErrorReport("VerifyError", "readiness probe failed", "", "backend/views.py", 10)],
            WorkingFiles: context.Orchestrator.Files.ToList(),
            VerifyEvidence: FormatVerifyEvidence(evidence)));

        fragments.Should().Contain("readiness_verify=fail");
        fragments.Should().Contain("verify gate failed");
        fragments.Should().Contain("[fragment:verify_evidence:");
    }

    [Fact]
    public async Task FullPipelineRunner_VerifyStage_RunsBeforeOrchestratorCompleted_WhenTestsPassed()
    {
        var service = CreateVerifyService(orchestrationPassed: true);
        var context = CreateCalorieContext(testsPassed: true, markOrchestratorCompleted: false);
        context.Orchestrator.Status.Should().NotBe(GenerationStatus.Completed);

        var verifyStage = new VerifyStage(
            service,
            Options.Create(new AutonomousBenchmarkModeOptions()),
            PlatformUtilizationTestOptions.Production,
            Options.Create(new VerifySubagentOptions
            {
                Enabled = true,
                RequirePassInProduction = true,
                EvidenceRoot = _evidenceRoot
            }),
            NullLogger<VerifyStage>.Instance);

        var runner = new FullGenerationPipelineRunner(
            new IGenerationStage[] { verifyStage },
            NullLogger<FullGenerationPipelineRunner>.Instance);

        var outcome = await runner.RunStageAsync(context, "verify", CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        context.Items.Should().ContainKey("verify_stage_reached");
        context.Orchestrator.Status.Should().NotBe(GenerationStatus.Completed);
    }

    [Fact]
    public async Task Banking_FullVerifyStage_CompletesWithRecipeEvidence()
    {
        var service = CreateVerifyService(orchestrationPassed: true, recipeId: "banking");
        var context = CreateBankingContext(testsPassed: true);

        var stage = new VerifyStage(
            service,
            Options.Create(new AutonomousBenchmarkModeOptions()),
            PlatformUtilizationTestOptions.Production,
            Options.Create(new VerifySubagentOptions
            {
                Enabled = true,
                RequirePassInProduction = true,
                EvidenceRoot = _evidenceRoot
            }),
            NullLogger<VerifyStage>.Instance);

        var outcome = await stage.ExecuteAsync(context, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        context.Items["verify_recipe_id"].Should().Be("banking");
        context.Items["verify_passed"].Should().Be(true);
        context.Items["verify_evidence_path"].Should().NotBeNull();
    }

    [Fact]
    public async Task CalorieVision_FullVerifyStage_CompletesWithRecipeEvidence()
    {
        var service = CreateVerifyService(orchestrationPassed: true);
        var context = CreateCalorieContext(testsPassed: true);

        var stage = new VerifyStage(
            service,
            Options.Create(new AutonomousBenchmarkModeOptions()),
            PlatformUtilizationTestOptions.Production,
            Options.Create(new VerifySubagentOptions
            {
                Enabled = true,
                RequirePassInProduction = true,
                EvidenceRoot = _evidenceRoot
            }),
            NullLogger<VerifyStage>.Instance);

        var outcome = await stage.ExecuteAsync(context, CancellationToken.None);

        outcome.ShouldContinue.Should().BeTrue();
        context.Items["verify_recipe_id"].Should().Be("calorie-vision");
        context.Items["verify_passed"].Should().Be(true);
        context.Items["verify_evidence_path"].Should().NotBeNull();

        var bundle = _evidenceStore.List(context.Orchestrator.Id);
        bundle.Artifacts.Should().Contain(a => a.Kind == VerifyEvidenceKind.Manifest);
        bundle.Artifacts.Should().Contain(a => a.Kind == VerifyEvidenceKind.VerifyReport);
        bundle.Artifacts.Single(a => a.Kind == VerifyEvidenceKind.Manifest).DownloadUrl
            .Should().Contain($"/verify/artifacts/manifest.json");
    }

    private VerifySubagentService CreateVerifyService(bool orchestrationPassed, string recipeId = "calorie-vision")
    {
        var registry = new VerifyRecipeRegistry(
            new VerifyRecipeLlmDetector(
                new Mock<Libr4.AI.Application.Abstractions.IAIService>().Object,
                VerifyRecipeCatalog.BuildAll().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase),
                Options.Create(new VerifySubagentOptions { EnableRecipeLlmFallback = false }),
                NullLogger<VerifyRecipeLlmDetector>.Instance),
            Options.Create(new VerifySubagentOptions { EvidenceRoot = _evidenceRoot }),
            NullLogger<VerifyRecipeRegistry>.Instance);

        var orchestration = orchestrationPassed
            ? new VerifyOrchestrationResult(
                ShadowPassed: true,
                ReadinessPassed: true,
                AgentPassed: true,
                AgentSummary: "ok",
                ReadinessResults:
                [
                    new VerifyReadinessResult("backend", "http://localhost:8000/", true, [], TimeSpan.Zero),
                    new VerifyReadinessResult("frontend", "http://localhost:5173/", true, [], TimeSpan.Zero)
                ],
                Path.Combine(_evidenceRoot, "readiness.json"),
                null)
            : new VerifyOrchestrationResult(
                ShadowPassed: true,
                ReadinessPassed: false,
                AgentPassed: false,
                AgentSummary: "browser smoke failed",
                ReadinessResults:
                [
                    new VerifyReadinessResult(
                        "backend",
                        "http://localhost:8000/",
                        false,
                        [new VerifyReadinessAttempt("backend", "http://localhost:8000/", 3, 0, false, "timeout", TimeSpan.FromSeconds(1))],
                        TimeSpan.FromSeconds(1))
                ],
                Path.Combine(_evidenceRoot, "readiness.json"),
                Path.Combine(_evidenceRoot, "verify-failure-evidence.json"));

        var orchestrator = new Mock<IVerifyOrchestrator>();
        orchestrator.Setup(o => o.PrepareVerifyRun(
                It.IsAny<GenerationContext>(),
                It.IsAny<VerifyRecipeDetectionResult>(),
                It.IsAny<string>()))
            .Returns((GenerationContext ctx, VerifyRecipeDetectionResult detection, string dir) =>
            {
                var recipe = detection.Recipe.Id == recipeId
                    ? detection.Recipe
                    : VerifyRecipeCatalog.BuildAll().Single(r => r.Id == recipeId);
                return new VerifyRunPlan(
                    ctx.Orchestrator.Id,
                    recipe,
                    dir,
                    ctx.Orchestrator.ShadowWorkspaceId,
                    ctx.Plan!.RuntimeImage,
                    detection.ManifestPath,
                    true,
                    detection.DetectionMethod);
            });

        orchestrator.Setup(o => o.RunVerifyOrchestrationAsync(
                It.IsAny<GenerationContext>(),
                It.IsAny<VerifyRunPlan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestration);

        return new VerifySubagentService(
            registry,
            orchestrator.Object,
            new VerifyGateService(),
            _failureStore,
            _evidenceStore,
            Options.Create(new VerifySubagentOptions
            {
                Enabled = true,
                EnableAgentSubagent = false,
                EvidenceRoot = _evidenceRoot
            }),
            Options.Create(new AutonomousBenchmarkModeOptions()),
            PlatformUtilizationTestOptions.Production,
            NullLogger<VerifySubagentService>.Instance);
    }

    private static GenerationContext CreateBankingContext(bool testsPassed)
    {
        var orchestrator = AppGenerationOrchestrator.Create(
            "build banking spring boot react app",
            "fp-banking-verify-e2e");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "BankingPortal",
            applicationDescription: "Banking portal spring boot + react",
            techStack: new TechStack(["Java", "TypeScript"], ["Spring Boot", "React"], [], [], "spring+react"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "eclipse-temurin:21",
            buildCommands:
            [
                "cd backend && mvn -B -ntp -DskipTests package",
                "cd frontend && npm run build"
            ],
            testCommands:
            [
                "cd backend && mvn -B -ntp test",
                "cd frontend && npm test -- --watch=false"
            ],
            maxIterations: 3));

        foreach (var file in new[]
                 {
                     new GeneratedFile("backend/pom.xml", "xml", "<project><artifactId>banking</artifactId></project>"),
                     new GeneratedFile("frontend/package.json", "json", """{"dependencies":{"react":"^18.0.0","vite":"^5.0.0"}}""")
                 })
            orchestrator.UpsertFile(file);

        if (testsPassed)
            orchestrator.MarkCompleted();

        var context = new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = "build banking spring boot react app",
            Plan = orchestrator.Plan
        };
        if (testsPassed)
            context.Items["tests_passed"] = true;
        return context;
    }

    private static GenerationContext CreateCalorieContext(bool testsPassed, bool markOrchestratorCompleted = true)
    {
        var orchestrator = AppGenerationOrchestrator.Create(
            "build CalorieVision django solidjs app",
            "fp-calorie-verify-e2e");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "CalorieVision",
            applicationDescription: "Calorie tracker django + solidjs",
            techStack: new TechStack(["Python", "TypeScript"], ["Django", "SolidJS"], [], [], "django+solidjs"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12",
            buildCommands:
            [
                "cd backend && python manage.py check",
                "cd frontend && npm run build"
            ],
            testCommands:
            [
                "cd backend && python manage.py test",
                "cd frontend && npm test -- --watch=false"
            ],
            maxIterations: 3));

        foreach (var file in new[]
                 {
                     new GeneratedFile("backend/manage.py", "python", "#!/usr/bin/env python\ndjango setup"),
                     new GeneratedFile("backend/requirements.txt", "text", "Django>=5.0"),
                     new GeneratedFile("frontend/package.json", "json", """{"dependencies":{"solid-js":"^1.8.0","vite":"^5.0.0"}}""")
                 })
            orchestrator.UpsertFile(file);

        if (testsPassed && markOrchestratorCompleted)
            orchestrator.MarkCompleted();

        var context = new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = "build CalorieVision django solidjs app",
            Plan = orchestrator.Plan
        };
        if (testsPassed)
            context.Items["tests_passed"] = true;
        return context;
    }

    private static string FormatVerifyEvidence(VerifyFailureEvidence evidence)
    {
        var sb = new StringBuilder();
        sb.AppendLine(evidence.Summary);
        sb.AppendLine(evidence.ReportText);
        return sb.ToString().TrimEnd();
    }

    private static async Task<LoopbackHttpServer> StartLoopbackHttpServerAsync()
    {
        var port = GetFreeTcpPort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var cts = new CancellationTokenSource();
        var loop = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await listener.GetContextAsync().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    break;
                }

                if (ctx is null)
                    continue;

                ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                var payload = Encoding.UTF8.GetBytes("django-ready");
                ctx.Response.OutputStream.Write(payload, 0, payload.Length);
                ctx.Response.Close();
            }
        }, cts.Token);

        return new LoopbackHttpServer(listener, cts, loop, port);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class LoopbackHttpServer : IAsyncDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts;
        private readonly Task _loop;

        public LoopbackHttpServer(HttpListener listener, CancellationTokenSource cts, Task loop, int port)
        {
            _listener = listener;
            _cts = cts;
            _loop = loop;
            Port = port;
            BaseUrl = $"http://127.0.0.1:{port}/";
        }

        public int Port { get; }
        public string BaseUrl { get; }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            _listener.Close();
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch
            {
                // loop cancelled
            }

            _cts.Dispose();
        }
    }
}
