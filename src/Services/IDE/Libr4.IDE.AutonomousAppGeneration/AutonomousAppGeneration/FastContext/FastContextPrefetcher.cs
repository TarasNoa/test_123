using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class FastContextPrefetcher : IFastContextPrefetcher
{
    private static readonly Regex SymbolRegex = new(
        @"(?:cannot find symbol|CS\d{4}|error TS\d+|undefined reference to|undefined symbol)\s*:?\s*['""]?([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex PascalTokenRegex = new(
        @"\b[A-Z][A-Za-z0-9_]{2,}\b",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private readonly ICodebaseIndex _index;
    private readonly FastContextOptions _options;
    private readonly ILogger<FastContextPrefetcher> _logger;

    public FastContextPrefetcher(
        ICodebaseIndex index,
        IOptions<FastContextOptions> options,
        ILogger<FastContextPrefetcher> logger)
    {
        _index = index;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FastContextPrefetchResult> PrefetchForRepairAsync(
        FastContextPrefetchRequest request,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return Empty(Array.Empty<string>());

        var start = FastContextTelemetry.StartTiming();
        var queries = BuildQueries(request);
        if (queries.Count == 0)
            return Empty(queries);

        var hits = new List<CodebaseSearchHit>();
        var searchOptions = new CodebaseSearchOptions(
            Limit: _options.MaxPrefetchHits,
            IncludeTests: false);

        if (!string.IsNullOrWhiteSpace(request.WorkspaceRoot) && Directory.Exists(request.WorkspaceRoot))
        {
            if (request.RunId is Guid runId)
                await _index.IndexAsync(request.WorkspaceRoot, runId, ct).ConfigureAwait(false);

            foreach (var query in queries)
            {
                var batch = await _index.SearchAsync(request.WorkspaceRoot, query, searchOptions, ct).ConfigureAwait(false);
                hits.AddRange(batch);
            }
        }
        else if (request.MemoryFiles is { Count: > 0 })
        {
            foreach (var query in queries)
                hits.AddRange(SearchInMemory(request.MemoryFiles, query, searchOptions.Limit));
        }

        var deduped = DeduplicateHits(hits).Take(_options.MaxPrefetchHits).ToList();
        var confidence = deduped.Count == 0 ? 0.0 : deduped.Max(h => h.Score);
        var formatted = FormatHits(deduped, queries);

        _logger.LogDebug(
            "Fast context prefetch: queries={Queries} hits={Hits} confidence={Confidence:F2}",
            string.Join(", ", queries),
            deduped.Count,
            confidence);

        FastContextTelemetry.RecordQuery("prefetch", FastContextTelemetry.ElapsedMs(start), deduped.Count > 0);

        return new FastContextPrefetchResult(deduped, confidence, queries, formatted);
    }

    public Task WarmIndexAsync(string workspaceRoot, Guid? runId = null, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return Task.CompletedTask;

        return _index.IndexAsync(workspaceRoot, runId, ct);
    }

    public static IReadOnlyList<string> BuildQueries(FastContextPrefetchRequest request)
    {
        var queries = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return;
            q = q.Trim();
            if (q.Length < 2 || q.Length > 80)
                return;
            if (seen.Add(q))
                queries.Add(q);
        }

        foreach (var error in request.Errors.Take(4))
        {
            if (!string.IsNullOrWhiteSpace(error.FilePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(error.FilePath.Replace('\\', '/'));
                Add(fileName);
            }

            foreach (Match m in SymbolRegex.Matches(error.Message))
                Add(m.Groups[1].Value);

            foreach (Match m in PascalTokenRegex.Matches(error.Message).Take(2))
                Add(m.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.BuildLog))
        {
            foreach (var line in request.BuildLog.Split('\n').Reverse().Take(40))
            {
                if (!line.Contains("error", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (Match m in SymbolRegex.Matches(line))
                    Add(m.Groups[1].Value);
                if (queries.Count >= 3)
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.UserRequest))
        {
            foreach (var token in request.UserRequest.Split([' ', ',', '.', ';', ':', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length is >= 4 and <= 40 && char.IsLetter(token[0]))
                    Add(token);
                if (queries.Count >= 5)
                    break;
            }
        }

        return queries.Take(3).ToList();
    }

    private static IReadOnlyList<CodebaseSearchHit> SearchInMemory(
        IReadOnlyList<GeneratedFile> files,
        string query,
        int limit)
    {
        var pattern = Regex.Escape(query);
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
        var hits = new List<CodebaseSearchHit>();

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.Content))
                continue;

            var lines = file.Content.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (!regex.IsMatch(lines[i]))
                    continue;

                var start = Math.Max(0, i - 2);
                var end = Math.Min(lines.Length - 1, i + 8);
                hits.Add(new CodebaseSearchHit(
                    file.RelativePath,
                    start + 1,
                    end + 1,
                    0.75,
                    string.Join('\n', lines[start..(end + 1)]),
                    "memory"));
                break;
            }

            if (hits.Count >= limit)
                break;
        }

        return hits;
    }

    private static List<CodebaseSearchHit> DeduplicateHits(IEnumerable<CodebaseSearchHit> hits) =>
        hits
            .GroupBy(h => $"{h.Path}:{h.StartLine}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(h => h.Score)
            .ToList();

    private string FormatHits(IReadOnlyList<CodebaseSearchHit> hits, IReadOnlyList<string> queries)
    {
        if (hits.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"queries: {string.Join(", ", queries)}");
        foreach (var hit in hits)
        {
            sb.AppendLine($"- {hit.Path}:{hit.StartLine}-{hit.EndLine} score={hit.Score:F2} ({hit.MatchKind})");
            sb.AppendLine(Truncate(hit.Snippet, _options.MaxSnippetChars));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n...[truncated]";

    private static FastContextPrefetchResult Empty(IReadOnlyList<string> queries) =>
        new(Array.Empty<CodebaseSearchHit>(), 0, queries, string.Empty);
}
