namespace Libr4.IDE.Domain.CodeSearch;

/// <summary>
/// Semantic code index for hybrid search (BM25 + dense vector)
/// Based on claude-context architecture
/// </summary>
public class SemanticCodeIndex
{
    public Guid Id { get; private set; }
    public string ProjectPath { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastIndexedAt { get; private set; }
    public int TotalFiles { get; private set; }
    public int TotalChunks { get; private set; }
    public IndexStatus Status { get; private set; }
    public double ProgressPercentage { get; private set; }
    public string? CurrentPhase { get; private set; }
    
    /// <summary>
    /// Merkle tree root hash for incremental indexing (from claude-context)
    /// </summary>
    public string? MerkleRootHash { get; private set; }
    
    /// <summary>
    /// Embedding model used for this index
    /// </summary>
    public string EmbeddingModel { get; private set; }
    
    /// <summary>
    /// Chunking strategy used
    /// </summary>
    public ChunkingStrategy ChunkingStrategy { get; private set; }

    private SemanticCodeIndex() { }
    
    public SemanticCodeIndex(
        string projectPath,
        string embeddingModel = "text-embedding-3-small",
        ChunkingStrategy chunkingStrategy = ChunkingStrategy.AST)
    {
        Id = Guid.NewGuid();
        ProjectPath = projectPath;
        CreatedAt = DateTime.UtcNow;
        LastIndexedAt = DateTime.UtcNow;
        TotalFiles = 0;
        TotalChunks = 0;
        Status = IndexStatus.NotStarted;
        ProgressPercentage = 0.0;
        EmbeddingModel = embeddingModel;
        ChunkingStrategy = chunkingStrategy;
    }
    
    public void UpdateProgress(string phase, double percentage, int filesIndexed, int chunksCreated)
    {
        CurrentPhase = phase;
        ProgressPercentage = Math.Clamp(percentage, 0.0, 100.0);
        TotalFiles = filesIndexed;
        TotalChunks = chunksCreated;
        LastIndexedAt = DateTime.UtcNow;
    }
    
    public void SetStatus(IndexStatus status)
    {
        Status = status;
        LastIndexedAt = DateTime.UtcNow;
    }
    
    public void SetMerkleRoot(string merkleRoot)
    {
        MerkleRootHash = merkleRoot;
    }
}

/// <summary>
/// Index status
/// </summary>
public enum IndexStatus
{
    NotStarted,
    Indexing,
    Completed,
    Failed,
    Partial
}

/// <summary>
/// Chunking strategy for code (from claude-context)
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// AST-based chunking (recommended for code)
    /// </summary>
    AST,
    
    /// <summary>
    /// Character-based chunking (fallback)
    /// </summary>
    Character,
    
    /// <summary>
    /// Function-based chunking
    /// </summary>
    Function
}
