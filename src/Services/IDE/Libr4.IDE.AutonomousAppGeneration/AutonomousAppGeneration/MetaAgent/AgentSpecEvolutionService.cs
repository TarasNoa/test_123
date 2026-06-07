using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;

public sealed class AgentSpecEvolutionAnalyzer
{
    public IReadOnlyList<(string SpecName, AgentSpecProposalDiff Diff, string Rationale)> Analyze(
        AppGenerationOrchestrator orchestrator) =>
        FSharpAlgorithmsBridge.AnalyzeAgentSpecEvolution(orchestrator);
}

public sealed class AgentSpecDiffApplier
{
    public AgentSpecDocument Apply(AgentSpecDocument baseline, AgentSpecProposalDiff diff) =>
        FSharpAlgorithmsBridge.ApplyAgentSpecDiff(baseline, diff);

    public string BuildUnifiedDiffPreview(AgentSpecDocument before, AgentSpecDocument after) =>
        FSharpAlgorithmsBridge.BuildAgentSpecDiffPreview(before, after);
}

public sealed class AgentSpecEvolutionService : IAgentSpecEvolutionService
{
    private readonly AgentSpecEvolutionOptions _options;
    private readonly IAgentSpecProposalStore _proposals;
    private readonly IAgentSpecVersionStore _versions;
    private readonly IAgentSpecRegistry _registry;
    private readonly AgentSpecEvolutionAnalyzer _analyzer;
    private readonly AgentSpecDiffApplier _applier;
    private readonly ILogger<AgentSpecEvolutionService> _logger;

    public AgentSpecEvolutionService(
        IOptions<AgentSpecEvolutionOptions> options,
        IAgentSpecProposalStore proposals,
        IAgentSpecVersionStore versions,
        IAgentSpecRegistry registry,
        ILogger<AgentSpecEvolutionService> logger)
    {
        _options = options.Value;
        _proposals = proposals;
        _versions = versions;
        _registry = registry;
        _analyzer = new AgentSpecEvolutionAnalyzer();
        _applier = new AgentSpecDiffApplier();
        _logger = logger;
    }

    public async Task<AgentSpecEvolutionAnalysisResult> AnalyzeFailedRunAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new AgentSpecEvolutionAnalysisResult(orchestrator.Id, Array.Empty<AgentSpecProposal>());

        var analyzed = _analyzer.Analyze(orchestrator);
        var created = new List<AgentSpecProposal>();
        foreach (var (specName, diff, rationale) in analyzed)
        {
            var proposal = new AgentSpecProposal(
                Guid.NewGuid(),
                orchestrator.Id,
                specName,
                diff,
                rationale,
                AgentSpecProposalStatus.Pending,
                DateTime.UtcNow,
                null,
                null,
                null,
                null);
            await _proposals.InsertAsync(proposal, ct).ConfigureAwait(false);
            created.Add(proposal);
        }

        _logger.LogInformation(
            "KLIP meta-agent created {Count} spec proposal(s) for failed run {RunId}",
            created.Count,
            orchestrator.Id);

