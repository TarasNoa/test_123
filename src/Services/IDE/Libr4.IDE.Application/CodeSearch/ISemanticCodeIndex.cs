namespace Libr4.IDE.Application.CodeSearch;

/// <summary>
/// Semantic code index — full SocratiCode-equivalent interface for our agents.
/// Provides hybrid search (dense embedding + BM25 keyword), symbol graph,
/// impact analysis, and context artifact management.
/// </summary>
public interface ISemanticCodeIndex
{
    // ── Indexing ────────────────────────────────────────────────────────────

    /// <summary>Start or resume indexing a project path in the background.</summary>
    Task<IndexingHandle> StartIndexingAsync(string projectPath, IndexingOptions? options = null, CancellationToken ct = default);

    /// <summary>Incrementally re-index only files changed since last index.</summary>
    Task<IndexingHandle> UpdateIndexAsync(string projectPath, CancellationToken ct = default);

    /// <summary>Get current indexing status and progress for a project.</summary>
    Task<IndexStatus> GetStatusAsync(string projectPath, CancellationToken ct = default);

    /// <summary>Remove all indexed data for a project.</summary>
    Task RemoveIndexAsync(string projectPath, CancellationToken ct = default);

    // ── Search ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Hybrid semantic + BM25 search with Reciprocal Rank Fusion.
    /// Returns ranked chunks from indexed files.
    /// </summary>
    Task<CodeSearchResult[]> SearchAsync(
        string projectPath,
        string query,
        CodeSearchOptions? options = null,
        CancellationToken ct = default);

    // ── Symbol Graph ────────────────────────────────────────────────────────

    /// <summary>Build or rebuild the polyglot dependency graph for a project.</summary>
    Task BuildGraphAsync(string projectPath, CancellationToken ct = default);

    /// <summary>Get imports and dependents for a specific file.</summary>
    Task<FileGraphInfo> QueryFileGraphAsync(string projectPath, string filePath, CancellationToken ct = default);

    /// <summary>Graph-level statistics: most-connected files, orphans, circular deps.</summary>
    Task<GraphStatisticsDto> GetGraphStatsAsync(string projectPath, CancellationToken ct = default);

    /// <summary>Detect circular import dependencies.</summary>
    Task<CircularDependencyResult[]> FindCircularDependenciesAsync(string projectPath, CancellationToken ct = default);

    // ── Impact Analysis ─────────────────────────────────────────────────────

    /// <summary>
    /// Blast radius — what breaks if symbol/file X changes.
    /// BFS through reverse-call edges.
    /// </summary>
    Task<ImpactResult> GetBlastRadiusAsync(
        string projectPath,
        string symbolOrFilePath,
        int maxDepth = 5,
        CancellationToken ct = default);

    /// <summary>Trace forward execution flow from an entry point symbol.</summary>
    Task<FlowResult> TraceExecutionFlowAsync(
        string projectPath,
        string entrySymbolId,
        int maxDepth = 4,
        CancellationToken ct = default);

    /// <summary>360° view of one symbol: definition, callers, callees.</summary>
    Task<SymbolView> GetSymbolViewAsync(string projectPath, string symbolId, CancellationToken ct = default);

    /// <summary>List all symbols in a file or search by name prefix.</summary>
    Task<SymbolDefinitionDto[]> ListSymbolsAsync(
        string projectPath,
        string? filePathFilter = null,
        string? namePrefix = null,
        CancellationToken ct = default);
}

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record IndexingOptions(
    bool WatchForChanges = true,
    bool BuildGraphAfterIndex = true,
    bool RespectGitignore = true,
    string[] ExtraExtensions = default!,
    int BatchSize = 50);

public sealed record IndexingHandle(string ProjectPath, Guid OperationId, DateTimeOffset StartedAt);

public sealed record IndexStatus(
    string ProjectPath,
    IndexState State,
    int TotalFiles,
    int IndexedFiles,
    int ChunkCount,
    int SymbolCount,
    DateTimeOffset? LastIndexedAt,
    string? ErrorMessage);

public enum IndexState { NotIndexed, Indexing, Ready, Updating, Error }

public sealed record CodeSearchOptions(
    int TopK = 10,
    double MinScore = 0.1,
    string[]? FilePatterns = null,
    string[]? Languages = null,
    bool IncludeLinkedProjects = false);

public sealed record CodeSearchResult(
    string FilePath,
    int StartLine,
    int EndLine,
    string Content,
    double SemanticScore,
    double KeywordScore,
    double FusedScore,
    string Language,
    string? SymbolName,
    string? SymbolKind);

public sealed record FileGraphInfo(
    string FilePath,
    string[] Imports,
    string[] ImportedBy,
    SymbolDefinitionDto[] Symbols);

public sealed record GraphStatisticsDto(
    int TotalFiles,
    int TotalSymbols,
    int TotalEdges,
    int TotalImports,
    Dictionary<string, int> LanguageBreakdown,
    (string File, int Connections)[] MostConnectedFiles,
    string[] OrphanFiles,
    double UnresolvedEdgePct);

public sealed record CircularDependencyResult(string[] CycleFiles);

public sealed record ImpactNode(
    string SymbolId,
    string FilePath,
    string SymbolName,
    int Distance,
    string EdgeKind);

public sealed record ImpactResult(
    string RootSymbolId,
    ImpactNode[] AffectedSymbols,
    string[] AffectedFiles,
    int TotalImpact);

public sealed record FlowResult(
    string EntrySymbolId,
    ImpactNode[] FlowNodes,
    string[] EntryPoints);

public sealed record SymbolView(
    SymbolDefinitionDto Symbol,
    SymbolDefinitionDto[] Callers,
    SymbolDefinitionDto[] Callees,
    string[] FilesImportingThisFile);

public sealed record SymbolDefinitionDto(
    string Id,
    string Name,
    string FullyQualifiedName,
    string Kind,
    string Language,
    string FilePath,
    int StartLine,
    int EndLine,
    string? DocComment);
