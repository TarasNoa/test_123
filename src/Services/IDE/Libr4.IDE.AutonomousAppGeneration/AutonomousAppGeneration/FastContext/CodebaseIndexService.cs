using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class CodebaseIndexService : ICodebaseIndex
{
    private readonly RipgrepCodeIndex _ripgrep;
    private readonly EmbeddingCodeIndex? _embedding;
    private readonly RepoGraphRanker _graphRanker;
    private readonly FastContextFusionRanker _fusion;
    private readonly FastContextOptions _options;
    private readonly ILogger<CodebaseIndexService> _logger;
    private readonly object _lock = new();
    private readonly Dictionary<string, CodebaseIndexManifest> _cache = new(StringComparer.OrdinalIgnoreCase);

    public CodebaseIndexService(
        RipgrepCodeIndex ripgrep,
        RepoGraphRanker graphRanker,
        FastContextFusionRanker fusion,
        IOptions<FastContextOptions> options,
        ILogger<CodebaseIndexService> logger,
        EmbeddingCodeIndex? embedding = null)
    {
        _ripgrep = ripgrep;
        _embedding = embedding;
        _graphRanker = graphRanker;
        _fusion = fusion;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IndexAsync(string workspaceRoot, Guid? runId = null, CancellationToken ct = default)
    {
        var start = FastContextTelemetry.StartTiming();
        var root = Path.GetFullPath(workspaceRoot);
        bool cacheHit;
        lock (_lock)
            cacheHit = _cache.ContainsKey(root);

        var manifest = await _ripgrep.BuildManifestAsync(root, ct).ConfigureAwait(false);
        lock (_lock)
            _cache[root] = manifest;

        if (runId is Guid rid)
            await WriteManifestAsync(rid, manifest, ct).ConfigureAwait(false);

        if (_embedding is not null)
            await _embedding.IndexAsync(root, ct).ConfigureAwait(false);

        FastContextTelemetry.RecordQuery("index", FastContextTelemetry.ElapsedMs(start), cacheHit);
        _logger.LogDebug("Indexed {FileCount} files in {Root}", manifest.FileCount, root);
    }

    public async Task<IReadOnlyList<CodebaseSearchHit>> SearchAsync(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions? options = null,
        CancellationToken ct = default)
    {
        var start = FastContextTelemetry.StartTiming();
        options ??= new CodebaseSearchOptions();
        var root = Path.GetFullPath(workspaceRoot);

        bool cacheHit;
        lock (_lock)
            cacheHit = _cache.ContainsKey(root);

        if (!cacheHit)
            _logger.LogDebug("Cold index for {Root}", root);

        var raw = await _ripgrep.SearchAsync(root, query, options, ct).ConfigureAwait(false);
        if (_embedding is not null)
        {
            var semantic = await _embedding.SearchAsync(root, query, options, ct).ConfigureAwait(false);
            raw = MergeHits(raw, semantic);
        }

        var enriched = EnrichSnippets(root, raw);
        var boosts = _graphRanker.BoostNeighbors(root, enriched);
        var fused = _fusion.Fuse(enriched, boosts, options.Limit);
        FastContextTelemetry.RecordQuery("search", FastContextTelemetry.ElapsedMs(start), cacheHit);
        return fused;
    }

    public Task<CodebaseSymbolContext?> GetSymbolAsync(
        string workspaceRoot,
        string symbol,
        string? pathHint = null,
        CancellationToken ct = default)
    {
        var root = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(root))
            return Task.FromResult<CodebaseSymbolContext?>(null);

        var candidates = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(p => !ShouldSkip(p))
            .Where(p => string.IsNullOrWhiteSpace(pathHint)
                        || p.Contains(pathHint, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(symbol, StringComparison.Ordinal))
                    continue;

                var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
                var start = Math.Max(0, i - 2);
                var end = Math.Min(lines.Length - 1, i + 8);
                var snippet = string.Join('\n', lines[start..(end + 1)]);
                return Task.FromResult<CodebaseSymbolContext?>(new CodebaseSymbolContext(
                    symbol,
                    rel,
                    i + 1,
                    end + 1,
                    Truncate(snippet, _options.MaxSnippetChars),
                    Array.Empty<string>()));
            }
        }

        return Task.FromResult<CodebaseSymbolContext?>(null);
    }

    public async Task InvalidateAsync(string workspaceRoot, CancellationToken ct = default)
    {
        var root = Path.GetFullPath(workspaceRoot);
        lock (_lock)
            _cache.Remove(root);

        if (_embedding is not null)
            await _embedding.InvalidateAsync(root, ct).ConfigureAwait(false);
    }

    private static List<CodebaseSearchHit> MergeHits(
        IReadOnlyList<CodebaseSearchHit> primary,
        IReadOnlyList<CodebaseSearchHit> secondary)
    {
        var merged = new List<CodebaseSearchHit>(primary.Count + secondary.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hit in primary.Concat(secondary))
        {
            var key = $"{hit.Path}:{hit.StartLine}:{hit.EndLine}";
            if (!seen.Add(key))
                continue;
            merged.Add(hit);
        }

        return merged;
    }

    private static List<CodebaseSearchHit> EnrichSnippets(string workspaceRoot, IReadOnlyList<CodebaseSearchHit> hits)
    {
        var result = new List<CodebaseSearchHit>(hits.Count);
        foreach (var hit in hits)
        {
            var abs = Path.Combine(workspaceRoot, hit.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(abs))
            {
                result.Add(hit);
                continue;
            }

            var lines = File.ReadAllLines(abs);
            var start = Math.Max(0, hit.StartLine - 3);
            var end = Math.Min(lines.Length - 1, hit.StartLine + 8);
            var snippet = string.Join('\n', lines[start..(end + 1)]);
            result.Add(hit with
            {
                StartLine = start + 1,
                EndLine = end + 1,
                Snippet = snippet
            });
        }

        return result;
    }

    private async Task WriteManifestAsync(Guid runId, CodebaseIndexManifest manifest, CancellationToken ct)
    {
        var dir = Path.Combine(_options.RunsRoot, runId.ToString("D"), "context-index");
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(dir, "manifest.json"), json, ct).ConfigureAwait(false);
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n...[truncated]";

    private static bool ShouldSkip(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.git/", StringComparison.OrdinalIgnoreCase);
    }
}
