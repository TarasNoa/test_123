using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AdaptiveReplannerServiceTests
{
    [Fact]
    public void DetectFailureSignatures_ShouldIdentifyRepeatedPatterns()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var gateHistory = new[]
        {
            new QualityGateResult("build", 5, false, new[] { "compilation_error", "missing_dependency" }),
            new QualityGateResult("build", 4, false, new[] { "compilation_error", "missing_dependency" }),
            new QualityGateResult("test", 6, false, new[] { "test_timeout" }),
            new QualityGateResult("build", 3, false, new[] { "compilation_error", "missing_dependency" }),
            new QualityGateResult("test", 5, false, new[] { "test_timeout" }),
            new QualityGateResult("test", 4, false, new[] { "test_timeout" }),
        };

        var signatures = service.DetectFailureSignatures(gateHistory);

        signatures.Should().HaveCount(2);
        signatures.Should().ContainSingle(s => s.Stage == "build" && s.OccurrenceCount == 3);
        signatures.Should().ContainSingle(s => s.Stage == "test" && s.OccurrenceCount == 3);
    }

    [Fact]
    public void DetectFailureSignatures_ShouldIgnoreSingleOccurrences()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var gateHistory = new[]
        {
            new QualityGateResult("build", 5, false, new[] { "unique_error_1" }),
            new QualityGateResult("test", 6, false, new[] { "unique_error_2" }),
        };

        var signatures = service.DetectFailureSignatures(gateHistory);

        signatures.Should().BeEmpty("single occurrences should not be detected as signatures");
    }

    [Fact]
    public void GenerateRecoveryTasks_ShouldCreateStageSpecificTasks()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var signatures = new[]
        {
            new FailureSignature("generation", new[] { "token_limit_exceeded", "model_error" }, 2, DateTime.UtcNow),
            new FailureSignature("consistency", new[] { "import_mismatch" }, 2, DateTime.UtcNow),
        };

        var currentGraph = new[]
        {
            new AgentTaskGraphEntry("t_generate", "Generate", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null),
        };

        var recommendations = service.GenerateRecoveryTasks(signatures, currentGraph);

        recommendations.Should().HaveCount(2);
        recommendations.Should().ContainSingle(r => r.Stage == "generation");
        recommendations.Should().ContainSingle(r => r.Stage == "consistency");
    }

    [Fact]
    public void GenerateRecoveryTasks_ShouldIncludeStageSpecificActions()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var signatures = new[]
        {
            new FailureSignature("generation", new[] { "token_limit" }, 2, DateTime.UtcNow),
        };

        var currentGraph = Array.Empty<AgentTaskGraphEntry>();

        var recommendations = service.GenerateRecoveryTasks(signatures, currentGraph);

        var genTask = recommendations.Single(r => r.Stage == "generation");
        genTask.RecommendedActions.Should().Contain("Reduce max tokens per phase");
        genTask.RecommendedActions.Should().Contain("Increase temperature for diversity");
    }

    [Fact]
    public void GenerateRecoveryTasks_ShouldRespectMaxRecoveryAttempts()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var signatures = new[]
        {
            new FailureSignature("build", new[] { "compilation_error" }, 5, DateTime.UtcNow),
        };

        // Simulate 3 previous recovery attempts
        var currentGraph = new[]
        {
            new AgentTaskGraphEntry("t_recovery_1", "Recovery", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), "build"),
            new AgentTaskGraphEntry("t_recovery_2", "Recovery", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), "build"),
            new AgentTaskGraphEntry("t_recovery_3", "Recovery", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), "build"),
        };

        var recommendations = service.GenerateRecoveryTasks(signatures, currentGraph);

        recommendations.Should().BeEmpty("should not generate recovery task when max attempts reached");
    }

    [Fact]
    public void WouldCreateLoop_ShouldDetectIdenticalRationales()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var task = new RecoveryTaskRecommendation(
            "t_recovery_new",
            "build",
            "Fix build errors",
            new[] { "action1" },
            "Build failed 2 times with patterns: compilation_error");

        var currentGraph = new[]
        {
            new AgentTaskGraphEntry(
                "t_recovery_old",
                "Recovery",
                Array.Empty<string>(),
                AgentTaskState.Done,
                Array.Empty<string>(),
                "Build failed 2 times with patterns: compilation_error"),
        };

        var wouldLoop = service.WouldCreateLoop(task, currentGraph);

        wouldLoop.Should().BeTrue("identical rationale should indicate a loop");
    }

    [Fact]
    public void WouldCreateLoop_ShouldAllowDifferentRationales()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var task = new RecoveryTaskRecommendation(
            "t_recovery_new",
            "build",
            "Fix build errors with stricter validation",
            new[] { "action1" },
            "Build failed 3 times with patterns: compilation_error, missing_dependency");

        var currentGraph = new[]
        {
            new AgentTaskGraphEntry(
                "t_recovery_old",
                "Recovery",
                Array.Empty<string>(),
                AgentTaskState.Done,
                Array.Empty<string>(),
                "Build failed 2 times with patterns: compilation_error"),
        };

        var wouldLoop = service.WouldCreateLoop(task, currentGraph);

        wouldLoop.Should().BeFalse("different rationale should not indicate a loop");
    }

    [Fact]
    public void GenerateRecoveryTasks_ShouldHandleMultipleStageFailures()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var signatures = new[]
        {
            new FailureSignature("plan", new[] { "circular_dependency" }, 2, DateTime.UtcNow),
            new FailureSignature("execution", new[] { "runtime_error" }, 2, DateTime.UtcNow),
            new FailureSignature("fix", new[] { "fix_incomplete" }, 2, DateTime.UtcNow),
        };

        var currentGraph = Array.Empty<AgentTaskGraphEntry>();

        var recommendations = service.GenerateRecoveryTasks(signatures, currentGraph);

        recommendations.Should().HaveCount(3);
        recommendations.Should().ContainSingle(r => r.Stage == "plan");
        recommendations.Should().ContainSingle(r => r.Stage == "execution");
        recommendations.Should().ContainSingle(r => r.Stage == "fix");
    }

    [Fact]
    public void GenerateRecoveryTasks_ShouldProvideRationaleForEachTask()
    {
        var service = new AdaptiveReplannerService(NullLogger<AdaptiveReplannerService>.Instance);

        var signatures = new[]
        {
            new FailureSignature("build", new[] { "error1", "error2" }, 3, DateTime.UtcNow),
        };

        var currentGraph = Array.Empty<AgentTaskGraphEntry>();

        var recommendations = service.GenerateRecoveryTasks(signatures, currentGraph);

        var task = recommendations.Single();
        task.Rationale.Should().Contain("failed 3 times");
        task.Rationale.Should().Contain("error1");
        task.Rationale.Should().Contain("error2");
    }
}
