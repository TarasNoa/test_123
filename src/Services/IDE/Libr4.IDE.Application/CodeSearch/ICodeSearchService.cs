namespace Libr4.IDE.Application.CodeSearch;

/// <summary>
/// Interface for code search service
/// </summary>
public interface ICodeSearchService
{
    Task<SearchResult[]> SearchAsync(string query, string[]? filePatterns = null, CancellationToken ct = default);
    Task<SymbolDefinition[]> FindSymbolsAsync(string symbolName, CancellationToken ct = default);
    Task<string[]> GetFileReferencesAsync(string filePath, CancellationToken ct = default);
}

public class SearchResult
{
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string LineContent { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public class SymbolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}
