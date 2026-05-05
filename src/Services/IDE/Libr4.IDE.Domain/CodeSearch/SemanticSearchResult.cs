namespace Libr4.IDE.Domain.CodeSearch;

/// <summary>
/// Semantic search result with hybrid scoring (BM25 + dense vector)
/// Based on claude-context architecture
/// </summary>
public class SemanticSearchResult
{
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Combined hybrid score (BM25 + vector similarity)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// BM25 score (keyword matching)
    /// </summary>
    public double BM25Score { get; set; }
    
    /// <summary>
    /// Vector similarity score (semantic)
    /// </summary>
    public double VectorScore { get; set; }
    
    /// <summary>
    /// Language of the code
    /// </summary>
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// Function name if this is a function chunk
    /// </summary>
    public string? FunctionName { get; set; }
    
    /// <summary>
    /// Class name if this is a class method
    /// </summary>
    public string? ClassName { get; set; }
    
    /// <summary>
    /// Chunk ID for reference
    /// </summary>
    public string ChunkId { get; set; } = string.Empty;
    
    /// <summary>
    /// Highlighted snippet of the result
    /// </summary>
    public string? HighlightedSnippet { get; set; }
}

/// <summary>
/// Search options for semantic code search
/// </summary>
public class SemanticSearchOptions
{
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int TopK { get; set; } = 5;
    
    /// <summary>
    /// Minimum score threshold
    /// </summary>
    public double MinScore { get; set; } = 0.0;
    
    /// <summary>
    /// Weight for BM25 score in hybrid search (0-1)
    /// </summary>
    public double BM25Weight { get; set; } = 0.5;
    
    /// <summary>
    /// Weight for vector score in hybrid search (0-1)
    /// </summary>
    public double VectorWeight { get; set; } = 0.5;
    
    /// <summary>
    /// Filter by file extension
    /// </summary>
    public string? FileExtension { get; set; }
    
    /// <summary>
    /// Filter by language
    /// </summary>
    public string? Language { get; set; }
    
    /// <summary>
    /// Whether to include highlighted snippets
    /// </summary>
    public bool IncludeHighlights { get; set; } = true;
    
    public SemanticSearchOptions()
    {
        // Default balanced hybrid search
        BM25Weight = 0.5;
        VectorWeight = 0.5;
    }
    
    public void SetHybridWeights(double bm25Weight, double vectorWeight)
    {
        BM25Weight = Math.Clamp(bm25Weight, 0.0, 1.0);
        VectorWeight = Math.Clamp(vectorWeight, 0.0, 1.0);
        
        // Normalize to sum to 1
        var total = BM25Weight + VectorWeight;
        if (total > 0)
        {
            BM25Weight /= total;
            VectorWeight /= total;
        }
    }
}
