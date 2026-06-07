using System.Security.Cryptography;
using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class EmbeddingCodeIndex
{
    private const int DefaultMinChunkLines = 40;
    private const int DefaultMaxChunkLines = 80;

    private readonly IEmbeddingService _embeddings;
    private readonly IVectorMemoryStore _vectorStore;
    private readonly FastContextOptions _options;
    private readonly ILogger<EmbeddingCodeIndex> _logger;

    public EmbeddingCodeIndex(
        IEmbeddingService embeddings,
        IVectorMemoryStore vectorStore,
        IOptions<FastContextOptions> options,
        ILogger<EmbeddingCodeIndex> logger)
    {
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IndexAsync(string workspaceRoot, CancellationToken ct = default)
    {
        if (!_options.EnableEmbeddingIndex)
            return;

        var root = Path.GetFullPath(workspaceRoot);
        var collectionId = CollectionId(root);
        await _vectorStore.DeleteCollectionAsync(collectionId, ct).ConfigureAwait(false);

        var chunks = new List<(string Id, string Path, int StartLine, int EndLine, string Text)>();
        foreach (var abs in EnumerateSourceFiles(root))
        {
            ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(root, abs).Replace('\\', '/');
            foreach (var chunk in ChunkFile(abs, rel))
                chunks.Add(chunk);
        }

        if (chunks.Count == 0)
            return;

        var texts = chunks.Select(c => c.Text).ToList();
        var vectors = await _embeddings.EmbedBatchAsync(texts, ct).ConfigureAwait(false);
        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            await _vectorStore.UpsertAsync(new VectorRecord(
                chunk.Id,
                collectionId,
                vectors[i],
                chunk.Text,
                new Dictionary<string, string>
                {
                    ["path"] = chunk.Path,
                    ["startLine"] = chunk.StartLine.ToString(),
                    ["endLine"] = chunk.EndLine.ToString(),
                }), ct).ConfigureAwait(false);
        }

        _logger.LogDebug("Embedded {ChunkCount} chunks for {Root}", chunks.Count, root);
    }

    public async Task<IReadOnlyList<CodebaseSearchHit>> SearchAsync(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions options,
        CancellationToken ct = default)
    {
        if (!_options.EnableEmbeddingIndex)
            return Array.Empty<CodebaseSearchHit>();

        var root = Path.GetFullPath(workspaceRoot);
        var embedding = await _embeddings.EmbedAsync(query, ct).ConfigureAwait(false);
        var results = await _vectorStore.SearchAsync(
            embedding,
            CollectionId(root),
            topK: Math.Min(options.Limit * 2, 24),
            minScore: _options.EmbeddingMinScore,
            ct: ct).ConfigureAwait(false);

        var hits = new List<CodebaseSearchHit>();
        foreach (var result in results)
        {
            var path = result.Record.Metadata?.TryGetValue("path", out var p) == true ? p : "unknown";
            if (!PassesFilters(path, options))
                continue;

            var startLine = result.Record.Metadata?.TryGetValue("startLine", out var s) == true
                            && int.TryParse(s, out var sl)
                ? sl
                : 1;
            var endLine = result.Record.Metadata?.TryGetValue("endLine", out var e) == true
                          && int.TryParse(e, out var el)
                ? el
                : startLine;

            hits.Add(new CodebaseSearchHit(
                path,
                startLine,
                endLine,
                result.Score,
                Truncate(result.Record.Text, _options.MaxSnippetChars),
                "embedding"));
        }

        return hits;
    }

    public Task InvalidateAsync(string workspaceRoot, CancellationToken ct = default)
    {
        if (!_options.EnableEmbeddingIndex)
            return Task.CompletedTask;

        return _vectorStore.DeleteCollectionAsync(CollectionId(Path.GetFullPath(workspaceRoot)), ct);
    }

    private IEnumerable<(string Id, string Path, int StartLine, int EndLine, string Text)> ChunkFile(
        string absolutePath,
        string relativePath)
    {
        var lines = File.ReadAllLines(absolutePath);
        if (lines.Length == 0)
            yield break;

        var minLines = Math.Max(1, _options.EmbeddingMinChunkLines > 0 ? _options.EmbeddingMinChunkLines : DefaultMinChunkLines);
        var maxLines = Math.Max(minLines, _options.EmbeddingMaxChunkLines > 0 ? _options.EmbeddingMaxChunkLines : DefaultMaxChunkLines);
        var stride = minLines;

        for (var start = 0; start < lines.Length; start += stride)
        {
            var end = Math.Min(lines.Length, start + maxLines);
            var chunkLines = lines[start..end];
            if (chunkLines.Length < Math.Min(minLines, lines.Length) && start > 0)
                continue;

            var startLine = start + 1;
            var endLine = end;
            var text = string.Join('\n', chunkLines);
            var id = $"{relativePath}:{startLine}:{endLine}";
            yield return (id, relativePath, startLine, endLine, text);
            if (end >= lines.Length)
                break;
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string workspaceRoot)
    {
        if (!Directory.Exists(workspaceRoot))
            yield break;

        foreach (var file in Directory.EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkip(file))
                continue;
            yield return file;
        }
    }

    private static bool PassesFilters(string relativePath, CodebaseSearchOptions options)
    {
        if (!options.IncludeTests && IsTestPath(relativePath))
            return false;

        if (options.Languages is { Count: > 0 })
        {
            var ext = Path.GetExtension(relativePath).TrimStart('.').ToLowerInvariant();
            if (!options.Languages.Any(l => ext.Equals(l.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    private static bool IsTestPath(string path) =>
        path.Contains("/test/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("_test.py", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldSkip(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/dist/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.venv/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string CollectionId(string workspaceRoot) =>
        $"libr4_codebase_{HashWorkspace(workspaceRoot)}";

    private static string HashWorkspace(string workspaceRoot) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(workspaceRoot))))
            .ToLowerInvariant()[..16];

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n...[truncated]";
}
