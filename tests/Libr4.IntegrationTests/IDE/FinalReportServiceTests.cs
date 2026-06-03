using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FinalReportServiceTests
{
    [Fact]
    public void GenerateFinalReport_ShouldIncludeTaskGraph()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);

        var report = service.GenerateFinalReport(orchestrator, "passed", new[] { "file1.cs", "file2.cs" });

        report.TaskGraph.Should().NotBeNull();
        report.TaskGraph.Should().NotBeEmpty();
        report.ApplicationName.Should().Be("TestApp");
    }

    [Fact]
    public void GenerateFinalReport_ShouldSynthesizeTaskGraph_WhenOrchestratorGraphIsEmpty()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        orchestrator.AttachPlan(BuildTestPlan());
        orchestrator.MarkCompleted();

        var report = service.GenerateFinalReport(orchestrator, "pass", Array.Empty<string>());
        var contract = service.GetReportContract("1.0");

        report.TaskGraph.Should().HaveCountGreaterThan(0);
        service.ValidateReportShape(report, contract).Should().BeTrue();
    }

    [Fact]
    public void GenerateFinalReport_ShouldIncludeTraceLinkage()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);

        var report = service.GenerateFinalReport(orchestrator, "passed", new[] { "file1.cs" });

        report.TraceLinkage.Should().NotBeEmpty();
        report.TraceLinkage.Should().Contain(t => t.LinkageType == "review_gate_verdict");
        report.TraceLinkage.Should().Contain(t => t.LinkageType == "task_graph");
    }

    [Fact]
    public void GenerateFinalReport_ShouldExtractExecutedSkills()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);

        var report = service.GenerateFinalReport(orchestrator, "passed", Array.Empty<string>());

        // Skills are extracted from skill invocations
        report.ExecutedSkills.Should().NotBeNull();
    }

    [Fact]
    public void GenerateFinalReport_ShouldExtractMemoryHits()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);

        // Simulate memory ingest
        var memoryEntry = new MemoryIngestAuditEntry(
            orchestrator.Id,
            "planning",
            MemoryKind.Semantic,
            "plan:TestApp",
            "Test plan summary",
            100,
            DateTime.UtcNow);
        orchestrator.RecordMemoryIngest(memoryEntry);

        var report = service.GenerateFinalReport(orchestrator, "passed", Array.Empty<string>());

        report.MemoryHits.Should().NotBeEmpty();
        report.MemoryHits.Should().Contain(h => h.Contains("plan:TestApp"));
    }

    [Fact]
    public void ValidateReportShape_ShouldPassValidReport()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);
        
        // Add task graph entry
        var taskEntry = new AgentTaskGraphEntry("t1", "Task 1", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null);
        orchestrator.ReplaceTaskGraph(new[] { taskEntry });

        var report = service.GenerateFinalReport(orchestrator, "passed", new[] { "file1.cs" });
        var contract = service.GetReportContract("1.0");

        var isValid = service.ValidateReportShape(report, contract);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateReportShape_ShouldFailMissingRunId()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());

        var report = new FinalGenerationReport(
            "",
            "TestApp",
            true,
            1,
            new[] { new AgentTaskGraphEntry("t1", "Task 1", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null) },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            "passed",
            new[] { new TraceLinkageReference("test", "test", "test") },
            Array.Empty<string>(),
            DateTime.UtcNow);

        var contract = service.GetReportContract("1.0");
        var isValid = service.ValidateReportShape(report, contract);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateReportShape_ShouldFailMissingTaskGraph()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());

        var report = new FinalGenerationReport(
            "run-1",
            "TestApp",
            true,
            1,
            Array.Empty<AgentTaskGraphEntry>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            "passed",
            new[] { new TraceLinkageReference("test", "test", "test") },
            Array.Empty<string>(),
            DateTime.UtcNow);

        var contract = service.GetReportContract("1.0");
        var isValid = service.ValidateReportShape(report, contract);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateReportShape_ShouldFailMissingTraceLinkage()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());

        var report = new FinalGenerationReport(
            "run-1",
            "TestApp",
            true,
            1,
            new[] { new AgentTaskGraphEntry("t1", "Task 1", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null) },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            "passed",
            Array.Empty<TraceLinkageReference>(),
            Array.Empty<string>(),
            DateTime.UtcNow);

        var contract = service.GetReportContract("1.0");
        var isValid = service.ValidateReportShape(report, contract);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetReportContract_ShouldReturnVersion1Contract()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());

        var contract = service.GetReportContract("1.0");

        contract.Version.Should().Be("1.0");
        contract.RequiredFields.Should().Contain("runId");
        contract.RequiredFields.Should().Contain("taskGraph");
        contract.RequiredFields.Should().Contain("traceLinkage");
        contract.MaxPayloadSizeBytes.Should().Be(5_000_000);
    }

    [Fact]
    public void SerializeReport_ShouldProduceValidJson()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);

        var report = service.GenerateFinalReport(orchestrator, "passed", new[] { "file1.cs" });
        var contract = service.GetReportContract("1.0");

        var json = service.SerializeReport(report, contract);

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"runId\"");
        json.Should().Contain("\"applicationName\"");
        json.Should().Contain("\"traceLinkage\"");
    }

    [Fact]
    public void SerializeReport_ShouldIncludeAllTraceLinkageTypes()
    {
        var service = new FinalReportService(NullLogger<FinalReportService>.Instance, new TaskGraphHydrationService());
        var orchestrator = AppGenerationOrchestrator.Create("TestApp", "fingerprint-1");
        var plan = BuildTestPlan();
        orchestrator.AttachPlan(plan);

        var report = service.GenerateFinalReport(orchestrator, "passed", new[] { "file1.cs" });
        var contract = service.GetReportContract("1.0");

        var json = service.SerializeReport(report, contract);

        json.Should().Contain("task_graph");
        json.Should().Contain("review_gate_verdict");
    }

    private static GenerationPlan BuildTestPlan()
    {
        return new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test application",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[]
            {
                new GenerationPhase(1, "planning", "Planning phase", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "generation", "Generation phase", Array.Empty<AgentAssignment>())
            },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 3);
    }
}
