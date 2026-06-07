using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class ContextPackBuilder : IContextPackBuilder
{
    private readonly IMemoryStore _memory;
    private readonly IRepoGraphBuilder? _repoGraph;
    private readonly IRepoContextFormatter _repoContextFormatter;
    private readonly ILspBridge? _lspBridge;
    private readonly IFastContextPrefetcher? _fastContext;
    private readonly IShadowWorkspaceAccessor? _workspaceAccessor;
    private readonly ContextPackOptions _options;
    private readonly FastContextOptions _fastContextOptions;

    public ContextPackBuilder(
        IMemoryStore memory,
        IOptions<ContextPackOptions> options,
        IRepoContextFormatter repoContextFormatter,
        IRepoGraphBuilder? repoGraph = null,
        ILspBridge? lspBridge = null,
        IFastContextPrefetcher? fastContext = null,
        IShadowWorkspaceAccessor? workspaceAccessor = null,
        IOptions<FastContextOptions>? fastContextOptions = null)
    {
        _memory = memory;
        _options = options.Value;
        _fastContextOptions = fastContextOptions?.Value ?? new FastContextOptions();
        _repoContextFormatter = repoContextFormatter;
        _repoGraph = repoGraph;
        _lspBridge = lspBridge;
        _fastContext = fastContext;
        _workspaceAccessor = workspaceAccessor;
    }

    public async Task<string> BuildPackAsync(
        string stage,
        AppGenerationOrchestrator orchestrator,
        int maxChars,
        CancellationToken ct = default)
    {
        maxChars = Math.Clamp(Math.Min(maxChars, GetStageBudget(stage)), 512, 32_000);
        var plan = orchestrator.Plan;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# stage={stage}");
        sb.AppendLine($"# run={orchestrator.Id:D}");
        sb.AppendLine($"# request={Truncate(orchestrator.UserRequest, 400)}");
        if (plan is not null)
        {
            sb.AppendLine($"# app={plan.ApplicationName}");
            sb.AppendLine($"# stack={string.Join(',', plan.TechStack.Languages)} / {string.Join(',', plan.TechStack.Frameworks)}");
            sb.AppendLine($"# phases={plan.Phases.Count}; build_cmds={plan.BuildCommands.Count}; test_cmds={plan.TestCommands.Count}");
        }

        var lastErrors = orchestrator.Iterations.LastOrDefault()?.Errors;
        if (lastErrors is { Count: > 0 })
        {
            sb.AppendLine("## recent_errors");
            foreach (var err in lastErrors.Take(Math.Max(1, _options.MaxRecentErrors)))
            {
                sb.AppendLine($"- {err.ErrorType}: {Truncate(err.Message, 240)} @ {err.FilePath}:{err.LineNumber}");
            }
        }

        if (orchestrator.Files.Count > 0)
        {
            sb.AppendLine("## files");
            var maxFiles = Math.Max(1, _options.MaxFilesListed);
            foreach (var f in orchestrator.Files.Take(maxFiles))
                sb.AppendLine($"- {f.RelativePath} ({f.Language})");
            if (orchestrator.Files.Count > maxFiles)
                sb.AppendLine($"... +{orchestrator.Files.Count - maxFiles} more");
        }

        var retrieved = await _memory.RetrieveAsync(
                new MemoryQuery(
                    orchestrator.RequestFingerprint,
                    Keyword: null,
                    TopK: Math.Clamp(_options.MemoryTopK, 1, 64)),
                ct)
            .ConfigureAwait(false);
        if (retrieved.Count > 0)
        {
            sb.AppendLine("## memory_retrieval (same fingerprint, newest first, with reasons)");
            foreach (var r in retrieved.Take(Math.Max(1, _options.MaxMemoryItemsInPack)))
            {
                var payloadNote = string.IsNullOrEmpty(r.Record.PayloadJson) ? string.Empty : " [has_payload]";
                sb.AppendLine(
                    $"- [{r.Record.Kind}] {r.Record.Stage}/{r.Record.Key}: {Truncate(r.Record.Summary, 200)}{payloadNote}");
                sb.AppendLine($"  (reason: {r.RetrievalReason}, score: {r.RelevanceScore:F2})");
            }
        }

        if (_options.UseRepoGraphOrdering && _repoGraph is not null && orchestrator.Files.Count > 0)
        {
            var remaining = Math.Max(0, maxChars - sb.Length - 32);
            if (remaining > 128)
            {
                var related = _repoContextFormatter.BuildRelatedContext(
                    orchestrator.Files,
                    _repoGraph,
                    remaining,
                    ResolveFocusPaths(stage, orchestrator),
                    Math.Max(1, _options.MaxRelatedFilesInPack));
                if (!string.IsNullOrWhiteSpace(related))
                {
                    sb.AppendLine("## related_files");
                    sb.Append(related);
                }
            }
        }

        if (_lspBridge is not null && IsLspStage(stage) && orchestrator.Files.Count > 0)
        {
            var focus = ResolveFocusPaths(stage, orchestrator);
            var lsp = await _lspBridge.GetWorkspaceContextAsync(
                    orchestrator.Files,
                    plan,
                    lastErrors,
                    focus,
                    ct)
                .ConfigureAwait(false);
            var formatted = lsp.FormatForContextPack(Math.Max(256, maxChars - sb.Length - 32));
            if (!string.IsNullOrWhiteSpace(formatted))
                sb.AppendLine(formatted);
        }

        if (_fastContext is not null && IsFastContextStage(stage))
        {
            var remaining = Math.Max(0, maxChars - sb.Length - 32);
            if (remaining > 256)
            {
                var fastSection = await BuildFastContextSectionAsync(orchestrator, lastErrors, remaining, ct)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(fastSection))
                    sb.AppendLine(fastSection);
            }
        }

        var s = sb.ToString();
        return s.Length <= maxChars ? s : s[..maxChars] + "\n…";
    }

    private static IReadOnlyList<string>? ResolveFocusPaths(string stage, AppGenerationOrchestrator orchestrator)
    {
        if (!stage.Contains("fix", StringComparison.OrdinalIgnoreCase)
            && !stage.Contains("repair", StringComparison.OrdinalIgnoreCase)
            && !stage.Contains("verify", StringComparison.OrdinalIgnoreCase))
            return null;

        var paths = orchestrator.Iterations
            .SelectMany(i => i.Errors)
            .Select(e => e.FilePath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return paths.Count == 0 ? null : paths;
    }

    private static string Truncate(string? text, int n)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length <= n ? text : text[..n] + "…";
    }

    private int GetStageBudget(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return _options.DefaultMaxChars;

        if (stage.Contains("plan", StringComparison.OrdinalIgnoreCase))
            return _options.PlanningMaxChars;
        if (stage.Contains("generation", StringComparison.OrdinalIgnoreCase))
            return _options.GenerationMaxChars;
        if (stage.Contains("consistency", StringComparison.OrdinalIgnoreCase))
            return _options.ConsistencyMaxChars;
        if (stage.Contains("fix", StringComparison.OrdinalIgnoreCase)
            || stage.Contains("repair", StringComparison.OrdinalIgnoreCase))
            return _options.FixingMaxChars;
        if (stage.Contains("verify", StringComparison.OrdinalIgnoreCase))
            return _options.VerifyMaxChars;
        if (stage.Contains("build", StringComparison.OrdinalIgnoreCase))
            return _options.BuildMaxChars;
        if (stage.Contains("execution", StringComparison.OrdinalIgnoreCase))
            return _options.ExecutionMaxChars;

        return _options.DefaultMaxChars;
    }

    private static bool IsLspStage(string stage) =>
        stage.Contains("fix", StringComparison.OrdinalIgnoreCase)
        || stage.Contains("repair", StringComparison.OrdinalIgnoreCase)
        || stage.Contains("verify", StringComparison.OrdinalIgnoreCase);

    private static bool IsFastContextStage(string stage) =>
        IsLspStage(stage) || stage.Contains("generation", StringComparison.OrdinalIgnoreCase);

    private async Task<string?> BuildFastContextSectionAsync(
        AppGenerationOrchestrator orchestrator,
        IReadOnlyList<ErrorReport>? lastErrors,
        int maxChars,
        CancellationToken ct)
    {
        string? workspaceRoot = null;
        if (orchestrator.ShadowWorkspaceId is Guid wsId
            && _workspaceAccessor?.TryGetWorkspace(wsId, out var wsContext) == true)
            workspaceRoot = wsContext.HostPath;

        var errors = lastErrors ?? Array.Empty<ErrorReport>();
        var prefetch = await _fastContext!.PrefetchForRepairAsync(
                new FastContextPrefetchRequest(
                    workspaceRoot,
                    BuildLog: null,
                    errors,
                    orchestrator.Files,
                    orchestrator.UserRequest,
                    orchestrator.Id),
                ct)
            .ConfigureAwait(false);

        if (!prefetch.MeetsContextPackThreshold(_fastContextOptions.MinConfidenceForContextPack))
            return null;

        var body = prefetch.FormattedText;
        if (body.Length > maxChars - 32)
            body = body[..Math.Max(0, maxChars - 40)] + "\n…";

        return $"## fast_context (confidence={prefetch.Confidence:F2})\n{body}";
    }
}
