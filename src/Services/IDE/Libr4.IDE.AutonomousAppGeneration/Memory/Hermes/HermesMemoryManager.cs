using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;
using Libr4.IDE.Domain.AgentMemorySystem;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class HermesMemoryManager : IHermesMemoryManager
{
    private static readonly HashSet<string> IngestOnSuccessTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file", "edit_file", "apply_patch", "run_build", "run_tests"
    };

    private static readonly HashSet<string> IngestOnFailureTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "bash", "run_build", "run_tests"
    };

    private readonly IHermesMemoryStore _store;
    private readonly ICognitiveMemoryBridge? _cognitive;
    private readonly HermesMemoryManagerOptions _options;
    private readonly ILogger<HermesMemoryManager> _logger;

    public HermesMemoryManager(
        IHermesMemoryStore store,
        IOptions<HermesMemoryManagerOptions> options,
        ILogger<HermesMemoryManager> logger,
        ICognitiveMemoryBridge? cognitive = null)
    {
        _store = store;
        _cognitive = cognitive;
        _options = options.Value;
        _logger = logger;
    }

    public string ResolveFingerprint(GenerationPlan plan, string? requestFingerprint = null)
    {
        if (!string.IsNullOrWhiteSpace(requestFingerprint))
            return requestFingerprint;

        var stack = string.Join(',', plan.TechStack.Frameworks.Concat(plan.TechStack.Languages));
        return $"{plan.ApplicationName}|{stack}".Trim().ToLowerInvariant();
    }

    public async Task<string?> PrefetchBeforeTurnAsync(HermesTurnContext context, CancellationToken ct = default)
    {
        if (!_options.EnablePrefetch || string.IsNullOrWhiteSpace(context.RequestFingerprint))
            return null;

        var keyword = context.Keywords?.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k));
        var results = await _store.RetrieveAsync(
            new HermesMemoryQuery(
                context.RequestFingerprint,
                Keyword: keyword,
                TopK: _options.PrefetchTopK,
                UserId: context.UserId),
            ct).ConfigureAwait(false);

        if (results.Count == 0 && _cognitive is null)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("## relevant_memory");
        foreach (var result in results.Take(_options.MaxNudgesPerTurn))
        {
            var label = HermesMemoryScoring.KindLabel(result.Entry.Kind);
            sb.AppendLine(
                $"- [{label}] {result.Entry.Key}: {Truncate(result.Entry.Summary, 220)} " +
                $"(score: {result.RelevanceScore:F2}, reason: {result.RetrievalReason})");
        }

        if (_cognitive is not null && !string.IsNullOrWhiteSpace(context.RequestFingerprint))
        {
            var cognitiveQuery = keyword ?? context.Keywords?.FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(cognitiveQuery))
            {
                var layered = await _cognitive.SearchLayerAsync(
                    context.RequestFingerprint,
                    MemoryLayer.TaskSkills,
                    cognitiveQuery,
                    _options.PrefetchTopK,
                    ct).ConfigureAwait(false);

                foreach (var fragment in layered.Take(_options.MaxNudgesPerTurn))
                {
                    sb.AppendLine(
                        $"- [L1_task_skills] {Truncate(fragment.Content, 220)} " +
                        $"(score: {fragment.RelevanceScore:F2}, reason: cognitive_layer)");
                }
            }
        }

        return sb.Length <= "## relevant_memory".Length + 2 ? null : sb.ToString().TrimEnd();
    }

    public async Task SyncAfterToolAsync(
        HermesTurnContext context,
        string toolName,
        string toolOutput,
        bool success,
        CancellationToken ct = default)
    {
        if (!_options.EnableToolIngest
            || context.RunId is not Guid runId
            || string.IsNullOrWhiteSpace(context.RequestFingerprint)
            || string.IsNullOrWhiteSpace(toolOutput)
            || toolOutput.Length < _options.MinToolOutputCharsForIngest)
        {
            return;
        }

        var shouldIngest = success
            ? IngestOnSuccessTools.Contains(toolName)
            : IngestOnFailureTools.Contains(toolName);

        if (!shouldIngest)
            return;

        var kind = success ? MemoryKind.Procedural : MemoryKind.Episodic;
        var key = $"tool:{toolName}:{context.Stage}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        var summary = Truncate(toolOutput, 480);
        var score = success ? 1.0 : 0.2;

        await _store.UpsertAsync(
            new HermesMemoryEntry(
                Guid.NewGuid(),
                runId,
                context.UserId,
                context.RequestFingerprint,
                kind,
                context.Stage,
                key,
                summary,
                PayloadJson: null,
                Tokens: EstimateTokens(summary),
                Score: score,
                CreatedAtUtc: DateTime.UtcNow),
            ct).ConfigureAwait(false);

        _logger.LogDebug(
            "Hermes ingested {Kind} memory from tool {Tool} (success={Success})",
            kind,
            toolName,
            success);
    }

    public async Task OnPreCompactAsync(HermesTurnContext context, CancellationToken ct = default)
    {
        if (!_options.EnablePreCompactConsolidation
            || context.RunId is not Guid runId
            || string.IsNullOrWhiteSpace(context.RequestFingerprint))
        {
            return;
        }

        var episodic = await _store.RetrieveAsync(
            new HermesMemoryQuery(context.RequestFingerprint, TopK: 200, Kinds: [MemoryKind.Episodic]),
            ct).ConfigureAwait(false);

        if (episodic.Count < 2)
            return;

        var grouped = episodic
            .GroupBy(r => r.Entry.Stage, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2);

        foreach (var group in grouped)
        {
            var merged = string.Join(" | ", group.Select(r => Truncate(r.Entry.Summary, 120)).Take(6));
            var key = $"consolidated:{group.Key}";
            await _store.UpsertAsync(
                new HermesMemoryEntry(
                    Guid.NewGuid(),
                    runId,
                    context.UserId,
                    context.RequestFingerprint,
                    MemoryKind.Semantic,
                    group.Key,
                    key,
                    Truncate(merged, 900),
                    PayloadJson: null,
                    Tokens: EstimateTokens(merged),
                    Score: 1.5,
                    CreatedAtUtc: DateTime.UtcNow),
                ct).ConfigureAwait(false);
        }
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
