using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class TaskEvidenceLinkageServiceTests
{
    [Fact]
    public void LinkTaskToEvidence_ShouldCreateValidLink()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_generate",
            "Generate files",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            null);

        var changedFiles = new[] { "src/Program.cs", "src/Utils.cs" };
        var commands = new[] { "dotnet build", "dotnet test" };
        var gates = new[] { "generation", "consistency" };

        var link = service.LinkTaskToEvidence(task, changedFiles, commands, gates);

        link.TaskId.Should().Be("t_generate");
        link.ChangedFilePaths.Should().HaveCount(2);
        link.ExecutedCommands.Should().HaveCount(2);
        link.QualityGateReferences.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateManifest_ShouldShowCompleteEvidence()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_generate",
            "Generate files",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            null);

        var links = new[]
        {
            new TaskEvidenceLink(
                "t_generate",
                new[] { "src/Program.cs", "src/Utils.cs" },
                new[] { "dotnet build" },
                new[] { "generation", "consistency" },
                DateTime.UtcNow),
        };

        var manifest = service.GenerateManifest(task, links);

        manifest.TaskId.Should().Be("t_generate");
        manifest.FilesChanged.Should().Be(2);
        manifest.CommandsExecuted.Should().Be(1);
        manifest.GatesReferenced.Should().Be(2);
        manifest.HasCompleteEvidence.Should().BeTrue();
    }

    [Fact]
    public void GenerateManifest_ShouldDetectIncompleteEvidence()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_generate",
            "Generate files",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            null);

        var links = new[]
        {
            new TaskEvidenceLink(
                "t_generate",
                new[] { "src/Program.cs" },
                Array.Empty<string>(), // No commands executed
                new[] { "generation" },
                DateTime.UtcNow),
        };

        var manifest = service.GenerateManifest(task, links);

        manifest.HasCompleteEvidence.Should().BeFalse("should detect missing command execution");
    }

    [Fact]
    public void GenerateManifest_ShouldAllowRecoveryTaskWithoutFileChanges()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_recovery_abc123",
            "Recovery replan",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            "build");

        var links = new[]
        {
            new TaskEvidenceLink(
                "t_recovery_abc123",
                Array.Empty<string>(), // Recovery may not change files
                new[] { "dotnet build", "dotnet test" },
                new[] { "execution" },
                DateTime.UtcNow),
        };

        var manifest = service.GenerateManifest(task, links);

        manifest.HasCompleteEvidence.Should().BeTrue("recovery tasks don't need file changes");
    }

    [Fact]
    public void ValidateEvidenceLinkage_ShouldPassForDoneTaskWithCompleteEvidence()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_generate",
            "Generate files",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            null);

        var links = new[]
        {
            new TaskEvidenceLink(
                "t_generate",
                new[] { "src/Program.cs" },
                new[] { "dotnet build" },
                new[] { "generation" },
                DateTime.UtcNow),
        };

        var isValid = service.ValidateEvidenceLinkage(task, links);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateEvidenceLinkage_ShouldFailForDoneTaskWithoutEvidence()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_generate",
            "Generate files",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            null);

        var links = Array.Empty<TaskEvidenceLink>();

        var isValid = service.ValidateEvidenceLinkage(task, links);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateEvidenceLinkage_ShouldSkipPendingTasks()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_test_loop",
            "Execute tests",
            Array.Empty<string>(),
            AgentTaskState.Pending,
            Array.Empty<string>(),
            null);

        var links = Array.Empty<TaskEvidenceLink>();

        var isValid = service.ValidateEvidenceLinkage(task, links);

        isValid.Should().BeTrue("pending tasks don't need evidence yet");
    }

    [Fact]
    public void ValidateEvidenceLinkage_ShouldRequireEvidenceForFailedTasks()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_generate",
            "Generate files",
            Array.Empty<string>(),
            AgentTaskState.Failed,
            Array.Empty<string>(),
            null);

        var links = new[]
        {
            new TaskEvidenceLink(
                "t_generate",
                Array.Empty<string>(),
                new[] { "dotnet build" },
                new[] { "generation" },
                DateTime.UtcNow),
        };

        var isValid = service.ValidateEvidenceLinkage(task, links);

        isValid.Should().BeTrue("failed task has evidence of execution");
    }

    [Fact]
    public void GetLinksForGraph_ShouldReturnOnlyRelevantLinks()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var graph = new[]
        {
            new AgentTaskGraphEntry("t_plan", "Plan", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null),
            new AgentTaskGraphEntry("t_generate", "Generate", Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), null),
        };

        var allLinks = new[]
        {
            new TaskEvidenceLink("t_plan", new[] { "plan.md" }, new[] { "cmd1" }, new[] { "plan" }, DateTime.UtcNow),
            new TaskEvidenceLink("t_generate", new[] { "src/Program.cs" }, new[] { "cmd2" }, new[] { "generation" }, DateTime.UtcNow),
            new TaskEvidenceLink("t_other", new[] { "other.txt" }, new[] { "cmd3" }, new[] { "other" }, DateTime.UtcNow),
        };

        var result = service.GetLinksForGraph(graph, allLinks);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(l => l.TaskId == "t_plan");
        result.Should().ContainSingle(l => l.TaskId == "t_generate");
    }

    [Fact]
    public void GenerateManifest_ShouldAggregateMultipleLinks()
    {
        var service = new TaskEvidenceLinkageService(NullLogger<TaskEvidenceLinkageService>.Instance);

        var task = new AgentTaskGraphEntry(
            "t_fix",
            "Fix errors",
            Array.Empty<string>(),
            AgentTaskState.Done,
            Array.Empty<string>(),
            null);

        var links = new[]
        {
            new TaskEvidenceLink(
                "t_fix",
                new[] { "src/Program.cs", "src/Utils.cs" },
                new[] { "dotnet build" },
                new[] { "build" },
                DateTime.UtcNow),
            new TaskEvidenceLink(
                "t_fix",
                new[] { "src/Utils.cs", "src/Helper.cs" },
                new[] { "dotnet test" },
                new[] { "execution" },
                DateTime.UtcNow),
        };

        var manifest = service.GenerateManifest(task, links);

        manifest.TotalLinks.Should().Be(2);
        manifest.FilesChanged.Should().Be(3); // Program.cs, Utils.cs, Helper.cs (distinct)
        manifest.CommandsExecuted.Should().Be(2); // build, test
        manifest.GatesReferenced.Should().Be(2); // build, execution
    }
}
