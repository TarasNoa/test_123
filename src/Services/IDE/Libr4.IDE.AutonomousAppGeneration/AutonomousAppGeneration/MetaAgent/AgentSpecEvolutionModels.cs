using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;

public enum AgentSpecProposalStatus
{
    Pending,
    Approved,
    Rejected,
    Applied
}

public sealed class AgentSpecProposalDiff
{
    public int? NewMaxTurns { get; set; }
    public List<string> ToolsToAdd { get; set; } = [];
    public string? InstructionAppend { get; set; }
}

public sealed record AgentSpecProposal(
    Guid Id,
    Guid RunId,
    string SpecName,
    AgentSpecProposalDiff Diff,
    string Rationale,
    AgentSpecProposalStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolvedBy,
    string? RejectionReason,
    int? AppliedVersion);

public sealed record AgentSpecVersionRecord(
    string SpecName,
    int Version,
    string FilePath,
    string ChangeSummary,
    Guid? SourceProposalId,
    DateTime CreatedAtUtc);

public sealed record AgentSpecChangelogEntry(
    string SpecName,
    int Version,
    string Summary,
    Guid? RunId,
    Guid? ProposalId,
    DateTime CreatedAtUtc);

public sealed record AgentSpecEvolutionAnalysisResult(
    Guid RunId,
    IReadOnlyList<AgentSpecProposal> Proposals);

public sealed record ApplyProposalResult(
    Guid ProposalId,
    int Version,
    string SpecName,
    string EvolvedSpecPath,
    string VersionPath);

public interface IAgentSpecProposalStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task InsertAsync(AgentSpecProposal proposal, CancellationToken ct = default);

    Task<AgentSpecProposal?> GetAsync(Guid proposalId, CancellationToken ct = default);

    Task<IReadOnlyList<AgentSpecProposal>> ListAsync(
        AgentSpecProposalStatus? status = null,
        CancellationToken ct = default);

    Task UpdateStatusAsync(
        Guid proposalId,
        AgentSpecProposalStatus status,
        string? resolvedBy = null,
        string? rejectionReason = null,
        int? appliedVersion = null,
        CancellationToken ct = default);
}

public interface IAgentSpecVersionStore
{
    Task<int> GetLatestVersionAsync(string specName, CancellationToken ct = default);

    Task<string> SaveVersionAsync(
        string specName,
        int version,
        AgentSpecDocument document,
        string changeSummary,
        Guid? sourceProposalId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentSpecVersionRecord>> ListVersionsAsync(string specName, CancellationToken ct = default);

    Task<IReadOnlyList<AgentSpecChangelogEntry>> GetChangelogAsync(string specName, CancellationToken ct = default);
}

public interface IAgentSpecEvolutionService
{
    Task<AgentSpecEvolutionAnalysisResult> AnalyzeFailedRunAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentSpecProposal>> ListProposalsAsync(
        AgentSpecProposalStatus? status = null,
        CancellationToken ct = default);

    Task<ApplyProposalResult> ApproveAsync(Guid proposalId, string? actor = null, CancellationToken ct = default);

    Task RejectAsync(Guid proposalId, string? actor = null, string? reason = null, CancellationToken ct = default);
}
