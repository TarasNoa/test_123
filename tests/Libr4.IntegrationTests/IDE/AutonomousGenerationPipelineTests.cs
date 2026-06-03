using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Handlers;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutonomousGenerationPipelineTests
{
    [Fact]
    public async Task StartHandler_ShouldDeferToIterationLoop_WhenPhaseCompileCheckFails()
    {
        var planner = new FakePlannerService();
        var codeGen = new FakeCodeGenerationService();
        var shadow = new FailingBuildShadowExecutionService();
        var errorAnalysis = new FakeErrorAnalysisService();
        var repository = new InMemoryAppGenerationRepository();
        var runControl = new AutonomousRunControlService();
        var qualityGates = new AlwaysPassExceptBuildQualityGateService();
        var consistency = new AlwaysPassConsistencyValidator();

        var handler = new StartAppGenerationCommandHandler(
            planner,
            codeGen,
            shadow,
            errorAnalysis,
            repository,
            runControl,
            qualityGates,
            consistency,
            Options.Create(new AutonomousLoopGuardOptions()),
            Options.Create(new AutonomousRetryOptions()),
            CreateAgentIntegrationCoordinator(),
            NullLogger<StartAppGenerationCommandHandler>.Instance);

        var response = await handler.Handle(
            new StartAppGenerationCommand("Generate production app", MaxIterations: 3),
            CancellationToken.None);

        // P1-12 of audit: per-phase build failure under StrictPerPhase no longer hard-aborts.
        // The orchestrator breaks out of the per-phase loop and hands off to the iteration
        // fix loop. With a shadow that always fails the build, the iteration loop also
        // exhausts (or short-circuits via non_actionable_error or same-error circuit breaker)
        // and the run still ends as Failed — but the failure reason now reflects the
        // iteration-loop outcome, not the per-phase abort.
        response.Succeeded.Should().BeFalse();
        response.Status.Should().Be("Failed");
        response.FailureReason.Should().NotBeNullOrEmpty();
        // The run must NOT report success while the build is broken; that's the
        // original P0-6 contract that P1-12 preserves.
        response.FailureReason.Should().NotContain("quality_gate_build_failed: phase=", "P1-12 defers per-phase build failures to the iteration loop instead of failing fast on them");
    }

    [Fact]
    public async Task StartHandler_ShouldStopImmediately_WhenCancelledDuringGenerating()
    {
        var planner = new FakePlannerService();
        var slowCodeGen = new SlowCancellableCodeGenerationService();
        var shadow = new FakeShadowExecutionService();
        var errorAnalysis = new FakeErrorAnalysisService();
        var repository = new InMemoryAppGenerationRepository();
        var runControl = new AutonomousRunControlService();
        var qualityGates = new AlwaysPassQualityGateService();
        var consistency = new AlwaysPassConsistencyValidator();

        var handler = new StartAppGenerationCommandHandler(
            planner,
            slowCodeGen,
            shadow,
            errorAnalysis,
            repository,
            runControl,
            qualityGates,
            consistency,
            Options.Create(new AutonomousLoopGuardOptions()),
            Options.Create(new AutonomousRetryOptions()),
            CreateAgentIntegrationCoordinator(),
            NullLogger<StartAppGenerationCommandHandler>.Instance);

        var cts = new CancellationTokenSource();
        var runIdTask = handler.Handle(
            new StartAppGenerationCommand("Generate production app", MaxIterations: 3),
            cts.Token);

        // Cancel immediately during generating stage
        await Task.Delay(50); // Give it a moment to start
        var runId = (await repository.GetAllAsync(CancellationToken.None)).FirstOrDefault()?.Id ?? Guid.Empty;
        if (runId != Guid.Empty)
        {
            runControl.CancelRun(runId, "test-user", "manual_cancel");
        }
        cts.Cancel();

        var response = await runIdTask;

        response.Succeeded.Should().BeFalse();
        response.Status.Should().Be("Failed");
        response.FailureReason.Should().Contain("cancelled_by_request");
    }

    [Fact]
    public async Task StartHandler_ShouldSynthesizeTargetedFixHints_BeforeApplyFixes()
    {
        var planner = new PythonPlannerService();
        var codeGen = new CapturingFixCodeGenerationService();
        var shadow = new FailingPythonModuleShadowExecutionService();
        var errorAnalysis = new StaticErrorAnalysisService(new[]
        {
            new ErrorReport("MissingPackage", "No module named 'httpx'", suggestedFix: "", filePath: null)
        });
        var repository = new InMemoryAppGenerationRepository();
        var runControl = new AutonomousRunControlService();
        var qualityGates = new AlwaysPassQualityGateService();
        var consistency = new AlwaysPassConsistencyValidator();

        var handler = new StartAppGenerationCommandHandler(
            planner,
            codeGen,
            shadow,
            errorAnalysis,
            repository,
            runControl,
            qualityGates,
            consistency,
            Options.Create(new AutonomousLoopGuardOptions()),
            Options.Create(new AutonomousRetryOptions()),
            CreateAgentIntegrationCoordinator(),
            NullLogger<StartAppGenerationCommandHandler>.Instance);

        _ = await handler.Handle(
            new StartAppGenerationCommand("Generate FastAPI app", MaxIterations: 1),
            CancellationToken.None);

        codeGen.LastReceivedErrors.Should().NotBeNull();
        codeGen.LastReceivedErrors.Should().ContainSingle();
        var synthesized = codeGen.LastReceivedErrors![0];
        synthesized.FilePath.Should().Be("requirements.txt");
        synthesized.SuggestedFix.ToLowerInvariant().Should().Contain("requirements");
    }

    [Fact]
    public async Task StartHandler_ShouldSupportResumeSeed_WhenResumeFromRunIdProvided()
    {
        var planner = new FakePlannerService();
        var codeGen = new FakeCodeGenerationService();
        var shadow = new FakeShadowExecutionService();
        var errorAnalysis = new FakeErrorAnalysisService();
        var repository = new InMemoryAppGenerationRepository();
        var runControl = new AutonomousRunControlService();
        var qualityGates = new AlwaysPassQualityGateService();
        var consistency = new AlwaysPassConsistencyValidator();

        var seed = AppGenerationOrchestrator.Create("Seed request", "seed-fp");
        seed.AttachPlan(BuildPlan());
        seed.UpsertFile(new GeneratedFile("src/main.py", "python", "print('seed')"));
        await repository.SaveAsync(seed, CancellationToken.None);

        var handler = new StartAppGenerationCommandHandler(
            planner,
            codeGen,
            shadow,
            errorAnalysis,
            repository,
            runControl,
            qualityGates,
            consistency,
            Options.Create(new AutonomousLoopGuardOptions()),
            Options.Create(new AutonomousRetryOptions()),
            CreateAgentIntegrationCoordinator(),
            NullLogger<StartAppGenerationCommandHandler>.Instance);

        var response = await handler.Handle(
            new StartAppGenerationCommand(
                UserRequest: string.Empty,
                MaxIterations: 2,
                ResumeFromRunId: seed.Id),
            CancellationToken.None);

        response.Succeeded.Should().BeTrue();
        var resumed = await repository.GetAsync(response.Id, CancellationToken.None);
        resumed.Should().NotBeNull();
        resumed!.QualityGates.Select(q => q.Stage).Should().Contain("resume_seed_plan");
        resumed.QualityGates.Select(q => q.Stage).Should().Contain("resume_seed_files");
        resumed.Files.Select(f => f.RelativePath).Should().Contain("src/main.py");
    }

    [Fact]
    public async Task StartHandler_ShouldRecordIncrementalCommitCheckpoint_AfterFixChanges()
    {
        var planner = new PythonPlannerService();
        var codeGen = new CapturingFixCodeGenerationService();
        var shadow = new FailingPythonModuleShadowExecutionService();
        var errorAnalysis = new StaticErrorAnalysisService(new[]
        {
            new ErrorReport("MissingPackage", "No module named 'httpx'", suggestedFix: "", filePath: null)
        });
        var repository = new InMemoryAppGenerationRepository();
        var runControl = new AutonomousRunControlService();
        var qualityGates = new AlwaysPassQualityGateService();
        var consistency = new AlwaysPassConsistencyValidator();

        var handler = new StartAppGenerationCommandHandler(
            planner,
            codeGen,
            shadow,
            errorAnalysis,
            repository,
            runControl,
            qualityGates,
            consistency,
            Options.Create(new AutonomousLoopGuardOptions()),
            Options.Create(new AutonomousRetryOptions()),
            CreateAgentIntegrationCoordinator(),
            NullLogger<StartAppGenerationCommandHandler>.Instance);

        var response = await handler.Handle(
            new StartAppGenerationCommand("Generate FastAPI app", MaxIterations: 1),
            CancellationToken.None);

        var run = await repository.GetAsync(response.Id, CancellationToken.None);
        run.Should().NotBeNull();
        run!.Checkpoints.Should().Contain(c => c.Action == "incremental_commit");
    }

    [Fact]
    public async Task ApplyFixes_ShouldAllowDependencyAwareMultiFileAndFilterUnrelatedFiles()
    {
        var ai = new FakeAiService(
            """
            {
              "files": [
                {
                  "relativePath": "src/App/Services/IUserService.cs",
                  "content": "namespace App.Services; public interface IUserService { string Get(); }"
                },
                {
                  "relativePath": "src/App/Services/UserService.cs",
                  "content": "namespace App.Services; public sealed class UserService : IUserService { public string Get() => \"ok\"; }"
                },
                {
                  "relativePath": "src/App/Hack/Evil.cs",
                  "content": "namespace App.Hack; public static class Evil { }"
                }
              ]
            }
            """);

        var service = new LlmCodeGenerationService(
            ai,
            NullLogger<LlmCodeGenerationService>.Instance,
            Options.Create(new AutonomousGenerationOptions
            {
                InitialBatchSize = 2,
                MaxBatchAttempts = 1,
                LlmStepTimeoutSeconds = 30,
                MaxManifestFiles = 20
            }),
            new DefaultProviderCapabilityMatrix(
                NullLogger<DefaultProviderCapabilityMatrix>.Instance,
                Options.Create(new ProviderMatrixOptions())));

        var plan = BuildPlan();
        var files = new List<GeneratedFile>
        {
            new("src/App/Controllers/UserController.cs", "csharp", "using App.Services; namespace App.Controllers; public sealed class UserController { private readonly IUserService _service; public UserController(IUserService service) { _service = service; } }"),
            new("src/App/Services/IUserService.cs", "csharp", "namespace App.Services; public interface IUserService { string Get(); }"),
            new("src/App/Services/UserService.cs", "csharp", "namespace App.Services; public sealed class UserService : IUserService { public string Get() => \"old\"; }"),
            new("src/App/App.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />")
        };

        var errors = new List<ErrorReport>
        {
            new(
                errorType: "compile",
                message: "CS0246 The type or namespace name 'IUserService' could not be found while constructing UserService",
                suggestedFix: "Add missing interface and registration",
                filePath: "src/App/Controllers/UserController.cs",
                lineNumber: 1)
        };

        var patches = await service.ApplyFixesAsync(plan, files, errors, CancellationToken.None);

        // Interface file may be omitted when unchanged by dependency-aware filter.
        patches.Select(p => p.RelativePath).Should().Contain("src/App/Services/UserService.cs");
        patches.Select(p => p.RelativePath).Should().NotContain("src/App/Hack/Evil.cs");
    }

    [Fact]
    public void CascadePlanner_ShouldApplyLlmFanOutDependencies_WhenValidJsonReturned()
    {
        var plan = BuildPlan();
        var ai = new FakeAiService(
            """
            {
              "rationale": "LLM fan-out for independent contracts/services before tests",
              "phases": [
                { "phase_name": "contracts", "dependencies": [], "expected_output": "contracts", "instructions": { "focus": "contracts" } },
                { "phase_name": "services", "dependencies": ["contracts"], "expected_output": "services", "instructions": { "focus": "services" } },
                { "phase_name": "tests", "dependencies": ["contracts", "services"], "expected_output": "tests", "instructions": { "focus": "tests" } }
              ]
            }
            """);
        var planner = CreateCascadePlannerWithAi(ai);

        var cascade = planner.Build(plan, "Generate task API");

        var testPhase = cascade.Phases.First(p => p.PhaseName == "tests");
        testPhase.Dependencies.Should().HaveCount(2);
        testPhase.Dependencies.Should().Contain("phase_1_contracts");
        testPhase.Dependencies.Should().Contain("phase_2_services");
        cascade.PlannerMode.Should().Be("llm_assisted");
    }

    [Fact]
    public void CascadePlanner_ShouldExposeRoutingProfile_WhenModelRoutingConfigured()
    {
        var plan = BuildPlan();
        var ai = new FakeAiService(
            """
            {
              "rationale": "route test",
              "phases": [
                { "phase_name": "contracts", "dependencies": [] },
                { "phase_name": "services", "dependencies": ["contracts"] },
                { "phase_name": "tests", "dependencies": ["services"] }
              ]
            }
            """);
        var planner = CreateCascadePlannerWithAi(ai, new CascadePlannerOptions
        {
            ModelRoutingProfile = "api",
            ApiModel = "provider/model-x"
        });

        var cascade = planner.Build(plan, "Generate task API");

        cascade.RoutingProfile.Should().Be("api");
        cascade.ModelHint.Should().Be("provider/model-x");
    }

    [Fact]
    public void CascadePlanner_ShouldFallbackToDeterministic_WhenLlmJsonInvalid()
    {
        var plan = BuildPlan();
        var ai = new FakeAiService("not a valid json");
        var planner = CreateCascadePlannerWithAi(ai);

        var cascade = planner.Build(plan, "Generate task API");

        cascade.Rationale.Should().Contain("Cascade planning enabled");
        var testPhase = cascade.Phases.First(p => p.PhaseName == "tests");
        testPhase.Dependencies.Should().Contain("phase_2_services");
    }

    [Fact]
    public void CascadePlanner_DeterministicMode_ShouldKeepStablePhaseIds()
    {
        var plan = BuildPlan();
        var planner = new AutonomousCascadePlanner();

        var first = planner.Build(plan, "Generate task API");
        var second = planner.Build(plan, "Generate task API");

        first.Phases.Select(p => p.PhaseId).Should().Equal(second.Phases.Select(p => p.PhaseId));
    }

    [Fact]
    public void CascadePlanner_DeterministicMode_ShouldExposeRepoBootstrapInstructions_WhenRepoBootstrapRequested()
    {
        var plan = new GenerationPlan(
            "KanbanAuthApi",
            "[[REPO_BOOTSTRAP_REQUIRED]] Adapt GitHub repo with JWT auth and kanban board.",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "repo bootstrap"),
            new[]
            {
                new GenerationPhase(1, "Repo bootstrap & adaptation", "Adapt upstream repo", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "Scaffold", "Create project structure", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "Implement core", "Auth and kanban", Array.Empty<AgentAssignment>()),
                new GenerationPhase(4, "Tests", "Business tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "CodeGenerationAgent" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var planner = new AutonomousCascadePlanner();
        var cascade = planner.Build(plan, "Use Obscura on GitHub to find permissive-license repo and adapt with auth+kanban");

        cascade.Rationale.Should().Contain("Repo-bootstrap cascade");
        cascade.PlannerMode.Should().Be("deterministic");

        var bootstrapPhase = cascade.Phases.First(p => p.PhaseName.Contains("bootstrap", StringComparison.OrdinalIgnoreCase));
        bootstrapPhase.Instructions.Should().ContainKey("repo_bootstrap_mode");
        bootstrapPhase.Instructions["repo_bootstrap_mode"].Should().Be("required");
        bootstrapPhase.Instructions.Should().ContainKey("require_bootstrap_evidence");
        bootstrapPhase.ExpectedOutput.Should().Contain("BOOTSTRAP_EVIDENCE.md");

        var testPhase = cascade.Phases.First(p => p.PhaseName.Contains("Tests", StringComparison.OrdinalIgnoreCase));
        testPhase.Instructions["require_business_tests"].Should().Be("auth+kanban");
    }

    [Fact]
    public void StackPlanHeuristics_ShouldPreferAspNetCore_ForRepoBootstrapWithoutExplicitNodePython()
    {
        const string request =
            "Using obscura find GitHub repo with permissive license, add JWT auth and kanban board";

        StackPlanHeuristics.ShouldPreferAspNetCoreForRepoBootstrap(request).Should().BeTrue();

        var nodePlan = new GenerationPlan(
            "App",
            "[[REPO_BOOTSTRAP_REQUIRED]]",
            new TechStack(
                new[] { "JavaScript" },
                new[] { "Express" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "node"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "node:22-alpine",
            new[] { "npm ci" },
            new[] { "npm test" },
            4);

        var aligned = StackPlanHeuristics.AlignAspNetCoreRepoBootstrapPlan(nodePlan, request);
        aligned.TechStack.Languages.Should().Contain("C#");
        aligned.TechStack.Frameworks.Should().Contain("ASP.NET Core");
        aligned.RuntimeImage.Should().Contain("dotnet");
        aligned.BuildCommands.Should().Contain(c => c.Contains("dotnet build", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StackPlanHeuristics_ShouldRespectExplicitNodeRequest_ForRepoBootstrap()
    {
        const string request = "Build express node.js API with github repo bootstrap and kanban";
        StackPlanHeuristics.ShouldPreferAspNetCoreForRepoBootstrap(request).Should().BeFalse();
    }

    [Fact]
    public async Task AgentIntegration_OnGateFailure_ShouldAppendRecoveryTask()
    {
        var coordinator = CreateAgentIntegrationCoordinator();
        var orchestrator = AppGenerationOrchestrator.Create("Generate task API", Guid.NewGuid().ToString("N"));
        var plan = BuildPlan();
        orchestrator.AttachPlan(plan);

        await coordinator.OnPlanAttachedAsync(orchestrator, plan, CancellationToken.None);
        var before = orchestrator.TaskGraph.Count;

        await coordinator.OnGateFailureAsync(
            orchestrator,
            "build:tests",
            new[] { "build_failed", "test_non_zero_exit" },
            CancellationToken.None);

        orchestrator.TaskGraph.Count.Should().BeGreaterThan(before);
        orchestrator.TaskGraph.Any(t => t.TaskId.StartsWith("t_recovery_", StringComparison.Ordinal))
            .Should().BeTrue();
        orchestrator.TaskGraph.Last().Title.Should().Contain("Recovery replan");
    }

    [Fact]
    public async Task AgentIntegration_OnRepeatedGateFailures_ShouldAppendAdaptiveRecoveryTask()
    {
        var coordinator = CreateAgentIntegrationCoordinatorWithStageBServices();
        var orchestrator = AppGenerationOrchestrator.Create("Generate resilient API", Guid.NewGuid().ToString("N"));
        var plan = BuildPlan();
        orchestrator.AttachPlan(plan);
        await coordinator.OnPlanAttachedAsync(orchestrator, plan, CancellationToken.None);

        orchestrator.RecordQualityGate("generation", 4, false, new[] { "token_limit_exceeded" });
        orchestrator.RecordQualityGate("generation", 3, false, new[] { "token_limit_exceeded" });

        var before = orchestrator.TaskGraph.Count;
        await coordinator.OnGateFailureAsync(
            orchestrator,
            "generation",
            new[] { "token_limit_exceeded" },
            CancellationToken.None);

        orchestrator.TaskGraph.Count.Should().BeGreaterThan(before);
        orchestrator.TaskGraph.Should().Contain(t =>
            t.TaskId.StartsWith("t_recovery_generation_", StringComparison.Ordinal) &&
            t.Notes != null &&
            t.Notes.Contains("Generation failed 2 times", StringComparison.Ordinal));
    }

    private static GenerationPlan BuildPlan()
    {
        return new GenerationPlan(
            applicationName: "App",
            applicationDescription: "Test app",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[]
            {
                new GenerationPhase(1, "contracts", "contracts", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "services", "services", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "tests", Array.Empty<AgentAssignment>())
            },
            requiredAgents: new[] { "planner", "generator", "tester" },
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: new[] { "dotnet build" },
            testCommands: new[] { "dotnet test" },
            maxIterations: 3);
    }

    [Fact]
    public async Task McpStandalone_ShouldReturnTransportDisabled_WhenStdioOff()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions { EnableStdioTransport = false }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "list_task_locks",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        outcome.OutcomeCode.Should().Be("transport_disabled");
    }

    [Fact]
    public async Task McpStandalone_BrowserLane_ShouldReject_WhenKillSwitchOn()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = false,
                KillSwitchBrowserLane = true,
            }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "browser.smoke",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.OutcomeCode.Should().Be("lane_kill_switch");
    }

    [Fact]
    public async Task McpStandalone_N8nLane_ShouldReject_WhenKillSwitchOn()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = false,
                KillSwitchN8nLane = true,
            }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "n8n.workflow.test",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.OutcomeCode.Should().Be("lane_kill_switch");
    }

    [Fact]
    public async Task McpStandalone_BrowserLane_ShouldAccept_WhenKillSwitchOff()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = false,
                KillSwitchBrowserLane = false,
            }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "browser.smoke",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        outcome.OutcomeCode.Should().Be("transport_disabled");
    }

    [Fact]
    public async Task McpStandalone_N8nLane_ShouldAccept_WhenKillSwitchOff()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = false,
                KillSwitchN8nLane = false,
            }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "n8n.workflow.test",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        outcome.OutcomeCode.Should().Be("transport_disabled");
    }

    [Fact]
    public async Task McpStandalone_BrowserLane_ShouldUseDegradedMode_WhenServerUnavailable()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = false,
                KillSwitchBrowserLane = false,
                EnableDeterministicFallback = true,
            }),
            new FakeMcpServerPreflight(alwaysAvailable: false),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "browser.smoke",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.OutcomeCode.Should().Be("mcp_server_missing");
    }

    [Fact]
    public async Task McpStandalone_BrowserLane_ShouldHardFail_WhenServerUnavailableAndFallbackDisabled()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = false,
                KillSwitchBrowserLane = false,
                EnableDeterministicFallback = false,
            }),
            new FakeMcpServerPreflight(alwaysAvailable: false),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "browser.smoke",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.OutcomeCode.Should().Be("mcp_server_missing");
    }

    [Fact]
    public async Task McpMeta_ListAndDescribe_ShouldReturnDiscoveryPayload()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions { EnableStdioTransport = false }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var list = await mcp.InvokeStandaloneAsync(
            null,
            "mcp.list",
            new Dictionary<string, object?>(),
            CancellationToken.None);

        list.Succeeded.Should().BeTrue();
        list.ResultSummary.Should().Contain("mcp.search");

        var describe = await mcp.InvokeStandaloneAsync(
            null,
            "mcp.describe",
            new Dictionary<string, object?> { ["tool"] = "mcp.call" },
            CancellationToken.None);

        describe.Succeeded.Should().BeTrue();
        describe.ResultSummary.Should().Contain("mcp.call");
    }

    [Fact]
    public async Task McpMeta_Call_ShouldRejectNestedMetaCalls()
    {
        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions { EnableStdioTransport = false }),
            new FakeMcpServerPreflight(alwaysAvailable: true),
            NullLogger<McpToolInvocationService>.Instance);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "mcp.call",
            new Dictionary<string, object?> { ["tool"] = "mcp.list" },
            CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.OutcomeCode.Should().Be("bad_request");
    }

    private static IAgentIntegrationCoordinator CreateAgentIntegrationCoordinator()
    {
        var registry = new DefaultSkillRegistry();
        var memory = new InMemoryMemoryStore();
        var cascade = new AutonomousCascadePlanner();
        return new AgentIntegrationCoordinator(
            new AgentTaskGraphService(cascade),
            memory,
            cascade,
            new SkillRunner(registry, new StageBasedSkillSelectionStrategy(registry)),
            new ContextPackBuilder(memory, Options.Create(new ContextPackOptions())),
            new SecurityReviewGateService(Options.Create(new SecurityReviewGateOptions())),
            NullLogger<AgentIntegrationCoordinator>.Instance);
    }

    private static IAgentIntegrationCoordinator CreateAgentIntegrationCoordinatorWithStageBServices()
    {
        var registry = new DefaultSkillRegistry();
        var memory = new InMemoryMemoryStore();
        var cascade = new AutonomousCascadePlanner();
        return new AgentIntegrationCoordinator(
            new AgentTaskGraphService(cascade),
            memory,
            cascade,
            new SkillRunner(registry, new StageBasedSkillSelectionStrategy(registry)),
            new ContextPackBuilder(memory, Options.Create(new ContextPackOptions())),
            new SecurityReviewGateService(Options.Create(new SecurityReviewGateOptions())),
            new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance),
            new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance),
            NullLogger<AgentIntegrationCoordinator>.Instance);
    }

    private static AutonomousCascadePlanner CreateCascadePlannerWithAi(IAIService ai, CascadePlannerOptions? plannerOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => ai);
        var provider = services.BuildServiceProvider();
        return new AutonomousCascadePlanner(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AutonomousCascadePlanner>.Instance,
            Options.Create(plannerOptions ?? new CascadePlannerOptions()));
    }

    private sealed class InMemoryAppGenerationRepository : IAppGenerationRepository
    {
        private readonly List<AppGenerationOrchestrator> _orchestrators = new();

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
        {
            var existing = _orchestrators.FirstOrDefault(o => o.Id == orchestrator.Id);
            if (existing is not null)
            {
                _orchestrators.Remove(existing);
            }
            _orchestrators.Add(orchestrator);
            return Task.CompletedTask;
        }

        public Task<AppGenerationOrchestrator?> FindByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_orchestrators.FirstOrDefault(o => o.Id == id));

        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_orchestrators.FirstOrDefault(o => o.Id == id));

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(_orchestrators.ToList());

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string fingerprint, CancellationToken ct = default)
            => Task.FromResult(_orchestrators.Where(o => o.RequestFingerprint == fingerprint).OrderByDescending(o => o.StartedAt).FirstOrDefault());

        public Task<IReadOnlyList<AppGenerationOrchestrator>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(_orchestrators);

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default)
        {
            var result = tenantId is null
                ? _orchestrators.ToList()
                : _orchestrators.Where(o => string.Equals(o.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>(result);
        }
    }

    private sealed class FakePlannerService : IAppPlannerService
    {
        public Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default)
            => Task.FromResult(BuildPlan());
    }

    private sealed class FakeCodeGenerationService : ICodeGenerationService
    {
        public Task<IReadOnlyList<GeneratedFile>> GenerateInitialAsync(GenerationPlan plan, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GeneratedFile>>(new List<GeneratedFile>
            {
                new("src/App/Program.cs", "csharp", "var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.MapGet(\"/\", () => \"ok\"); app.Run();")
            });

        public Task<IReadOnlyList<GenerationPhaseBatchResult>> GenerateInitialByPhasesAsync(GenerationPlan plan, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GenerationPhaseBatchResult>>(new List<GenerationPhaseBatchResult>
            {
                new("contracts", new List<GeneratedFile>
                {
                    new("App.sln", "text", "solution"),
                    new("src/App/App.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />")
                }),
                new("services", new List<GeneratedFile>
                {
                    new("src/App/Program.cs", "csharp", "var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.MapGet(\"/\", () => \"ok\"); app.Run();")
                })
            });

        public Task<IReadOnlyList<GeneratedFile>> ApplyFixesAsync(GenerationPlan plan, IReadOnlyList<GeneratedFile> currentFiles, IReadOnlyList<ErrorReport> errors, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GeneratedFile>>(Array.Empty<GeneratedFile>());
    }

    private sealed class FailingBuildShadowExecutionService : IShadowExecutionService
    {
        private readonly Guid _workspaceId = Guid.NewGuid();

        public Task<Guid> PrepareWorkspaceAsync(IReadOnlyList<GeneratedFile> files, string runtimeImage, CancellationToken ct = default)
            => Task.FromResult(_workspaceId);

        public Task UpdateWorkspaceAsync(Guid workspaceId, IReadOnlyList<GeneratedFile> files, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<ExecutionResult> RunAsync(Guid workspaceId, GenerationPlan plan, CancellationToken ct = default)
        {
            var buildRecord = new CommandExecutionRecord(
                phase: "build",
                command: "dotnet build",
                exitCode: 1,
                duration: TimeSpan.FromMilliseconds(300),
                runtimeProvider: "docker",
                runtimeSessionId: "test",
                executedAtUtc: DateTime.UtcNow);

            return Task.FromResult(new ExecutionResult(
                succeeded: false,
                exitCode: 1,
                duration: TimeSpan.FromMilliseconds(300),
                logs: new List<ConsoleLogEntry> { new(DateTime.UtcNow, "stderr", "build failed") },
                commandExecutions: new List<CommandExecutionRecord> { buildRecord }));
        }

        public Task DisposeWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeErrorAnalysisService : IErrorAnalysisService
    {
        public Task<IReadOnlyList<ErrorReport>> AnalyzeAsync(ExecutionResult execution, IReadOnlyList<GeneratedFile> files, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ErrorReport>>(Array.Empty<ErrorReport>());
    }

    private sealed class StaticErrorAnalysisService : IErrorAnalysisService
    {
        private readonly IReadOnlyList<ErrorReport> _errors;

        public StaticErrorAnalysisService(IReadOnlyList<ErrorReport> errors)
        {
            _errors = errors;
        }

        public Task<IReadOnlyList<ErrorReport>> AnalyzeAsync(ExecutionResult execution, IReadOnlyList<GeneratedFile> files, CancellationToken ct = default)
            => Task.FromResult(_errors);
    }

    private sealed class AlwaysPassConsistencyValidator : IAutonomousCodeConsistencyValidator
    {
        public QualityGateResult Validate(IReadOnlyList<GeneratedFile> files, GenerationPlan plan)
            => new("consistency", 10, true, Array.Empty<string>());
    }

    private sealed class AlwaysPassExceptBuildQualityGateService : IAutonomousQualityGateService
    {
        public QualityGateResult EvaluatePlan(GenerationPlan plan) => new("plan", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateBuild(ExecutionResult execution) => new("build", 2, false, new[] { "build_failed" });
        public QualityGateResult EvaluateGeneratedFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan plan) => new("generation", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateExecution(ExecutionResult execution, GenerationPlan plan) => new("execution", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateFixProgress(IReadOnlyList<ErrorReport> errors, IReadOnlyList<GeneratedFile> patches) => new("fix", 10, true, Array.Empty<string>());
    }

    private sealed class AlwaysPassQualityGateService : IAutonomousQualityGateService
    {
        public QualityGateResult EvaluatePlan(GenerationPlan plan) => new("plan", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateBuild(ExecutionResult execution) => new("build", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateGeneratedFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan plan) => new("generation", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateExecution(ExecutionResult execution, GenerationPlan plan) => new("execution", 10, true, Array.Empty<string>());
        public QualityGateResult EvaluateFixProgress(IReadOnlyList<ErrorReport> errors, IReadOnlyList<GeneratedFile> patches) => new("fix", 10, true, Array.Empty<string>());
    }

    private sealed class SlowCancellableCodeGenerationService : ICodeGenerationService
    {
        public async Task<IReadOnlyList<GeneratedFile>> GenerateInitialAsync(GenerationPlan plan, CancellationToken ct = default)
        {
            await Task.Delay(2000, ct); // Slow enough to allow cancellation
            ct.ThrowIfCancellationRequested();
            return new List<GeneratedFile>
            {
                new("src/App/Program.cs", "csharp", "var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.MapGet(\"/\", () => \"ok\"); app.Run();")
            };
        }

        public async Task<IReadOnlyList<GenerationPhaseBatchResult>> GenerateInitialByPhasesAsync(GenerationPlan plan, CancellationToken ct = default)
        {
            await Task.Delay(2000, ct);
            ct.ThrowIfCancellationRequested();
            return new List<GenerationPhaseBatchResult>
            {
                new("contracts", new List<GeneratedFile>
                {
                    new("App.sln", "text", "solution"),
                    new("src/App/App.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />")
                })
            };
        }

        public Task<IReadOnlyList<GeneratedFile>> ApplyFixesAsync(GenerationPlan plan, IReadOnlyList<GeneratedFile> currentFiles, IReadOnlyList<ErrorReport> errors, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GeneratedFile>>(Array.Empty<GeneratedFile>());
    }

    private sealed class PythonPlannerService : IAppPlannerService
    {
        public Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default)
        {
            return Task.FromResult(new GenerationPlan(
                applicationName: "PyApp",
                applicationDescription: "FastAPI app",
                techStack: new TechStack(
                    languages: new[] { "Python" },
                    frameworks: new[] { "FastAPI" },
                    databases: Array.Empty<string>(),
                    infrastructure: Array.Empty<string>(),
                    rationale: "test"),
                phases: new[]
                {
                    new GenerationPhase(1, "api", "api", Array.Empty<AgentAssignment>())
                },
                requiredAgents: new[] { "planner", "generator" },
                runtimeImage: "python:3.11",
                buildCommands: new[] { "pytest -q" },
                testCommands: new[] { "pytest -q" },
                maxIterations: 1));
        }
    }

    private sealed class CapturingFixCodeGenerationService : ICodeGenerationService
    {
        public IReadOnlyList<ErrorReport>? LastReceivedErrors { get; private set; }

        public Task<IReadOnlyList<GeneratedFile>> GenerateInitialAsync(GenerationPlan plan, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GeneratedFile>>(new List<GeneratedFile>
            {
                new("main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
                new("requirements.txt", "text", "fastapi==0.110.0\n")
            });

        public Task<IReadOnlyList<GenerationPhaseBatchResult>> GenerateInitialByPhasesAsync(GenerationPlan plan, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GenerationPhaseBatchResult>>(new List<GenerationPhaseBatchResult>
            {
                new("api", new List<GeneratedFile>
                {
                    new("main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
                    new("requirements.txt", "text", "fastapi==0.110.0\n")
                })
            });

        public Task<IReadOnlyList<GeneratedFile>> ApplyFixesAsync(GenerationPlan plan, IReadOnlyList<GeneratedFile> currentFiles, IReadOnlyList<ErrorReport> errors, CancellationToken ct = default)
        {
            LastReceivedErrors = errors;
            return Task.FromResult<IReadOnlyList<GeneratedFile>>(Array.Empty<GeneratedFile>());
        }
    }

    private sealed class FailingPythonModuleShadowExecutionService : IShadowExecutionService
    {
        private readonly Guid _workspaceId = Guid.NewGuid();

        public Task<Guid> PrepareWorkspaceAsync(IReadOnlyList<GeneratedFile> files, string runtimeImage, CancellationToken ct = default)
            => Task.FromResult(_workspaceId);

        public Task UpdateWorkspaceAsync(Guid workspaceId, IReadOnlyList<GeneratedFile> files, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<ExecutionResult> RunAsync(Guid workspaceId, GenerationPlan plan, CancellationToken ct = default)
        {
            var logs = new List<ConsoleLogEntry>
            {
                new(DateTime.UtcNow, "stderr", "ModuleNotFoundError: No module named 'httpx'")
            };
            return Task.FromResult(new ExecutionResult(false, 1, TimeSpan.FromMilliseconds(100), logs));
        }

        public Task DisposeWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeShadowExecutionService : IShadowExecutionService
    {
        private readonly Guid _workspaceId = Guid.NewGuid();

        public Task<Guid> PrepareWorkspaceAsync(IReadOnlyList<GeneratedFile> files, string runtimeImage, CancellationToken ct = default)
            => Task.FromResult(_workspaceId);

        public Task UpdateWorkspaceAsync(Guid workspaceId, IReadOnlyList<GeneratedFile> files, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<ExecutionResult> RunAsync(Guid workspaceId, GenerationPlan plan, CancellationToken ct = default)
        {
            var buildRecord = new CommandExecutionRecord(
                phase: "build",
                command: "dotnet build",
                exitCode: 0,
                duration: TimeSpan.FromMilliseconds(300),
                runtimeProvider: "docker",
                runtimeSessionId: "test",
                executedAtUtc: DateTime.UtcNow);

            return Task.FromResult(new ExecutionResult(
                succeeded: true,
                exitCode: 0,
                duration: TimeSpan.FromMilliseconds(300),
                logs: new List<ConsoleLogEntry> { new(DateTime.UtcNow, "stdout", "build succeeded") },
                commandExecutions: new List<CommandExecutionRecord> { buildRecord }));
        }

        public Task DisposeWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAiService : IAIService
    {
        private readonly string _completion;

        public FakeAiService(string completion)
        {
            _completion = completion;
        }

        public Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
            => Task.FromResult(_completion);

        public Task<string> GenerateEmbeddingAsync(string text, string? model = null)
            => Task.FromResult("embedding");

        public Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
            => Task.FromResult("analysis");

        public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
            => Task.FromResult("chat");
    }

    [Fact]
    public async Task MemoryStore_PruneAsync_ShouldPreferHighSignalFixMemoryUnderTightBudget()
    {
        var store = new InMemoryMemoryStore();
        var fingerprint = "fp-prune-priority";
        var now = DateTime.UtcNow;

        await store.IngestAsync(
            new MemoryRecord(Guid.NewGuid(), fingerprint, "planning", MemoryKind.Episodic, "note-1", "generic planning note", null, 45, now),
            CancellationToken.None);
        await store.IngestAsync(
            new MemoryRecord(Guid.NewGuid(), fingerprint, "fixing", MemoryKind.Procedural, "fix-1", "critical fix evidence", "{\"fix\":true}", 45, now.AddMinutes(-20)),
            CancellationToken.None);

        await store.PruneAsync(fingerprint, maxTokenBudget: 50, CancellationToken.None);
        var results = await store.RetrieveAsync(new MemoryQuery(fingerprint, null, TopK: 10), CancellationToken.None);

        results.Should().ContainSingle();
        results.Single().Record.Kind.Should().Be(MemoryKind.Procedural);
        results.Single().Record.Stage.Should().Be("fixing");
    }

    [Fact]
    public async Task MemoryStore_PruneAsync_ShouldKeepMultipleHighValueRecordsWithinBudget()
    {
        var store = new InMemoryMemoryStore();
        var fingerprint = "fp-prune-multi";
        var now = DateTime.UtcNow;

        await store.IngestAsync(
            new MemoryRecord(Guid.NewGuid(), fingerprint, "planning", MemoryKind.Episodic, "note-1", "low priority note", null, 60, now),
            CancellationToken.None);
        await store.IngestAsync(
            new MemoryRecord(Guid.NewGuid(), fingerprint, "build", MemoryKind.Semantic, "err-1", "build failure signature", "{\"error\":true}", 25, now.AddMinutes(-15)),
            CancellationToken.None);
        await store.IngestAsync(
            new MemoryRecord(Guid.NewGuid(), fingerprint, "fixing", MemoryKind.Procedural, "fix-1", "successful remediation", "{\"fix\":true}", 25, now.AddMinutes(-30)),
            CancellationToken.None);

        await store.PruneAsync(fingerprint, maxTokenBudget: 60, CancellationToken.None);
        var results = await store.RetrieveAsync(new MemoryQuery(fingerprint, null, TopK: 10), CancellationToken.None);

        results.Select(r => r.Record.Kind).Should().Contain(new[] { MemoryKind.Semantic, MemoryKind.Procedural });
        results.Select(r => r.Record.Kind).Should().NotContain(MemoryKind.Episodic);
    }

    [Fact]
    public void SkillSchemaValidator_ShouldValidateValidSkill()
    {
        var validator = new SkillSchemaValidator();
        var skill = new SkillDefinition(
            "libr4.test.skill",
            "1.0.0",
            "Test skill",
            new[] { "test" },
            "trusted",
            new[] { "planning" },
            new SkillModelConfig(Temperature: 0.5, MaxTokens: 2048),
            new SkillRunConfig(TimeoutSeconds: 300, MaxRetries: 2),
            new[] { "file-read", "file-write" });

        var result = validator.Validate(skill);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SkillSchemaValidator_ShouldRejectInvalidVersion()
    {
        var validator = new SkillSchemaValidator();
        var skill = new SkillDefinition(
            "libr4.test.skill",
            "invalid",
            "Test skill",
            new[] { "test" },
            "trusted",
            new[] { "planning" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            Array.Empty<string>());

        var result = validator.Validate(skill);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("version"));
    }

    [Fact]
    public void SkillSchemaValidator_ShouldRejectInvalidSafetyLabel()
    {
        var validator = new SkillSchemaValidator();
        var skill = new SkillDefinition(
            "libr4.test.skill",
            "1.0.0",
            "Test skill",
            new[] { "test" },
            "invalid-label",
            new[] { "planning" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            Array.Empty<string>());

        var result = validator.Validate(skill);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("safety label"));
    }

    [Fact]
    public void SkillSchemaValidator_ShouldRejectBlockedSkill()
    {
        var validator = new SkillSchemaValidator();
        var skill = new SkillDefinition(
            "libr4.test.skill",
            "1.0.0",
            "Test skill",
            new[] { "test" },
            "blocked",
            new[] { "planning" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            Array.Empty<string>());

        var result = validator.Validate(skill);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("blocked"));
    }

    [Fact]
    public void SkillSchemaValidator_ShouldRejectInvalidTemperature()
    {
        var validator = new SkillSchemaValidator();
        var skill = new SkillDefinition(
            "libr4.test.skill",
            "1.0.0",
            "Test skill",
            new[] { "test" },
            "trusted",
            new[] { "planning" },
            new SkillModelConfig(Temperature: 3.0), // Invalid: > 2
            new SkillRunConfig(),
            Array.Empty<string>());

        var result = validator.Validate(skill);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("temperature", StringComparison.OrdinalIgnoreCase));
    }
}
