using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.CodeSearch;

/// <summary>
/// Code search service: delegates to SemanticCodeIndex when available,
/// falls back to filesystem text search.
/// </summary>
public class CodeSearchService : ICodeSearchService
{
    private readonly ILogger<CodeSearchService> _logger;
    private readonly ISemanticCodeIndex? _semanticIndex;

    public CodeSearchService(
        ILogger<CodeSearchService> logger,
        ISemanticCodeIndex? semanticIndex = null)
    {
        _logger = logger;
        _semanticIndex = semanticIndex;
    }

    public async Task<SearchResult[]> SearchAsync(string query, string[]? filePatterns = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for: {Query}", query);

        if (_semanticIndex != null)
        {
            try
            {
                var opts = new CodeSearchOptions(
                    TopK: 20,
                    MinScore: 0.1,
                    FilePatterns: filePatterns);
                var results = await _semanticIndex.SearchAsync(string.Empty, query, opts, ct);
                return results.Select(r => new SearchResult
                {
                    FilePath = r.FilePath,
                    LineNumber = r.StartLine,
                    LineContent = r.Content.Split('\n').FirstOrDefault() ?? string.Empty,
                    RelevanceScore = r.FusedScore
                }).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SemanticCodeIndex search failed, falling back to filesystem search");
            }
        }

        return await FilesystemSearchAsync(query, filePatterns, ct);
    }

    public async Task<SymbolDefinition[]> FindSymbolsAsync(string symbolName, CancellationToken ct = default)
    {
        _logger.LogInformation("Finding symbols: {SymbolName}", symbolName);

        if (_semanticIndex != null)
        {
            try
            {
                var results = await _semanticIndex.ListSymbolsAsync(
                    string.Empty, namePrefix: symbolName, ct: ct);
                return results.Select(r => new SymbolDefinition
                {
                    Name = r.Name,
                    Kind = r.Kind,
                    FilePath = r.FilePath,
                    LineNumber = r.StartLine
                }).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SemanticCodeIndex symbol search failed");
            }
        }

        return Array.Empty<SymbolDefinition>();
    }

    public async Task<string[]> GetFileReferencesAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting references for: {FilePath}", filePath);

        if (_semanticIndex != null)
        {
            try
            {
                var impact = await _semanticIndex.GetBlastRadiusAsync(
                    string.Empty, filePath, ct: ct);
                return impact.AffectedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SemanticCodeIndex impact analysis failed");
            }
        }

        return Array.Empty<string>();
    }

    private static async Task<SearchResult[]> FilesystemSearchAsync(
        string query, string[]? filePatterns, CancellationToken ct)
    {
        var extensions = filePatterns?.Length > 0
            ? filePatterns
            : new[] { "*.cs", "*.ts", "*.js", "*.py", "*.fs", "*.rs" };

        var results = new List<SearchResult>();
        var roots = new[] { Directory.GetCurrentDirectory() };

        foreach (var root in roots)
        {
            foreach (var pattern in extensions)
            {
                if (ct.IsCancellationRequested) break;
                foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        var lines = await File.ReadAllLinesAsync(file, ct);
                        for (var i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                            {
                                results.Add(new SearchResult
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    LineContent = lines[i].Trim(),
                                    RelevanceScore = 0.5
                                });
                                if (results.Count >= 50) return results.ToArray();
                            }
                        }
                    }
                    catch { /* skip unreadable files */ }
                }
            }
        }

        return results.ToArray();
    }
}