        return new AgentSpecEvolutionAnalysisResult(orchestrator.Id, created);
    }

    public Task<IReadOnlyList<AgentSpecProposal>> ListProposalsAsync(
        AgentSpecProposalStatus? status = null,
        CancellationToken ct = default) =>
        _proposals.ListAsync(status, ct);

    public async Task<ApplyProposalResult> ApproveAsync(Guid proposalId, string? actor = null, CancellationToken ct = default)
    {
        var proposal = await _proposals.GetAsync(proposalId, ct).ConfigureAwait(false)
                       ?? throw new InvalidOperationException($"proposal_not_found:{proposalId}");

        if (proposal.Status is AgentSpecProposalStatus.Applied or AgentSpecProposalStatus.Approved)
            throw new InvalidOperationException($"proposal_already_resolved:{proposalId}");

        var baseline = LoadBaselineDocument(proposal.SpecName);
        var evolved = _applier.Apply(baseline, proposal.Diff);
        var preview = _applier.BuildUnifiedDiffPreview(baseline, evolved);

        var nextVersion = await _versions.GetLatestVersionAsync(proposal.SpecName, ct).ConfigureAwait(false) + 1;
        var versionPath = await _versions.SaveVersionAsync(
            proposal.SpecName,
            nextVersion,
            evolved,
            preview,
            proposal.Id,
            ct).ConfigureAwait(false);

        var evolvedPath = WriteEvolvedSpec(proposal.SpecName, evolved);

        await _proposals.UpdateStatusAsync(
            proposalId,
            AgentSpecProposalStatus.Applied,
            actor,
            appliedVersion: nextVersion,
            ct: ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Approved KLIP proposal {ProposalId} -> {SpecName} v{Version}",
            proposalId,
            proposal.SpecName,
            nextVersion);

        return new ApplyProposalResult(proposalId, nextVersion, proposal.SpecName, evolvedPath, versionPath);
    }

    public async Task RejectAsync(
        Guid proposalId,
        string? actor = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        var proposal = await _proposals.GetAsync(proposalId, ct).ConfigureAwait(false)
                       ?? throw new InvalidOperationException($"proposal_not_found:{proposalId}");

        if (proposal.Status is AgentSpecProposalStatus.Applied or AgentSpecProposalStatus.Rejected)
            throw new InvalidOperationException($"proposal_already_resolved:{proposalId}");

        await _proposals.UpdateStatusAsync(
            proposalId,
            AgentSpecProposalStatus.Rejected,
            actor,
            reason,
            ct: ct).ConfigureAwait(false);
    }

    private AgentSpecDocument LoadBaselineDocument(string specName)
    {
        if (_registry.TryGet(specName, out var spec))
        {
            return new AgentSpecDocument
            {
                Name = spec.Name,
                Model = spec.Model,
                MaxTurns = spec.MaxTurns,
                MaxTokens = spec.MaxTokens,
                Toolset = spec.Toolset.ToList(),
                Instruction = spec.Instruction,
                Permissions = spec.Permissions
            };
        }

        var bundled = ResolveBundledPath(specName);
        if (bundled is not null && File.Exists(bundled))
            return AgentSpecLoader.LoadFromFile(bundled);

        return new AgentSpecDocument { Name = specName, MaxTurns = 12 };
    }

    private string? ResolveBundledPath(string specName)
    {
        var dir = Path.IsPathRooted(_options.BundledSpecsDirectory)
            ? _options.BundledSpecsDirectory
            : Path.Combine(AppContext.BaseDirectory, _options.BundledSpecsDirectory);
        var candidate = Path.Combine(dir, $"{specName}.agent.yaml");
        return File.Exists(candidate) ? candidate : null;
    }

    private string WriteEvolvedSpec(string specName, AgentSpecDocument document)
    {
        var root = Path.IsPathRooted(_options.EvolvedSpecsRoot)
            ? _options.EvolvedSpecsRoot
            : Path.GetFullPath(_options.EvolvedSpecsRoot);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"{specName}.agent.yaml");
        File.WriteAllText(path, AgentSpecYamlWriter.Write(document));
        return path;
    }
}

public static class AgentSpecYamlWriter
{
    public static string Write(AgentSpecDocument doc)
    {
        var lines = new List<string> { $"name: {doc.Name}" };
        if (!string.IsNullOrWhiteSpace(doc.Extend))
            lines.Add($"extend: {doc.Extend}");
        if (!string.IsNullOrWhiteSpace(doc.Model))
            lines.Add($"model: {doc.Model}");
        if (doc.MaxTurns is not null)
            lines.Add($"maxTurns: {doc.MaxTurns}");
        if (doc.MaxTokens is not null)
            lines.Add($"maxTokens: {doc.MaxTokens}");
        if (!string.IsNullOrWhiteSpace(doc.Permissions))
            lines.Add($"permissions: {doc.Permissions}");

        if (doc.Toolset.Count > 0)
        {
            lines.Add("toolset:");
            foreach (var tool in doc.Toolset)
                lines.Add($"  - {tool}");
        }

        lines.Add("instruction: |");
        foreach (var line in (doc.Instruction ?? string.Empty).Split('\n'))
            lines.Add($"  {line}".TrimEnd());

        return string.Join('\n', lines) + Environment.NewLine;
    }
}

public sealed class AgentSpecEvolutionFinalizationHook : Services.IAutonomousFinalizationHook
{
    private readonly IAgentSpecEvolutionService _evolution;
    private readonly AgentSpecEvolutionOptions _options;

    public AgentSpecEvolutionFinalizationHook(
        IAgentSpecEvolutionService evolution,
        IOptions<AgentSpecEvolutionOptions> options)
    {
        _evolution = evolution;
        _options = options.Value;
    }

    public int Order => 87;

    public string Name => "klip_meta_agent_analyze";

    public Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.AutoAnalyzeFailedRuns)
            return Task.CompletedTask;

        if (orchestrator.Status != GenerationStatus.Failed)
            return Task.CompletedTask;

        return _evolution.AnalyzeFailedRunAsync(orchestrator, ct);
    }
}
