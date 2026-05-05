using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class ContextPackBuilder : IContextPackBuilder
{
    private readonly IMemoryStore _memory;
    private readonly ContextPackOptions _options;

    public ContextPackBuilder(IMemoryStore memory, IOptions<ContextPackOptions> options)
    {
        _memory = memory;
        _options = options.Value;
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

        var s = sb.ToString();
        return s.Length <= maxChars ? s : s[..maxChars] + "\n…";
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
        if (stage.Contains("fix", StringComparison.OrdinalIgnoreCase))
            return _options.FixingMaxChars;
        if (stage.Contains("build", StringComparison.OrdinalIgnoreCase))
            return _options.BuildMaxChars;
        if (stage.Contains("execution", StringComparison.OrdinalIgnoreCase))
            return _options.ExecutionMaxChars;

        return _options.DefaultMaxChars;
    }
}
