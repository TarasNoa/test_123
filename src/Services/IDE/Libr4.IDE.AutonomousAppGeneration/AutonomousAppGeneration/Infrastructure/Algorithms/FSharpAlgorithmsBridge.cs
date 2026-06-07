using System.Collections.ObjectModel;
using Libr4.IDE.AutonomousAppGeneration.Algorithms.FastContext;
using Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching;
using Libr4.IDE.AutonomousAppGeneration.Algorithms.Playbook;
using Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph;
using Microsoft.FSharp.Core;
using FSharpPatch = Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.PatchApplicator;
using FSharpDiffParser = Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.UnifiedDiffParser;
using FSharpFusion = Libr4.IDE.AutonomousAppGeneration.Algorithms.FastContext.FusionRanker;
using FSharpPlaybook = Libr4.IDE.AutonomousAppGeneration.Algorithms.Playbook.RepairPlaybookSignature;
using FSharpHermes = Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.HermesMemoryScoring;
using FSharpRrf = Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.ReciprocalRankFusion;
using FSharpCompaction = Libr4.IDE.AutonomousAppGeneration.Algorithms.Context.HeuristicSemanticCompactor;
using FSharpFragments = Libr4.IDE.AutonomousAppGeneration.Algorithms.Context.ContextFragmentBudget;
using FSharpCircuit = Libr4.IDE.AutonomousAppGeneration.Algorithms.ModelRouting.RoleModelCircuit;
using FSharpSpecEvolution = Libr4.IDE.AutonomousAppGeneration.Algorithms.MetaAgent.AgentSpecEvolution;
using FSharpAgentParse = Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.AgentResponseParser;
using FSharpReasoning = Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.ReasoningChannelParser;
using FSharpTurn = Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.AgentSessionTurnMachine;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Reasoning;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

