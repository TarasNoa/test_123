using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.Spaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentSpaceTests : IDisposable
{
    private readonly string _root;
    private readonly bool _gitAvailable;

    public AgentSpaceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"agent-space-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _gitAvailable = IsGitAvailable();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public async Task CreateSpace_InitializesMainWorktreeAndSharedDir()
    {
        if (!_gitAvailable)
            return;

        var service = CreateService();

        var space = await service.CreateSpaceAsync(new CreateSpaceRequest(
            Name: "Calorie Space",
            RepositoryUrl: null,
            BaseBranch: "main",
            OwnerId: "user-1",
            McpProfile: null,
            UserRequest: "Build calorie tracker"));

        space.SharedMemoryScope.Should().Be($"project:{space.SpaceId:D}");
        Directory.Exists(Path.Combine(space.RootPath, "main")).Should().BeTrue();
        File.Exists(Path.Combine(space.RootPath, "shared", "LIBR4.md")).Should().BeTrue();
    }

    [Fact]
    public async Task SpawnTwoAgents_DifferentWorktrees_NoFileCollision()
    {
        if (!_gitAvailable)
            return;

        var service = CreateService();
        var space = await service.CreateSpaceAsync(new CreateSpaceRequest(
            "Parallel Space", null, "main", "user-1", null, "parallel agents"));

        var impl = await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "build api", null));
        var explorer = await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Explorer, "research", null));

        impl.WorktreePath.Should().NotBe(explorer.WorktreePath);
        Directory.Exists(impl.WorktreePath).Should().BeTrue();
        Directory.Exists(explorer.WorktreePath).Should().BeTrue();

        var implFile = Path.Combine(impl.WorktreePath, "impl-only.txt");
        var explorerFile = Path.Combine(explorer.WorktreePath, "explorer-only.txt");
        await File.WriteAllTextAsync(implFile, "impl");
        await File.WriteAllTextAsync(explorerFile, "explore");
        File.Exists(implFile).Should().BeTrue();
        File.Exists(Path.Combine(impl.WorktreePath, "explorer-only.txt")).Should().BeFalse();
        File.Exists(Path.Combine(explorer.WorktreePath, "impl-only.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task ContextBus_PublishesAndReadsEvents()
    {
        var bus = new FileSpaceContextBus(Options.Create(new AgentSpaceOptions { SpacesRoot = _root }));
        var spaceId = Guid.NewGuid();

        await bus.PublishAsync(spaceId, "plan", "Plan ready", "step 1: scaffold", "m1");
        var events = await bus.ReadRecentAsync(spaceId);

        events.Should().ContainSingle(e => e.Kind == "plan" && e.Title == "Plan ready");
        bus.BuildDMailAddress(spaceId, SpaceMemberRole.Explorer).Should().Contain("@space/");
        bus.BuildHermesScope(spaceId).Should().StartWith("project:");
    }

    [Fact]
    public async Task ContextFanout_WritesNdjsonToMemberRuns()
    {
        if (!_gitAvailable)
            return;

        var runsRoot = Path.Combine(_root, "runs");
        var options = CreateOptions();
        var store = CreateStore(options);
        var service = CreateService(store, options);
        var ndjson = new NdjsonEventWriter(Options.Create(new AgentRuntimeOptions { RunsRoot = runsRoot }));
        var fanout = new SpaceContextNdjsonFanout(store, ndjson, NullLogger<SpaceContextNdjsonFanout>.Instance);
        var bus = new FileSpaceContextBus(options, fanout);

        var space = await service.CreateSpaceAsync(new CreateSpaceRequest("Fanout", null, "main", "u1", null, "test"));
        var implRunId = Guid.NewGuid();
        var impl = await service.SpawnAgentAsync(
            space.SpaceId,
            new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "build", implRunId));

        await bus.PublishAsync(space.SpaceId, "space_context_ready", "Ready", "plan body", "explorer-1");

        var eventsPath = Path.Combine(runsRoot, implRunId.ToString("D"), "events.jsonl");
        File.Exists(eventsPath).Should().BeTrue();
        var lines = await File.ReadAllTextAsync(eventsPath);
        lines.Should().Contain("space_context_updated");
        lines.Should().Contain("space_context_ready");
    }

    [Fact]
    public async Task Orchestrator_RunsExplorerImplementerVerifierPipeline()
    {
        if (!_gitAvailable)
            return;

        var options = CreateOptions();
        var store = CreateStore(options);
        var service = CreateService(store, options);
        var orchestrator = CreateOrchestrator(service, store, options);

        var space = await service.CreateSpaceAsync(new CreateSpaceRequest("Pipeline", null, "main", "u1", null, "orchestrate"));
        var result = await orchestrator.RunParallelPipelineAsync(
            space.SpaceId,
            new SpaceOrchestrationRequest(
                ExplorerTask: "research api",
                ImplementerTask: "implement api",
                VerifierTask: "verify integration"));

        result.ContextReady.Should().BeTrue();
        result.Explorer.Role.Should().Be(SpaceMemberRole.Explorer);
        result.Implementer.Role.Should().Be(SpaceMemberRole.Implementer);
        result.Verifier.Should().NotBeNull();
        result.Verifier!.WorktreePath.Should().Contain("main");
        result.Stage.Should().BeOneOf("verifier_spawned", "implementer_merged", "merge_conflict");
        result.Timeline.Should().Contain(e => e.Kind == "space_context_ready");
    }

    [Fact]
    public async Task MergeConflict_SurfacesHumanReadableReport()
    {
        if (!_gitAvailable)
            return;

        var service = CreateService();
        var space = await service.CreateSpaceAsync(new CreateSpaceRequest("Conflict", null, "main", "u1", null, "conflict test"));

        var impl1 = await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "a", null));
        var impl2 = await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "b", null));

        await CommitReadmeAsync(impl1.WorktreePath, "version from agent 1");
        await CommitReadmeAsync(impl2.WorktreePath, "version from agent 2");

        var merge1 = await service.MergeMemberAsync(space.SpaceId, impl1.MemberId);
        merge1.Success.Should().BeTrue();

        var merge2 = await service.MergeMemberAsync(space.SpaceId, impl2.MemberId);
        merge2.Success.Should().BeFalse();
        merge2.Conflicts.Should().NotBeEmpty();
        merge2.Output.Should().Match("*conflict*", because: "human-readable merge conflict report");
    }

    [Fact]
    public async Task ContextReady_DMailHandoffToImplementer()
    {
        if (!_gitAvailable)
            return;

        var runsRoot = Path.Combine(_root, "runs");
        var options = CreateOptions();
        var store = CreateStore(options);
        var service = CreateService(store, options);
        var dmail = new FileDMailBus(Options.Create(new DMailOptions { RunsRoot = runsRoot }));
        var ndjson = new NdjsonEventWriter(Options.Create(new AgentRuntimeOptions { RunsRoot = runsRoot }));
        var fanout = new SpaceContextNdjsonFanout(store, ndjson, NullLogger<SpaceContextNdjsonFanout>.Instance, dmail);
        var bus = new FileSpaceContextBus(options, fanout);

        var space = await service.CreateSpaceAsync(new CreateSpaceRequest("DMail", null, "main", "u1", null, "handoff"));
        var implRunId = Guid.NewGuid();
        await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "build", implRunId));

        await bus.PublishAsync(space.SpaceId, "space_context_ready", "Context ready", "plan details", "explorer-1");

        var address = bus.BuildDMailAddress(space.SpaceId, SpaceMemberRole.Implementer);
        var messages = await dmail.ReadAsync(implRunId, to: address);
        messages.Should().ContainSingle(m => m.Payload.Contains("Context ready"));
    }

    [Fact]
    public async Task ListWorktreeFiles_ReturnsDirectoryEntries()
    {
        if (!_gitAvailable)
            return;

        var service = CreateService();
        var space = await service.CreateSpaceAsync(new CreateSpaceRequest("Files", null, "main", "u1", null, "list files"));
        var member = await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "build", null));

        await File.WriteAllTextAsync(Path.Combine(member.WorktreePath, "feature.txt"), "hello");

        var listing = await service.ListWorktreeFilesAsync(space.SpaceId, member.MemberId);
        listing.Should().NotBeNull();
        listing!.Entries.Should().Contain(e => e.Name == "feature.txt" && !e.IsDirectory);
        listing.Entries.Should().Contain(e => e.Name == "README.md");
    }

    [Fact]
    public async Task PreviewMerge_ReturnsDiffBeforeIntegrationMerge()
    {
        if (!_gitAvailable)
            return;

        var service = CreateService();
        var space = await service.CreateSpaceAsync(new CreateSpaceRequest("Preview", null, "main", "u1", null, "preview"));
        var member = await service.SpawnAgentAsync(space.SpaceId, new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, "build", null));

        await File.WriteAllTextAsync(Path.Combine(member.WorktreePath, "feature.txt"), "feature work");
        await RunGitAsync(member.WorktreePath, "add", "feature.txt");
        await RunGitAsync(member.WorktreePath, "-c", "user.email=space@libr4.local", "-c", "user.name=Libr4 Space", "commit", "-m", "add feature");

        var preview = await service.PreviewMergeAsync(space.SpaceId, member.MemberId);
        preview.Should().NotBeNull();
        preview!.SourceBranch.Should().Be(member.BranchName);
        preview.Files.Should().Contain(f => f.Path.Contains("feature.txt", StringComparison.OrdinalIgnoreCase));
        preview.UnifiedDiff.Should().Contain("feature work");
    }

    private AgentSpaceService CreateService(
        SqliteSpaceStore? store = null,
        IOptions<AgentSpaceOptions>? options = null,
        ISpaceContextBus? bus = null)
    {
        options ??= CreateOptions();
        store ??= CreateStore(options);
        bus ??= new FileSpaceContextBus(options);
        return new AgentSpaceService(
            store,
            new GitWorktreeService(NullLogger<GitWorktreeService>.Instance),
            bus,
            options,
            NullLogger<AgentSpaceService>.Instance);
    }

    private SpaceOrchestrator CreateOrchestrator(
        AgentSpaceService service,
        SqliteSpaceStore store,
        IOptions<AgentSpaceOptions> options)
    {
        var bus = new FileSpaceContextBus(options);
        return new SpaceOrchestrator(
            service,
            bus,
            new SpaceConcurrencyGate(options),
            options,
            NullLogger<SpaceOrchestrator>.Instance);
    }

    private IOptions<AgentSpaceOptions> CreateOptions() =>
        Options.Create(new AgentSpaceOptions
        {
            StoreDbPath = DbPath(),
            SpacesRoot = _root,
            MaxWorktreesPerSpace = 4,
            HardWorktreeCap = 8,
            MaxParallelLlmPerSpace = 2,
            OrchestratorContextReadySeconds = 5
        });

    private SqliteSpaceStore CreateStore(IOptions<AgentSpaceOptions> options)
    {
        var store = new SqliteSpaceStore(options, NullLogger<SqliteSpaceStore>.Instance);
        store.EnsureSchemaAsync().GetAwaiter().GetResult();
        return store;
    }

    private string DbPath() => Path.Combine(_root, "spaces.db");

    private static async Task CommitReadmeAsync(string worktreePath, string content)
    {
        var readme = Path.Combine(worktreePath, "README.md");
        await File.WriteAllTextAsync(readme, content);
        await RunGitAsync(worktreePath, "add", "README.md");
        await RunGitAsync(worktreePath, "-c", "user.email=space@libr4.local", "-c", "user.name=Libr4 Space", "commit", "-m", "agent change");
    }

    private static async Task RunGitAsync(string cwd, params string[] args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = System.Diagnostics.Process.Start(psi)
                            ?? throw new InvalidOperationException("git_not_available");
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git failed ({process.ExitCode}): {stderr}");
    }

    private static bool IsGitAvailable()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(5000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
