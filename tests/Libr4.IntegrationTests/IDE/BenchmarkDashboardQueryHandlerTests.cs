using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Handlers;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BenchmarkDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAggregatedDashboardForRecentRuns()
    {
        var repository = new InMemoryAppGenerationRepository();
        var qualityAssessment = new RunQualityAssessmentService();
        var handler = new GetBenchmarkDashboardQueryHandler(repository, qualityAssessment);

        var successRun = CreateRun("fingerprint-bench-1", true, "generation", 9, true, Array.Empty<string>(), 900);
        var failedRun = CreateRun("fingerprint-bench-2", false, "build:infra", 2, false, new[] { "build_failed" }, 1500);
        successRun.RecordQualityGate("build", 9, true, Array.Empty<string>());
        failedRun.RecordMcpExecution(new McpExecutionAuditEntry(
            ToolName: "n8n.workflow.test",
            ServerName: "n8n-lane",
            Lane: McpExecutionLaneKind.N8n,
            RiskLevel: McpToolRiskLevel.Medium,
            ArgumentsSha256: "hash2",
            StartedAtUtc: DateTime.UtcNow,
            DurationMs: 0,
            Outcome: "mcp_server_missing",
            Detail: "missing server"));
        await repository.SaveAsync(successRun);
        await repository.SaveAsync(failedRun);

        var result = await handler.Handle(new GetBenchmarkDashboardQuery(Limit: 10), CancellationToken.None);

        result.TotalRuns.Should().Be(2);
        result.SucceededRuns.Should().Be(1);
        result.FailedRuns.Should().Be(1);
        result.TopFailureReasons.Should().Contain("build_failed");
        result.TotalMcpDegradedEvents.Should().Be(1);
        result.TopMcpBlockerCodes.Should().Contain("mcp_server_missing");
        result.StageTrends.Should().Contain(s => s.Stage == "build" || s.Stage == "generation");
        result.TopRegressions.Should().Contain(r => r.Stage == "build" && r.Delta < 0);
        result.Runs.Should().HaveCount(2);
        result.Runs.Should().Contain(r => r.TotalCommandDurationMs == 1500);
    }

    private static AppGenerationOrchestrator CreateRun(
        string fingerprint,
        bool succeeded,
        string gateStage,
        int gateScore,
        bool gatePassed,
        IReadOnlyList<string> reasons,
        long commandDurationMs)
    {
        var orchestrator = AppGenerationOrchestrator.Create("Generate app", fingerprint);
        var plan = new GenerationPlan(
            "SampleApp",
            "Sample description",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "planning", "Plan", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "generation", "Generate", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "build", "Build", Array.Empty<AgentAssignment>())
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
            phase: gateStage,
            command: "test-cmd",
            exitCode: succeeded ? 0 : 1,
            duration: TimeSpan.FromMilliseconds(commandDurationMs),
            runtimeProvider: "process",
            runtimeSessionId: "bench-session",
            executedAtUtc: DateTime.UtcNow);
        var execution = new ExecutionResult(
            succeeded: succeeded,
            exitCode: succeeded ? 0 : 1,
            duration: TimeSpan.FromMilliseconds(commandDurationMs + 100),
            logs: new[] { new ConsoleLogEntry(DateTime.UtcNow, succeeded ? "stdout" : "stderr", "log") },
            commandExecutions: new[] { command });
        orchestrator.CompleteIteration(iteration.Id, execution);
        orchestrator.RecordQualityGate(gateStage, gateScore, gatePassed, reasons);
        if (succeeded) orchestrator.MarkCompleted();
        else orchestrator.MarkFailed("failed");
        return orchestrator;
    }
}