/// <summary>C# в†” F# bridge for Wave 1 algorithm migrations (Golden Stack Brain layer).</summary>
internal static class FSharpAlgorithmsBridge
{
    private static ReadOnlyDictionary<string, string> ToContentsDict(IReadOnlyDictionary<string, string>? contentsByPath) =>
        contentsByPath is null
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(contentsByPath, StringComparer.OrdinalIgnoreCase));

    public static RepoGraphEngine.RepoGraphDto BuildRepoGraph(
        IReadOnlyList<string> relativePaths,
        IReadOnlyDictionary<string, string>? contentsByPath)
    {
        var graph = RepoGraphEngine.buildGraph(relativePaths.ToArray(), ToContentsDict(contentsByPath));
        return RepoGraphLibClangAugmenter.Augment(graph, relativePaths, contentsByPath);
    }

    public static string[] OrderForGeneration(
        IReadOnlyList<string> relativePaths,
        IReadOnlyDictionary<string, string>? contentsByPath) =>
        RepoGraphEngine.orderForGeneration(relativePaths.ToArray(), ToContentsDict(contentsByPath));

    public static string[] OrderForRepair(
        IReadOnlyList<string> relativePaths,
        IReadOnlyDictionary<string, string>? contentsByPath) =>
        RepoGraphEngine.orderForRepair(relativePaths.ToArray(), ToContentsDict(contentsByPath));

    public static UnifiedDiffParser.UnifiedDiffDto ParseUnifiedDiff(string patch, string? fallbackPath) =>
        FSharpDiffParser.parse(patch, ToOption(fallbackPath));

    public static PatchApplicator.PatchApplyResultDto ApplyExact(string original, UnifiedDiffParser.UnifiedDiffDto diff) =>
        FSharpPatch.applyExact(original, diff);

    public static PatchApplicator.PatchApplyResultDto ApplyFuzzy(string original, UnifiedDiffParser.UnifiedDiffDto diff) =>
        FSharpPatch.applyFuzzy(original, diff);

    public static PatchApplicator.PatchApplyResultDto ApplyThreeWay(string original, string? baseContent, UnifiedDiffParser.UnifiedDiffDto diff) =>
        FSharpPatch.applyThreeWay(original, ToOption(baseContent), diff);

    public static FusionRanker.FusedHitDto[] FuseSearchHits(
        FusionRanker.SearchHitDto[] ripgrepHits,
        (FusionRanker.SearchHitDto Hit, double Boost)[] graphBoosts,
        int limit,
        FusionRanker.FusionOptions options) =>
        FSharpFusion.fuse(
            ripgrepHits,
            graphBoosts.Select(b => Tuple.Create(b.Hit, b.Boost)).ToArray(),
            limit,
            options);

    public static (string Signature, string StackPattern) BuildPlaybookSignature(
        (string ErrorType, string? FilePath, string Message)[] errors,
        string? buildLog,
        string? applicationName,
        string[] languages,
        string[] frameworks)
    {
        var result = FSharpPlaybook.fromErrors(
            errors.Select(e => Tuple.Create(e.ErrorType, ToOption(e.FilePath), e.Message)).ToArray(),
            ToOption(buildLog),
            ToOption(applicationName),
            languages,
            frameworks);
        return (result.Item1, result.Item2);
    }

    public static FSharpOption<string> ToOption(string? value) =>
        string.IsNullOrWhiteSpace(value) ? FSharpOption<string>.None : FSharpOption<string>.Some(value);

    public static Context.RepoGraph.RepoGraph ToRepoGraph(RepoGraphEngine.RepoGraphDto dto) =>
        new()
        {
            Files = dto.Files.Select(f => new Context.RepoGraph.RepoFileNode(f.RelativePath, f.Language)).ToList(),
            Edges = dto.Edges.Select(e => new Context.RepoGraph.RepoDependencyEdge(e.FromPath, e.ToPath, e.Kind)).ToList()
        };

    public static UnifiedDiffParser.UnifiedDiffDto ToFSharpDiff(AgentRuntime.Patching.UnifiedDiff diff) =>
        new(
            ToOption(diff.TargetPath),
            diff.Hunks.Select(h => new UnifiedDiffParser.DiffHunkDto(
                h.OldStart, h.OldCount, h.NewStart, h.NewCount, h.Lines.ToArray())).ToArray());

    public static AgentRuntime.Patching.PatchApplyResult ToPatchResult(PatchApplicator.PatchApplyResultDto dto) =>
        new(
            dto.Success,
            OptionModule.IsSome(dto.PatchedContent) ? dto.PatchedContent.Value : null,
            OptionModule.IsSome(dto.ConflictReport) ? dto.ConflictReport.Value : null,
            (AgentRuntime.Patching.PatchApplyMode)(int)dto.Mode);

    public static FusionRanker.SearchHitDto ToFSharpHit(FastContext.CodebaseSearchHit hit) =>
        new(hit.Path, hit.StartLine, hit.Score, hit.MatchKind, hit.Snippet ?? string.Empty);

    private static FSharpHermes.MemoryEntryDto ToHermesEntry(Memory.Hermes.HermesMemoryEntry entry) =>
        new(
            (int)entry.Kind,
            entry.Stage,
            entry.Key,
            entry.Summary,
            entry.Score,
            entry.CreatedAtUtc);

    public static double ComputeHermesRelevanceScore(Memory.Hermes.HermesMemoryEntry entry, string? keyword) =>
        FSharpHermes.computeRelevanceScore(
            ToHermesEntry(entry),
            ToOption(keyword),
            DateTime.UtcNow);

    public static string BuildHermesRetrievalReason(Memory.Hermes.HermesMemoryEntry entry, string? keyword) =>
        FSharpHermes.buildRetrievalReason(ToHermesEntry(entry), ToOption(keyword));

    public static string HermesKindLabel(int kind) =>
        FSharpHermes.kindLabelPublic(kind);

    public static IReadOnlyList<(string Id, double Score)> FuseReciprocalRank(
        IReadOnlyList<IReadOnlyList<string>> rankedLists,
        double k)
    {
        var lists = rankedLists.Select(list => list.ToArray()).ToArray();
        return FSharpRrf.fuse(lists, k).Select(item => (item.Id, item.Score)).ToList();
    }

    public static SemanticCompactionSummary SummarizeConversation(
        IReadOnlyList<AgentConversationTurn> turns,
        IReadOnlyList<string> manifestPaths)
    {
        var dtoTurns = turns
            .Select(t => new FSharpCompaction.ConversationTurnDto(t.Role, t.Content))
            .ToArray();
        var summary = FSharpCompaction.summarize(dtoTurns, manifestPaths.ToArray());
        return new SemanticCompactionSummary(
            summary.Decisions,
            summary.FilesTouched,
            summary.OpenIssues,
            summary.NextActions,
            summary.ErrorsResolved);
    }

    private static FSharpFragments.FragmentDto ToFragmentDto(ContextFragment fragment) =>
        new(
            ContextFragmentOptions.ToKey(fragment.Type),
            (int)fragment.Type,
            fragment.Content,
            fragment.Priority,
            fragment.Provenance?
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => Tuple.Create(kv.Key, kv.Value))
                .ToArray() ?? Array.Empty<Tuple<string, string>>());

    public static string AssembleContextFragments(
        IReadOnlyList<ContextFragment> fragments,
        int maxTotalChars,
        IReadOnlyDictionary<string, int> perTypeCaps) =>
        FSharpFragments.assemble(
            fragments.Select(ToFragmentDto).ToArray(),
            maxTotalChars,
            new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(perTypeCaps, StringComparer.OrdinalIgnoreCase)));

    public static string FormatContextFragmentMarker(ContextFragment fragment) =>
        FSharpFragments.formatMarker(
            ContextFragmentOptions.ToKey(fragment.Type),
            fragment.Provenance?
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => Tuple.Create(kv.Key, kv.Value))
                .ToArray() ?? Array.Empty<Tuple<string, string>>());

    public static int DefaultContextFragmentPriority(ContextFragmentType type) =>
        FSharpFragments.defaultPriority((int)type);

    public static string BuildRoleModelCircuitKey(string role, string model) =>
        FSharpCircuit.buildKey(role, model);

    public static bool IsRoleCircuitOpen(RoleModelCircuitBreaker.CircuitState state, int openSeconds) =>
        FSharpCircuit.isOpen(ToCircuitDto(state), DateTime.UtcNow, openSeconds);

    public static bool ShouldRoleCircuitHalfOpen(RoleModelCircuitBreaker.CircuitState state, int openSeconds) =>
        FSharpCircuit.shouldTransitionToHalfOpen(ToCircuitDto(state), DateTime.UtcNow, openSeconds);

    public static RoleModelCircuitBreaker.CircuitState AdvanceRoleCircuitOnSuccess(
        RoleModelCircuitBreaker.CircuitState state) =>
        FromCircuitDto(FSharpCircuit.onSuccess(ToCircuitDto(state)));

    public static RoleModelCircuitBreaker.CircuitState AdvanceRoleCircuitOnFailure(
        RoleModelCircuitBreaker.CircuitState state,
        int threshold) =>
        FromCircuitDto(FSharpCircuit.onFailure(ToCircuitDto(state), threshold, DateTime.UtcNow));

    private static FSharpCircuit.CircuitStateDto ToCircuitDto(RoleModelCircuitBreaker.CircuitState state) =>
        new(
            state.Current,
            state.Failures,
            state.OpenedAtUtc.HasValue
                ? FSharpOption<DateTime>.Some(state.OpenedAtUtc.Value)
                : FSharpOption<DateTime>.None);

    private static RoleModelCircuitBreaker.CircuitState FromCircuitDto(FSharpCircuit.CircuitStateDto dto) =>
        new()
        {
            Current = dto.Current,
            Failures = dto.Failures,
            OpenedAtUtc = OptionModule.IsSome(dto.OpenedAtUtc) ? dto.OpenedAtUtc.Value : null
        };

    public static IReadOnlyList<(string SpecName, AgentSpecProposalDiff Diff, string Rationale)> AnalyzeAgentSpecEvolution(
        AppGenerationOrchestrator orchestrator)
    {
        var verifyGate = orchestrator.QualityGates
            .LastOrDefault(g => g.Stage.Equals("verify_subagent", StringComparison.OrdinalIgnoreCase));

        var run = new FSharpSpecEvolution.FailedRunDto(
            orchestrator.Status == GenerationStatus.Failed,
            verifyGate is { Passed: false },
            verifyGate?.Reasons.ToArray() ?? Array.Empty<string>(),
            orchestrator.Iterations.Count(i => !i.Succeeded),
            ToOption(orchestrator.FailureReason),
            orchestrator.PipelineStageReached ?? string.Empty,
            orchestrator.Files.Count);

        return FSharpSpecEvolution.analyze(run)
            .Select(p => (
                p.SpecName,
                new AgentSpecProposalDiff
                {
                    NewMaxTurns = FromIntOption(p.Diff.NewMaxTurns),
                    ToolsToAdd = p.Diff.ToolsToAdd.ToList(),
                    InstructionAppend = OptionModule.IsSome(p.Diff.InstructionAppend) ? p.Diff.InstructionAppend.Value : null
                },
                p.Rationale))
            .ToList();
    }

    public static AgentSpecDocument ApplyAgentSpecDiff(AgentSpecDocument baseline, AgentSpecProposalDiff diff)
    {
        var evolved = FSharpSpecEvolution.applyDiff(ToSpecDto(baseline), ToDiffDto(diff));
        return FromSpecDto(evolved, baseline);
    }

    public static string BuildAgentSpecDiffPreview(AgentSpecDocument before, AgentSpecDocument after) =>
        FSharpSpecEvolution.buildDiffPreview(ToSpecDto(before), ToSpecDto(after));

    private static FSharpSpecEvolution.ProposalDiffDto ToDiffDto(AgentSpecProposalDiff diff) =>
        new(
            ToIntOption(diff.NewMaxTurns),
            diff.ToolsToAdd.ToArray(),
            ToOption(diff.InstructionAppend));

    private static FSharpOption<int> ToIntOption(int? value) =>
        value.HasValue ? FSharpOption<int>.Some(value.Value) : FSharpOption<int>.None;

    private static int? FromIntOption(FSharpOption<int> value) =>
        OptionModule.IsSome(value) ? value.Value : null;

    private static FSharpSpecEvolution.SpecDocumentDto ToSpecDto(AgentSpecDocument doc) =>
        new(
            doc.Name,
            ToOption(doc.Extend),
            ToOption(doc.Model),
            ToIntOption(doc.MaxTurns),
            ToIntOption(doc.MaxTokens),
            doc.Toolset.ToArray(),
            ToOption(doc.Instruction),
            ToOption(doc.Permissions));

    private static AgentSpecDocument FromSpecDto(FSharpSpecEvolution.SpecDocumentDto dto, AgentSpecDocument template) =>
        new()
        {
            Name = dto.Name,
            Extend = OptionModule.IsSome(dto.Extend) ? dto.Extend.Value : template.Extend,
            Model = OptionModule.IsSome(dto.Model) ? dto.Model.Value : template.Model,
            MaxTurns = FromIntOption(dto.MaxTurns),
            MaxTokens = FromIntOption(dto.MaxTokens),
            Toolset = dto.Toolset.ToList(),
            Instruction = OptionModule.IsSome(dto.Instruction) ? dto.Instruction.Value : template.Instruction,
            Permissions = OptionModule.IsSome(dto.Permissions) ? dto.Permissions.Value : template.Permissions,
            Browser = template.Browser
        };

    public static ReasoningParseResult SplitReasoningChannel(string raw)
    {
        var dto = FSharpReasoning.split(raw);
        return new ReasoningParseResult(
            dto.VisibleContent,
            OptionModule.IsSome(dto.ReasoningContent) ? dto.ReasoningContent.Value : null);
    }

    public static AgentTurnResponse ParseAgentResponse(string raw, bool stripReasoning)
    {
        var dto = FSharpAgentParse.parse(raw, stripReasoning);
        AgentToolCall? toolCall = null;

        if (dto.Action == 0
            && OptionModule.IsSome(dto.ToolName)
            && OptionModule.IsSome(dto.ToolInputJson))
        {
            using var inputDoc = System.Text.Json.JsonDocument.Parse(dto.ToolInputJson.Value);
            toolCall = new AgentToolCall(dto.ToolName.Value, inputDoc.RootElement.Clone());
        }

        return new AgentTurnResponse(
            (AgentTurnAction)dto.Action,
            toolCall,
            OptionModule.IsSome(dto.Summary) ? dto.Summary.Value : null,
            dto.VisibleContent,
            OptionModule.IsSome(dto.ReasoningContent) ? dto.ReasoningContent.Value : null);
    }

    private static FSharpTurn.PatchEntryDto[] ToPatchEntries(IReadOnlyDictionary<string, GeneratedFile> patches) =>
        patches.Values
            .Select(p => new FSharpTurn.PatchEntryDto(
                FixerPatchScopePolicy.NormalizePatchRelativePath(p.RelativePath),
                p.Content))
            .ToArray();

    private static string[] NormalizeTargetPaths(IReadOnlyList<string>? targetPaths) =>
        targetPaths is null || targetPaths.Count == 0
            ? Array.Empty<string>()
            : targetPaths
                .Select(FixerPatchScopePolicy.NormalizePatchRelativePath)
                .Where(p => p.Length > 0)
                .ToArray();

    public static bool TargetsSatisfied(
        IReadOnlyDictionary<string, GeneratedFile> patches,
        IReadOnlyList<string>? targetPaths,
        int minChars = 20) =>
        FSharpTurn.targetsSatisfied(
            ToPatchEntries(patches),
            NormalizeTargetPaths(targetPaths),
            minChars);

    public static IReadOnlyList<GeneratedFile> FilterSessionPatches(
        IReadOnlyDictionary<string, GeneratedFile> patches,
        IReadOnlyList<string>? targetPaths)
    {
        var filtered = FSharpTurn.filterPatches(
            ToPatchEntries(patches),
            NormalizeTargetPaths(targetPaths));
        var byPath = patches.Values.ToDictionary(
            p => FixerPatchScopePolicy.NormalizePatchRelativePath(p.RelativePath),
            p => p,
            StringComparer.OrdinalIgnoreCase);
        return filtered
            .Select(p => byPath[p.Path])
            .ToList();
    }

    public static FSharpTurn.AfterParseDecision DecideAfterParse(
        bool isGeneration,
        AgentTurnAction action,
        bool hasToolCall,
        IReadOnlyDictionary<string, GeneratedFile> patches,
        IReadOnlyList<string>? targetPaths,
        int minChars = 20) =>
        FSharpTurn.decideAfterParse(
            isGeneration,
            (FSharpTurn.TurnAction)(int)action,
            hasToolCall,
            ToPatchEntries(patches),
            NormalizeTargetPaths(targetPaths),
            minChars);

    public static (FSharpTurn.AfterToolDecision Decision, int ConsecutiveReadOnlyTools) DecideAfterTool(
        bool isGeneration,
        int consecutiveReadOnlyTools,
        int maxInvestigationReadOnly,
        IReadOnlyDictionary<string, GeneratedFile> patches,
        IReadOnlyList<string>? targetPaths,
        bool toolIsReadOnly,
        int minChars = 20)
    {
        var counters = new FSharpTurn.TurnCountersDto(0, consecutiveReadOnlyTools);
        var result = FSharpTurn.decideAfterTool(
            isGeneration,
            counters,
            maxInvestigationReadOnly,
            ToPatchEntries(patches),
            NormalizeTargetPaths(targetPaths),
            minChars,
            toolIsReadOnly);
        return ((FSharpTurn.AfterToolDecision)(int)result.Item1, result.Item2.ConsecutiveReadOnlyTools);
    }
}