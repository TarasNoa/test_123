using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DiagnosticsBundleServiceTests
{
    [Fact]
    public async Task GenerateBundleAsync_ShouldIncludeBenchmarkSummaryFromManifest()
    {
        var repository = new InMemoryAppGenerationRepository();
        var qualityAssessment = new RunQualityAssessmentService();
        var mcpOptions = Options.Create(new McpExecutionOptions());
        var watchdog = new DefaultMcpLaneWatchdog(
            new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance),
            new DefaultMcpToolRegistry(),
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);
        var manifestBuilder = new ExecutionManifestBuilder(qualityAssessment, watchdog);
        var service = new DiagnosticsBundleService(
            repository,
            manifestBuilder,
            watchdog,
            NullLogger<DiagnosticsBundleService>.Instance);

        var orchestrator = AppGenerationOrchestrator.Create(
            "Generate task management API",
            "fingerprint-diagnostics-1");

        var plan = new GenerationPlan(
            "TaskApi",
            "Task API with benchmark trace",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "planning", "Create plan", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "generation", "Generate files", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "build", "Build and test", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "python -m pip install -r requirements.txt" },
            new[] { "pytest -q" },
            2);

        orchestrator.AttachPlan(plan);
        orchestrator.BeginGeneration();

        var iteration = orchestrator.BeginIteration();
        var command = new CommandExecutionRecord(
            phase: "build",
            command: "pytest -q",
            exitCode: 0,
            duration: TimeSpan.FromMilliseconds(1200),
            runtimeProvider: "process",
            runtimeSessionId: "test-session",
            executedAtUtc: DateTime.UtcNow);
        var execution = new ExecutionResult(
            succeeded: true,
            exitCode: 0,
            duration: TimeSpan.FromMilliseconds(1500),
            logs: new[] { new ConsoleLogEntry(DateTime.UtcNow, "stdout", "ok") },
            commandExecutions: new[] { command });
        orchestrator.CompleteIteration(iteration.Id, execution);
        orchestrator.RecordQualityGate("generation", 9, true, Array.Empty<string>());
        orchestrator.RecordQualityGate("build", 8, false, new[] { "build_failed" });
        orchestrator.RecordMcpExecution(new McpExecutionAuditEntry(
            ToolName: "browser.smoke",
            ServerName: "browser-lane",
            Lane: McpExecutionLaneKind.Browser,
            RiskLevel: McpToolRiskLevel.Medium,
            ArgumentsSha256: "hash",
            StartedAtUtc: DateTime.UtcNow,
            DurationMs: 0,
            Outcome: "mcp_server_missing",
            Detail: "MCP server executable not found"));
        orchestrator.MarkCompleted();

        await repository.SaveAsync(orchestrator);

        var bundle = await service.GenerateBundleAsync(orchestrator.Id);

        bundle.Should().NotBeNull();
        bundle!.Manifest.BenchmarkSummary.TotalQualityEvaluations.Should().Be(2);
        bundle.Manifest.BenchmarkSummary.TotalFailedEvaluations.Should().Be(1);
        bundle.Manifest.BenchmarkSummary.TotalCommandDurationMs.Should().Be(1200);
        bundle.Manifest.BenchmarkSummary.TopFailureReasons.Should().Contain("build_failed");
        bundle.Manifest.McpLaneDiagnostics.Should().ContainSingle();
        bundle.Manifest.McpLaneDiagnostics[0].Lane.Should().Be("Browser");
        bundle.Manifest.McpLaneDiagnostics[0].DegradedEvents.Should().Be(1);
        bundle.Manifest.McpLaneDiagnostics[0].TopBlockerCodes.Should().Contain("mcp_server_missing");
    }
}
