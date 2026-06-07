using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentSpecEvolutionTests : IDisposable
{
    private readonly string _root;
    private readonly AgentSpecEvolutionService _service;
    private readonly SqliteAgentSpecProposalStore _proposalStore;
    private readonly FileAgentSpecVersionStore _versionStore;
    private readonly StubAgentSpecRegistry _registry;

    public AgentSpecEvolutionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"klip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);

        var bundled = Path.Combine(_root, "bundled");
        Directory.CreateDirectory(bundled);
        File.WriteAllText(Path.Combine(bundled, "verify.agent.yaml"), """
            name: verify
            maxTurns: 12
            toolset:
              - read_file
            instruction: |
              baseline verify spec
            """);

        var options = Options.Create(new AgentSpecEvolutionOptions
        {
            Enabled = true,
            AutoAnalyzeFailedRuns = true,
            ProposalsDbPath = Path.Combine(_root, "proposals.db"),
            VersionsRoot = Path.Combine(_root, "versions"),
            EvolvedSpecsRoot = Path.Combine(_root, "evolved"),
            BundledSpecsDirectory = bundled
        });

        _proposalStore = new SqliteAgentSpecProposalStore(options);
        _versionStore = new FileAgentSpecVersionStore(options);
        _registry = new StubAgentSpecRegistry(new AgentSpec
        {
            Name = "verify",
            MaxTurns = 12,
            Toolset = ["read_file"],
            Instruction = "baseline verify spec"
        });

        _service = new AgentSpecEvolutionService(
            options,
            _proposalStore,
            _versionStore,
            _registry,
            NullLogger<AgentSpecEvolutionService>.Instance);
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
            // best effort
        }
    }

    [Fact]
    public async Task AnalyzeFailedRun_WithVerifyGateFailure_CreatesPendingProposal()
    {
        var run = FailedRunWithVerifyGate();

        var result = await _service.AnalyzeFailedRunAsync(run);

        result.Proposals.Should().NotBeEmpty();
        result.Proposals.Should().Contain(p =>
            p.SpecName.Equals("verify", StringComparison.OrdinalIgnoreCase)
            && p.Status == AgentSpecProposalStatus.Pending);
    }

    [Fact]
    public async Task ApproveProposal_WritesVersionedSpecAndEvolvedOverride()
    {
        var run = FailedRunWithVerifyGate();
        var analysis = await _service.AnalyzeFailedRunAsync(run);
        var proposal = analysis.Proposals.First(p => p.SpecName == "verify");

        var applied = await _service.ApproveAsync(proposal.Id, "reviewer");

        applied.Version.Should().Be(1);
        File.Exists(applied.EvolvedSpecPath).Should().BeTrue();
        File.Exists(applied.VersionPath).Should().BeTrue();

        var evolvedYaml = await File.ReadAllTextAsync(applied.EvolvedSpecPath);
        evolvedYaml.Should().Contain("browser_snapshot");
        evolvedYaml.Should().Contain("capture browser evidence");

        var changelog = await _versionStore.GetChangelogAsync("verify");
        changelog.Should().ContainSingle(e => e.Version == 1 && e.ProposalId == proposal.Id);
    }

    [Fact]
    public async Task RejectProposal_UpdatesStatusWithoutWritingEvolvedSpec()
    {
        var run = FailedRunWithVerifyGate();
        var analysis = await _service.AnalyzeFailedRunAsync(run);
        var proposal = analysis.Proposals.First();

        await _service.RejectAsync(proposal.Id, "reviewer", "not needed");

        var stored = await _proposalStore.GetAsync(proposal.Id);
        stored!.Status.Should().Be(AgentSpecProposalStatus.Rejected);
        Directory.Exists(Path.Combine(_root, "evolved")).Should().BeFalse();
    }

    [Fact]
    public void DiffApplier_AppendsToolsAndInstruction()
    {
        var applier = new AgentSpecDiffApplier();
        var baseline = new AgentSpecDocument
        {
            Name = "repair",
            MaxTurns = 12,
            Toolset = ["read_file"],
            Instruction = "repair baseline"
        };

        var evolved = applier.Apply(baseline, new AgentSpecProposalDiff
        {
            NewMaxTurns = 20,
            ToolsToAdd = ["todo_write"],
            InstructionAppend = "track attempts"
        });

        evolved.MaxTurns.Should().Be(20);
        evolved.Toolset.Should().Contain("todo_write");
        evolved.Instruction.Should().Contain("track attempts");
    }

    private static AppGenerationOrchestrator FailedRunWithVerifyGate()
    {
        var run = AppGenerationOrchestrator.Create("build app", "fp-klip");
        run.RecordQualityGate("verify_subagent", 2, passed: false, ["readiness check failed"]);
        run.RecordPipelineStageReached("Verify");
        run.MarkFailed("verify_subagent_failed");
        return run;
    }

    private sealed class StubAgentSpecRegistry : IAgentSpecRegistry
    {
        private readonly Dictionary<string, AgentSpec> _specs;

        public StubAgentSpecRegistry(params AgentSpec[] specs) =>
            _specs = specs.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string name, out AgentSpec spec) => _specs.TryGetValue(name, out spec!);

        public IReadOnlyList<AgentSpec> All => _specs.Values.ToList();
    }
}
