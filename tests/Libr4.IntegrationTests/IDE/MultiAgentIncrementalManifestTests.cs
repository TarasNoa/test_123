using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class MultiAgentIncrementalManifestTests
{
    private static GenerationPlan JavaBankPlan() =>
        StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan(
                "MobileBankApp",
                "banking",
                StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "eclipse-temurin:21-jdk",
                Array.Empty<string>(),
                Array.Empty<string>(),
                6),
            "java react banking");

    private static AgentOrchestrationOptions ExpandedIncrementalOptions() => new()
    {
        UseIncrementalFileScopedGeneration = true,
        UseExpandedJavaReactManifest = true,
        IncrementalSeedMode = IncrementalSeedMode.MinimalSpine,
        RejectUnplannedGeneratedPaths = true,
        MaxFilesPerIncrementalTask = 4
    };

    [Fact]
    public void GroupEntries_DevOpsPhase_AlwaysSingleFile()
    {
        var entries = new[]
        {
            new PlannedFileEntry("README.md", AgentPhase.DevOps, "readme", "generic"),
            new PlannedFileEntry("docker-compose.yml", AgentPhase.DevOps, "compose", "generic"),
        };

        var batches = IncrementalFileBatchGrouper.GroupEntries(entries, 4, AgentPhase.DevOps);

        batches.Should().HaveCount(2);
        batches.Should().OnlyContain(b => b.Count == 1);
    }

    [Fact]
    public void GroupEntries_ConfigPaths_AlwaysSingleFile()
    {
        var entries = new[]
        {
            new PlannedFileEntry("backend/src/main/java/com/generated/banking/config/SecurityConfig.java", AgentPhase.Backend, "sec", "java-spring"),
            new PlannedFileEntry("backend/src/main/java/com/generated/banking/config/WebConfig.java", AgentPhase.Backend, "web", "java-spring"),
        };

        var batches = IncrementalFileBatchGrouper.GroupEntries(entries, 4, AgentPhase.Backend);

        batches.Should().HaveCount(2);
        batches.Should().OnlyContain(b => b.Count == 1);
    }

    [Fact]
    public void GroupEntries_BatchesSameFolder_UpToFour()
    {
        var entries = new[]
        {
            new PlannedFileEntry("backend/src/main/java/com/generated/banking/dto/A.java", AgentPhase.Backend, "a", "java-spring"),
            new PlannedFileEntry("backend/src/main/java/com/generated/banking/dto/B.java", AgentPhase.Backend, "b", "java-spring"),
            new PlannedFileEntry("backend/src/main/java/com/generated/banking/dto/C.java", AgentPhase.Backend, "c", "java-spring"),
            new PlannedFileEntry("backend/pom.xml", AgentPhase.Backend, "pom", "java-spring"),
        };

        var batches = IncrementalFileBatchGrouper.GroupEntries(entries, 4);

        batches.Should().HaveCount(2);
        batches.First(b => b.Any(e => e.Path.EndsWith("pom.xml"))).Should().ContainSingle();
        batches.First(b => b.Count > 1).Should().HaveCount(3);
    }

    [Fact]
    public void ExpandedManifest_HasDeterministicPathCount_NoDuplicates()
    {
        var plan = JavaBankPlan();
        var entries = JavaReactExpandedFileManifest.AllForPlan(plan);

        entries.Should().HaveCountGreaterThan(55);
        entries.Select(e => e.Path).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CreateRegistry_RejectsStrayPaths_AcceptsOnlyTarget()
    {
        var plan = JavaBankPlan();
        var registry = MultiAgentIncrementalManifest.CreateRegistry(plan, ExpandedIncrementalOptions())!;
        var parsed = new List<DomainGeneratedFile>
        {
            new("backend/src/main/java/com/generated/banking/service/AccountService.java", "java", "class S{}"),
            new("backend/src/main/java/com/generated/banking/STRAY.java", "java", "class X{}"),
            new("frontend/src/App.tsx", "typescript", "export {}")
        };

        var accepted = registry.AcceptOnlyPlanned(
            parsed,
            new[] { "backend/src/main/java/com/generated/banking/service/AccountService.java" });

        accepted.Should().ContainSingle();
        accepted[0].RelativePath.Should().EndWith("AccountService.java");
    }

    [Fact]
    public void CreateFileScopedTasks_ExpandedBackend_BatchesRelatedPaths()
    {
        var plan = JavaBankPlan();
        var options = ExpandedIncrementalOptions();
        options.MaxFilesPerIncrementalTask = 4;
        var registry = MultiAgentIncrementalManifest.CreateRegistry(plan, options)!;
        var entryCount = registry.EntriesForPhase(AgentPhase.Backend).Count;
        var tasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(AgentPhase.Backend, plan, options, registry);

        tasks.Should().HaveCountLessThan(entryCount);
        tasks.Should().OnlyContain(t => t.Subtasks.Count == 0);
        tasks.Should().OnlyContain(t => t.Context.ScopedOutputOnly);
        tasks.SelectMany(t => t.Context.TargetRelativePaths)
            .Should()
            .OnlyHaveUniqueItems()
            .And.HaveCount(entryCount);
        tasks.Should().OnlyContain(t =>
            t.Context.TargetRelativePaths.All(registry.IsAllowed));
        tasks.Should().Contain(t => t.Context.TargetRelativePaths.Length > 1);
    }

    [Fact]
    public void CreateFileScopedTasks_Backend_OneTaskPerFile_NoNestedSubagents()
    {
        var plan = JavaBankPlan();
        var options = new AgentOrchestrationOptions
        {
            UseIncrementalFileScopedGeneration = true,
            UseExpandedJavaReactManifest = false,
            MaxFilesPerIncrementalTask = 1,
            UseFeatureScopedGeneration = false
        };
        var tasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(AgentPhase.Backend, plan, options);

        tasks.Should().HaveCountGreaterThan(8);
        tasks.Should().OnlyContain(t => t.Subtasks.Count == 0);
        tasks.Should().OnlyContain(t => t.Context.ScopedOutputOnly);
        tasks.Should().OnlyContain(t => t.Context.TargetRelativePaths.Length == 1);
        tasks.Select(t => t.Context.TargetRelativePaths[0])
            .Should()
            .Contain("backend/src/main/java/com/generated/banking/service/AccountService.java");
    }

    [Fact]
    public void FilterParsedToTargets_OnlyAllowsDeclaredPaths()
    {
        var parsed = new List<DomainGeneratedFile>
        {
            new("backend/App.java", "java", "class App{}"),
            new("frontend/src/App.tsx", "typescript", "export {}")
        };

        var filtered = MultiAgentGenerationContext.FilterParsedToTargets(
            parsed,
            new[] { "backend/App.java" });

        filtered.Should().ContainSingle(f => f.RelativePath == "backend/App.java");
    }

    [Fact]
    public void PartitionByExistingWorkspace_SkipsTasksWhenSeedFileIsComplete()
    {
        var plan = JavaBankPlan();
        var options = new AgentOrchestrationOptions
        {
            SkipIncrementalTaskWhenTargetExists = true,
            UseExpandedJavaReactManifest = false
        };
        var tasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(AgentPhase.Backend, plan, options);
        var pomTask = tasks.First(t =>
            t.Context.TargetRelativePaths.Contains("backend/pom.xml", StringComparer.OrdinalIgnoreCase));

        var workspace = new List<DomainGeneratedFile>
        {
            new("backend/pom.xml", "xml", new string('x', 200))
        };

        var (toRun, skipped) = IncrementalFileTaskPlanner.PartitionByExistingWorkspace(
            new List<AgentTask> { pomTask },
            workspace,
            options,
            registry: null);

        skipped.Should().ContainSingle();
        toRun.Should().BeEmpty();
    }

    [Fact]
    public void PartitionByExistingWorkspace_ExpandedMode_SkipsOnlyMinimalSpineNotWholeBackend()
    {
        var plan = JavaBankPlan();
        var options = ExpandedIncrementalOptions();
        var registry = MultiAgentIncrementalManifest.CreateRegistry(plan, options)!;
        var tasks = MultiAgentIncrementalManifest.CreateFileScopedTasks(AgentPhase.Backend, plan, options, registry);
        var accountTask = tasks.First(t =>
            t.Context.TargetRelativePaths.Contains(
                "backend/src/main/java/com/generated/banking/service/AccountService.java",
                StringComparer.OrdinalIgnoreCase));

        var workspace = new List<DomainGeneratedFile>
        {
            new("backend/pom.xml", "xml", new string('x', 200)),
            new("backend/src/main/java/com/generated/banking/service/AccountService.java", "java", new string('x', 200))
        };

        var (toRun, skipped) = IncrementalFileTaskPlanner.PartitionByExistingWorkspace(
            new List<AgentTask> { accountTask },
            workspace,
            options,
            registry);

        skipped.Should().BeEmpty("AccountService is not minimal spine — must still run LLM");
        toRun.Should().ContainSingle();
    }

    [Fact]
    public void MergeWorkspaceAndPhaseBatches_PrefersWorkspaceForMinimum()
    {
        var plan = JavaBankPlan();
        var workspace = new List<DomainGeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project/>"),
            new("backend/src/main/java/com/generated/banking/BankingApplication.java", "java", "class Main{}"),
            new("frontend/package.json", "json", "{}"),
            new("frontend/src/main.tsx", "typescript", "import React"),
            new("frontend/src/App.tsx", "typescript", "export default function App(){}"),
            new("frontend/src/api/client.ts", "typescript", "export async function fetchAccounts(){}"),
            new("backend/src/test/java/com/generated/banking/BankingApiTests.java", "java", "class T{}"),
            new("backend/src/main/resources/application.yml", "yaml", "server:\n  port: 8080")
        };

        var batchOnly = new List<DomainGeneratedFile>
        {
            new("backend/src/main/java/com/generated/banking/service/AccountService.java", "java", "class S{}")
        };

        var merged = StackArtifactCompleteness.MergeWorkspaceAndPhaseBatches(workspace, batchOnly);
        StackArtifactCompleteness.MeetsPlanMinimum(plan, merged).Should().BeTrue();
        merged.Count.Should().BeGreaterThanOrEqualTo(8);
    }
}
