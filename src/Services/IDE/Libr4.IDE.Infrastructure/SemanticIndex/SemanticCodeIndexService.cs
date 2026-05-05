using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.CodeSearch;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// Full SocratiCode-equivalent semantic code index for Libr4 agents.
/// Features:
///  - AST-aware chunking (symbol boundaries)
///  - Dense embedding via configured AI provider
///  - BM25 keyword scoring
///  - Reciprocal Rank Fusion of dense + BM25
///  - Qdrant (or in-process fallback) vector store
///  - Polyglot symbol graph with impact analysis
///  - Incremental indexing via content hashes
///  - File watcher for live updates
/// </summary>
public sealed class SemanticCodeIndexService : ISemanticCodeIndex, IAsyncDisposable
{
    private readonly IVectorMemoryStore _vectorStore;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<SemanticCodeIndexService> _logger;
    private readonly SemanticIndexOptions _options;

    private readonly ConcurrentDictionary<string, ProjectIndexState> _projects = new();
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, ProjectSymbolGraph> _graphs = new();
    private readonly ConcurrentDictionary<string, string> _fileHashes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] SupportedExtensions =
        [".cs", ".fs", ".fsi", ".fsx", ".rs", ".ts", ".tsx", ".js", ".jsx",
         ".py", ".go", ".java", ".kt", ".md", ".json", ".yaml", ".yml",
         ".toml", ".xml", ".sql", ".sh", ".bash", ".ps1"];

    private static readonly string[] DefaultIgnorePatterns =
        ["bin", "obj", ".git", "node_modules", "dist", "build", ".vs", ".idea",
         "packages", "publish", "__pycache__", ".pytest_cache", "coverage",
         "*.min.js", "*.map", "package-lock.json", "yarn.lock"];

    public SemanticCodeIndexService(
        IVectorMemoryStore vectorStore,
        IEmbeddingService embeddings,
        IOptions<SemanticIndexOptions> options,
        ILogger<SemanticCodeIndexService> logger)
    {
        _vectorStore = vectorStore;
        _embeddings = embeddings;
        _logger = logger;
        _options = options.Value;
    }

    // ── Indexing ─────────────────────────────────────────────────────────────

    public async Task<IndexingHandle> StartIndexingAsync(
        string projectPath,
        IndexingOptions? options = null,
        CancellationToken ct = default)
    {
        var opts = options ?? new IndexingOptions();
        var handle = new IndexingHandle(projectPath, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var state = _projects.GetOrAdd(projectPath, _ => new ProjectIndexState(projectPath));
        state.State = IndexState.Indexing;
        state.OperationId = handle.OperationId;

        _ = Task.Run(() => RunFullIndexAsync(projectPath, opts, state, ct), ct);

        _logger.LogInformation("[SemanticIndex] Started indexing {Path} op={Op}", projectPath, handle.OperationId);
        return handle;
    }

    public async Task<IndexingHandle> UpdateIndexAsync(string projectPath, CancellationToken ct = default)
    {
        var handle = new IndexingHandle(projectPath, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var state = _projects.GetOrAdd(projectPath, _ => new ProjectIndexState(projectPath));
        state.State = IndexState.Updating;

        _ = Task.Run(() => RunIncrementalIndexAsync(projectPath, state, ct), ct);
        return handle;
    }

    public Task<IndexStatus> GetStatusAsync(string projectPath, CancellationToken ct = default)
    {
        if (_projects.TryGetValue(projectPath, out var state))
        {
            return Task.FromResult(new IndexStatus(
                projectPath,
                state.State,
                state.TotalFiles,
                state.IndexedFiles,
                state.ChunkCount,
                state.SymbolCount,
                state.LastIndexedAt,
                state.ErrorMessage));
        }

        return Task.FromResult(new IndexStatus(projectPath, IndexState.NotIndexed, 0, 0, 0, 0, null, null));
    }

    public async Task RemoveIndexAsync(string projectPath, CancellationToken ct = default)
    {
        if (_watchers.TryRemove(projectPath, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _projects.TryRemove(projectPath, out _);
        _graphs.TryRemove(projectPath, out _);

        var collectionId = GetCollectionId(projectPath);
        await _vectorStore.DeleteCollectionAsync(collectionId, ct);
        _logger.LogInformation("[SemanticIndex] Removed index for {Path}", projectPath);
    }

    // ── Hybrid Search (dense + BM25 + RRF) ──────────────────────────────────

    public async Task<CodeSearchResult[]> SearchAsync(
        string projectPath,
        string query,
        CodeSearchOptions? options = null,
        CancellationToken ct = default)
    {
        var opts = options ?? new CodeSearchOptions();
        var collectionId = GetCollectionId(projectPath);

        // 1. Dense embedding search
        float[] queryEmbedding;
        try
        {
            queryEmbedding = await _embeddings.EmbedAsync(query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SemanticIndex] Embedding failed, falling back to BM25-only search");
            return await BM25OnlySearchAsync(projectPath, query, opts, ct);
        }

        var vectorResults = await _vectorStore.SearchAsync(
            queryEmbedding,
            collectionId: collectionId,
            topK: opts.TopK * 3,
            minScore: 0.1,
            ct: ct);

        // 2. BM25 scoring from stored chunk texts
        var allChunks = _projects.TryGetValue(projectPath, out var state)
            ? (IReadOnlyDictionary<string, CodeChunk>)state.CachedChunks
            : new Dictionary<string, CodeChunk>();

        var avgDocLength = allChunks.Values.Any()
            ? allChunks.Values.Average(c => (double)c.Content.Length / 5.0)
            : 300.0;

        var queryTermDf = ComputeQueryTermDocFreq(query, allChunks.Values);

        // 3. RRF fusion
        var densePosMap = vectorResults
            .Select((r, i) => (r.Record.Id, Rank: i + 1))
            .ToDictionary(x => x.Id, x => x.Rank);

        var bm25Scores = allChunks
            .Select(kv => (
                Id: kv.Key,
                Score: Libr4.IDE.Domain.FSharp.Bm25.score(
                    query,
                    kv.Value.Content,
                    avgDocLength,
                    allChunks.Count,
                    ToFSharpMap(queryTermDf))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(opts.TopK * 3)
            .ToList();

        var bm25PosMap = bm25Scores
            .Select((x, i) => (x.Id, Rank: i + 1))
            .ToDictionary(x => x.Id, x => x.Rank);

        const double rrfK = 60.0;
        var allIds = densePosMap.Keys.Union(bm25PosMap.Keys).Distinct().ToList();

        var fused = allIds.Select(id =>
        {
            var fallbackRank = allIds.Count + 100;
            var denseRank = densePosMap.TryGetValue(id, out var dr) ? dr : fallbackRank;
            var bm25Rank = bm25PosMap.TryGetValue(id, out var br) ? br : fallbackRank;
            var rrfScore = 1.0 / (rrfK + denseRank) + 1.0 / (rrfK + bm25Rank);

            var denseSc = vectorResults.FirstOrDefault(r => r.Record.Id == id)?.Score ?? 0.0;
            var bm25Sc = bm25Scores.FirstOrDefault(x => x.Id == id).Score;

            return (Id: id, FusedScore: rrfScore, DenseScore: denseSc, Bm25Score: bm25Sc);
        })
        .Where(x => x.FusedScore >= opts.MinScore)
        .OrderByDescending(x => x.FusedScore)
        .Take(opts.TopK)
        .ToList();

        var results = new List<CodeSearchResult>();
        foreach (var item in fused)
        {
            var id = item.Id; var fusedScore = item.FusedScore; var denseScore = item.DenseScore; var bm25Score = item.Bm25Score;
            if (!allChunks.TryGetValue(id, out var chunk))
            {
                var vr = vectorResults.FirstOrDefault(r => r.Record.Id == id);
                if (vr == null) continue;
                chunk = RecordToChunk(vr.Record);
            }

            if (opts.Languages != null && opts.Languages.Length > 0 &&
                !opts.Languages.Contains(chunk.Language, StringComparer.OrdinalIgnoreCase))
                continue;

            if (opts.FilePatterns != null && opts.FilePatterns.Length > 0 &&
                !opts.FilePatterns.Any(p => chunk.FilePath.Contains(p, StringComparison.OrdinalIgnoreCase)))
                continue;

            results.Add(new CodeSearchResult(
                FilePath: chunk.FilePath,
                StartLine: chunk.StartLine,
                EndLine: chunk.EndLine,
                Content: chunk.Content,
                SemanticScore: denseScore,
                KeywordScore: bm25Score,
                FusedScore: fusedScore,
                Language: chunk.Language,
                SymbolName: chunk.SymbolName,
                SymbolKind: chunk.SymbolKind));
        }

        return results.ToArray();
    }

    // ── Symbol Graph ──────────────────────────────────────────────────────────

    public async Task BuildGraphAsync(string projectPath, CancellationToken ct = default)
    {
        _logger.LogInformation("[SemanticIndex] Building symbol graph for {Path}", projectPath);

        var files = GetIndexableFiles(projectPath);
        var graph = new ProjectSymbolGraph(projectPath);

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                var ext = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
                var extracted = SymbolExtractor.ExtractSymbols(file, content, ext);

                foreach (var sym in extracted.Symbols)
                {
                    var id = $"{file}::{sym.Name}::{sym.StartLine}";
                    graph.Symbols[id] = new SymbolEntry(id, sym.Name, sym.Kind, file, sym.StartLine, sym.EndLine, ext);
                }

                foreach (var import in extracted.Imports)
                {
                    var resolved = ResolveImport(import, file, projectPath);
                    if (resolved != null)
                        graph.ImportEdges.Add((file, resolved));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[SemanticIndex] Graph extraction failed for {File}", file);
            }
        }

        _graphs[projectPath] = graph;
        _logger.LogInformation("[SemanticIndex] Graph built: {Symbols} symbols, {Imports} imports",
            graph.Symbols.Count, graph.ImportEdges.Count);
    }

    public Task<FileGraphInfo> QueryFileGraphAsync(string projectPath, string filePath, CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return Task.FromResult(new FileGraphInfo(filePath, [], [], []));

        var imports = graph.ImportEdges
            .Where(e => e.From == filePath)
            .Select(e => e.To)
            .Distinct()
            .ToArray();

        var importedBy = graph.ImportEdges
            .Where(e => e.To == filePath)
            .Select(e => e.From)
            .Distinct()
            .ToArray();

        var symbols = graph.Symbols.Values
            .Where(s => s.FilePath == filePath)
            .Select(s => new SymbolDefinitionDto(s.Id, s.Name, $"{filePath}::{s.Name}", s.Kind, s.Language, s.FilePath, s.StartLine, s.EndLine, null))
            .ToArray();

        return Task.FromResult(new FileGraphInfo(filePath, imports, importedBy, symbols));
    }

    public Task<GraphStatisticsDto> GetGraphStatsAsync(string projectPath, CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return Task.FromResult(new GraphStatisticsDto(0, 0, 0, 0, new Dictionary<string, int>(), [], [], 0));

        var langBreakdown = graph.Symbols.Values
            .GroupBy(s => s.Language)
            .ToDictionary(g => g.Key, g => g.Count());

        var fileDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (from, to) in graph.ImportEdges)
        {
            fileDegree[from] = fileDegree.GetValueOrDefault(from) + 1;
            fileDegree[to] = fileDegree.GetValueOrDefault(to) + 1;
        }

        var mostConnected = fileDegree
            .OrderByDescending(kv => kv.Value)
            .Take(20)
            .Select(kv => (kv.Key, kv.Value))
            .ToArray();

        var connectedFiles = new HashSet<string>(fileDegree.Keys, StringComparer.OrdinalIgnoreCase);
        var allProjectFiles = GetIndexableFiles(projectPath).Select(f => Path.GetRelativePath(projectPath, f)).ToList();
        var orphans = allProjectFiles.Where(f => !connectedFiles.Contains(f)).ToArray();

        var stats = new GraphStatisticsDto(
            TotalFiles: allProjectFiles.Count,
            TotalSymbols: graph.Symbols.Count,
            TotalEdges: graph.ImportEdges.Count + graph.CallEdges.Count,
            TotalImports: graph.ImportEdges.Count,
            LanguageBreakdown: langBreakdown,
            MostConnectedFiles: mostConnected,
            OrphanFiles: orphans,
            UnresolvedEdgePct: 0.0);

        return Task.FromResult(stats);
    }

    public Task<CircularDependencyResult[]> FindCircularDependenciesAsync(string projectPath, CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return Task.FromResult<CircularDependencyResult[]>([]);

        var allFiles = GetIndexableFiles(projectPath);

        var importEdges = graph.ImportEdges
            .Select(e => new Libr4.IDE.Domain.FSharp.ImportEdge(e.From, e.To, Microsoft.FSharp.Core.FSharpOption<string>.None, 0))
            .ToList();

        var fsharpImports = Microsoft.FSharp.Collections.ListModule.OfSeq(importEdges);
        var fsharpFiles = Microsoft.FSharp.Collections.ListModule.OfSeq(allFiles);

        var (_, cycles) = Libr4.IDE.Domain.FSharp.TopologicalSort.sortFiles(fsharpImports, fsharpFiles);

        if (Microsoft.FSharp.Collections.ListModule.IsEmpty(cycles))
            return Task.FromResult<CircularDependencyResult[]>([]);

        var cycleList = Microsoft.FSharp.Collections.ListModule.ToArray(cycles);
        return Task.FromResult<CircularDependencyResult[]>([new CircularDependencyResult(cycleList)]);
    }

    // ── Impact Analysis ──────────────────────────────────────────────────────

    public async Task<ImpactResult> GetBlastRadiusAsync(
        string projectPath,
        string symbolOrFilePath,
        int maxDepth = 5,
        CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return new ImpactResult(symbolOrFilePath, [], [], 0);

        var fsharpGraph = BuildFSharpGraph(graph, projectPath);
        var blastNodes = Libr4.IDE.Domain.FSharp.ImpactAnalysis.computeBlastRadius(fsharpGraph, symbolOrFilePath, maxDepth);

        var impactNodes = Microsoft.FSharp.Collections.ListModule.ToArray(blastNodes)
            .Select(n => new ImpactNode(n.SymbolId, n.FilePath, n.SymbolName, n.DistanceFromRoot, n.EdgeKind.ToString()))
            .ToArray();

        var affectedFiles = impactNodes.Select(n => n.FilePath).Distinct().ToArray();

        return new ImpactResult(symbolOrFilePath, impactNodes, affectedFiles, impactNodes.Length);
    }

    public async Task<FlowResult> TraceExecutionFlowAsync(
        string projectPath,
        string entrySymbolId,
        int maxDepth = 4,
        CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return new FlowResult(entrySymbolId, [], []);

        var fsharpGraph = BuildFSharpGraph(graph, projectPath);
        var flowNodes = Libr4.IDE.Domain.FSharp.ImpactAnalysis.traceFlow(fsharpGraph, entrySymbolId, maxDepth);

        var nodes = Microsoft.FSharp.Collections.ListModule.ToArray(flowNodes)
            .Select(n => new ImpactNode(n.SymbolId, n.FilePath, n.SymbolName, n.DistanceFromRoot, n.EdgeKind.ToString()))
            .ToArray();

        return new FlowResult(entrySymbolId, nodes, [entrySymbolId]);
    }

    public Task<SymbolView> GetSymbolViewAsync(string projectPath, string symbolId, CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return Task.FromResult(new SymbolView(
                new SymbolDefinitionDto(symbolId, symbolId, symbolId, "Unknown", "Unknown", "", 0, 0, null),
                [], [], []));

        graph.Symbols.TryGetValue(symbolId, out var sym);
        var symDto = sym != null
            ? new SymbolDefinitionDto(sym.Id, sym.Name, $"{sym.FilePath}::{sym.Name}", sym.Kind, sym.Language, sym.FilePath, sym.StartLine, sym.EndLine, null)
            : new SymbolDefinitionDto(symbolId, symbolId, symbolId, "Unknown", "Unknown", "", 0, 0, null);

        var callerIds = graph.CallEdges.Where(e => e.Callee == symbolId).Select(e => e.Caller).Distinct().ToList();
        var calleeIds = graph.CallEdges.Where(e => e.Caller == symbolId).Select(e => e.Callee).Distinct().ToList();

        var callers = callerIds.Select(id => graph.Symbols.TryGetValue(id, out var s)
            ? new SymbolDefinitionDto(s.Id, s.Name, $"{s.FilePath}::{s.Name}", s.Kind, s.Language, s.FilePath, s.StartLine, s.EndLine, null)
            : new SymbolDefinitionDto(id, id, id, "Unknown", "Unknown", "", 0, 0, null)).ToArray();

        var callees = calleeIds.Select(id => graph.Symbols.TryGetValue(id, out var s)
            ? new SymbolDefinitionDto(s.Id, s.Name, $"{s.FilePath}::{s.Name}", s.Kind, s.Language, s.FilePath, s.StartLine, s.EndLine, null)
            : new SymbolDefinitionDto(id, id, id, "Unknown", "Unknown", "", 0, 0, null)).ToArray();

        var filePath = sym?.FilePath ?? "";
        var filesImporting = graph.ImportEdges
            .Where(e => e.To == filePath)
            .Select(e => e.From)
            .Distinct()
            .ToArray();

        return Task.FromResult(new SymbolView(symDto, callers, callees, filesImporting));
    }

    public Task<SymbolDefinitionDto[]> ListSymbolsAsync(
        string projectPath,
        string? filePathFilter = null,
        string? namePrefix = null,
        CancellationToken ct = default)
    {
        if (!_graphs.TryGetValue(projectPath, out var graph))
            return Task.FromResult<SymbolDefinitionDto[]>([]);

        var query = graph.Symbols.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filePathFilter))
            query = query.Where(s => s.FilePath.Contains(filePathFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(namePrefix))
            query = query.Where(s => s.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(query
            .Take(200)
            .Select(s => new SymbolDefinitionDto(s.Id, s.Name, $"{s.FilePath}::{s.Name}", s.Kind, s.Language, s.FilePath, s.StartLine, s.EndLine, null))
            .ToArray());
    }

    // ── Private indexing logic ───────────────────────────────────────────────

    private async Task RunFullIndexAsync(
        string projectPath,
        IndexingOptions opts,
        ProjectIndexState state,
        CancellationToken ct)
    {
        try
        {
            var files = GetIndexableFiles(projectPath);
            state.TotalFiles = files.Count;
            state.IndexedFiles = 0;
            state.ChunkCount = 0;

            _logger.LogInformation("[SemanticIndex] Indexing {Count} files in {Path}", files.Count, projectPath);

            var collectionId = GetCollectionId(projectPath);
            var batchSize = opts.BatchSize;

            for (int i = 0; i < files.Count; i += batchSize)
            {
                if (ct.IsCancellationRequested) break;
                var batch = files.Skip(i).Take(batchSize).ToList();
                await IndexBatchAsync(batch, collectionId, projectPath, state, ct);
            }

            state.State = IndexState.Ready;
            state.LastIndexedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("[SemanticIndex] Indexing complete: {Chunks} chunks", state.ChunkCount);

            if (opts.BuildGraphAfterIndex)
                await BuildGraphAsync(projectPath, ct);

            if (opts.WatchForChanges)
                StartWatcher(projectPath);
        }
        catch (Exception ex)
        {
            state.State = IndexState.Error;
            state.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[SemanticIndex] Indexing failed for {Path}", projectPath);
        }
    }

    private async Task RunIncrementalIndexAsync(string projectPath, ProjectIndexState state, CancellationToken ct)
    {
        try
        {
            var files = GetIndexableFiles(projectPath);
            var collectionId = GetCollectionId(projectPath);
            var changed = new List<string>();

            foreach (var file in files)
            {
                var hash = await ComputeHashAsync(file, ct);
                if (!_fileHashes.TryGetValue(file, out var existingHash) || existingHash != hash)
                    changed.Add(file);
            }

            if (changed.Count > 0)
            {
                _logger.LogInformation("[SemanticIndex] Incremental: {Count} changed files", changed.Count);
                await IndexBatchAsync(changed, collectionId, projectPath, state, ct);
            }

            state.State = IndexState.Ready;
            state.LastIndexedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            state.State = IndexState.Error;
            state.ErrorMessage = ex.Message;
        }
    }

    private async Task IndexBatchAsync(
        List<string> files,
        string collectionId,
        string projectPath,
        ProjectIndexState state,
        CancellationToken ct)
    {
        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) return;
            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                var hash = ComputeHash(content);

                if (_fileHashes.TryGetValue(file, out var existingHash) && existingHash == hash)
                {
                    state.IndexedFiles++;
                    continue;
                }

                var ext = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
                var chunks = CodeChunker.Chunk(file, content, ext);

                if (!_projects.TryGetValue(projectPath, out var ps)) continue;

                foreach (var chunk in chunks)
                {
                    float[] embedding;
                    try
                    {
                        embedding = await _embeddings.EmbedAsync(chunk.Content, ct);
                    }
                    catch
                    {
                        embedding = GenerateFallbackEmbedding(chunk.Content);
                    }

                    var metadata = new Dictionary<string, string>
                    {
                        ["filePath"]   = chunk.FilePath,
                        ["language"]   = chunk.Language,
                        ["startLine"]  = chunk.StartLine.ToString(),
                        ["endLine"]    = chunk.EndLine.ToString(),
                        ["symbolName"] = chunk.SymbolName ?? string.Empty,
                        ["symbolKind"] = chunk.SymbolKind
                    };

                    await _vectorStore.UpsertAsync(new VectorRecord(
                        Id: chunk.Id,
                        CollectionId: collectionId,
                        Embedding: embedding,
                        Text: chunk.Content,
                        Metadata: metadata), ct);

                    ps.CachedChunks[chunk.Id] = chunk;
                    state.ChunkCount++;
                }

                _fileHashes[file] = hash;
                state.IndexedFiles++;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[SemanticIndex] Failed to index file {File}", file);
            }
        }
    }

    private async Task<CodeSearchResult[]> BM25OnlySearchAsync(
        string projectPath,
        string query,
        CodeSearchOptions opts,
        CancellationToken ct)
    {
        if (!_projects.TryGetValue(projectPath, out var state))
            return [];

        var allChunks = state.CachedChunks;
        if (allChunks.Count == 0) return [];

        var avgDocLength = allChunks.Values.Average(c => (double)c.Content.Length / 5.0);
        var queryTermDf = ComputeQueryTermDocFreq(query, allChunks.Values);

        return allChunks.Values
            .Select(chunk => (chunk, Score: Libr4.IDE.Domain.FSharp.Bm25.score(
                query, chunk.Content, avgDocLength, allChunks.Count, ToFSharpMap(queryTermDf))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(opts.TopK)
            .Select(x => new CodeSearchResult(
                FilePath: x.chunk.FilePath,
                StartLine: x.chunk.StartLine,
                EndLine: x.chunk.EndLine,
                Content: x.chunk.Content,
                SemanticScore: 0,
                KeywordScore: x.Score,
                FusedScore: x.Score,
                Language: x.chunk.Language,
                SymbolName: x.chunk.SymbolName,
                SymbolKind: x.chunk.SymbolKind))
            .ToArray();
    }

    // ── File watching ─────────────────────────────────────────────────────────

    private void StartWatcher(string projectPath)
    {
        if (_watchers.ContainsKey(projectPath)) return;

        var watcher = new FileSystemWatcher(projectPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        var debounce = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        void OnChanged(object _, FileSystemEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath).TrimStart('.').ToLowerInvariant();
            if (!SupportedExtensions.Contains("." + ext)) return;

            var now = DateTime.UtcNow;
            if (debounce.TryGetValue(e.FullPath, out var last) && (now - last).TotalSeconds < 2) return;
            debounce[e.FullPath] = now;

            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                await RunIncrementalIndexAsync(projectPath, _projects.GetOrAdd(projectPath, _ => new ProjectIndexState(projectPath)), CancellationToken.None);
            });
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        _watchers[projectPath] = watcher;
        _logger.LogInformation("[SemanticIndex] File watcher started for {Path}", projectPath);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<string> GetIndexableFiles(string projectPath)
    {
        if (!Directory.Exists(projectPath)) return [];

        return Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (!SupportedExtensions.Contains(ext)) return false;

                var rel = Path.GetRelativePath(projectPath, f);
                return !DefaultIgnorePatterns.Any(p =>
                    rel.StartsWith(p, StringComparison.OrdinalIgnoreCase) ||
                    rel.Contains($"/{p}/") || rel.Contains($"\\{p}\\"));
            })
            .ToList();
    }

    private static string GetCollectionId(string projectPath)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(projectPath));
        return "code_" + Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string ComputeHash(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private static Dictionary<string, int> ComputeQueryTermDocFreq(string query, IEnumerable<CodeChunk> chunks)
    {
        var queryTokens = Libr4.IDE.Domain.FSharp.Bm25.tokenize(query);
        var df = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in chunks)
        {
            var docTokens = new HashSet<string>(Libr4.IDE.Domain.FSharp.Bm25.tokenize(chunk.Content));
            foreach (var token in queryTokens)
            {
                if (docTokens.Contains(token))
                    df[token] = df.GetValueOrDefault(token) + 1;
            }
        }
        return df;
    }

    private static Microsoft.FSharp.Collections.FSharpMap<string, int> ToFSharpMap(Dictionary<string, int> dict)
    {
        var pairs = dict.Select(kv => Tuple.Create(kv.Key, kv.Value));
        return Microsoft.FSharp.Collections.MapModule.OfSeq(pairs);
    }

    private static float[] GenerateFallbackEmbedding(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var vec = new float[384];
        for (int i = 0; i < 384; i++)
            vec[i] = (bytes[i % 32] / 255f) * 2 - 1;
        return vec;
    }

    private static CodeChunk RecordToChunk(VectorRecord r) =>
        new(r.Id, r.Metadata?.GetValueOrDefault("filePath") ?? "",
            r.Metadata?.GetValueOrDefault("language") ?? "",
            int.TryParse(r.Metadata?.GetValueOrDefault("startLine"), out var sl) ? sl : 0,
            int.TryParse(r.Metadata?.GetValueOrDefault("endLine"), out var el) ? el : 0,
            r.Text,
            r.Metadata?.GetValueOrDefault("symbolName"),
            r.Metadata?.GetValueOrDefault("symbolKind") ?? "");

    private static string? ResolveImport(string importPath, string fromFile, string projectRoot)
    {
        if (importPath.StartsWith('.'))
        {
            var dir = Path.GetDirectoryName(fromFile) ?? projectRoot;
            var resolved = Path.GetFullPath(Path.Combine(dir, importPath));
            foreach (var ext in new[] { ".ts", ".tsx", ".js", ".jsx", ".cs", ".fs" })
            {
                if (File.Exists(resolved + ext)) return resolved + ext;
            }
        }
        return null;
    }

    private Libr4.IDE.Domain.FSharp.SymbolGraph BuildFSharpGraph(ProjectSymbolGraph graph, string projectPath)
    {
        var symbolMap = Microsoft.FSharp.Collections.MapModule.OfSeq(
            graph.Symbols.Select(kv => Tuple.Create(kv.Key,
                new Libr4.IDE.Domain.FSharp.Symbol(
                    id: kv.Key,
                    name: kv.Value.Name,
                    fullyQualifiedName: $"{kv.Value.FilePath}::{kv.Value.Name}",
                    kind: Libr4.IDE.Domain.FSharp.SymbolKind.Method,
                    language: Libr4.IDE.Domain.FSharp.Language.NewUnknown(kv.Value.Language),
                    filePath: kv.Value.FilePath,
                    startLine: kv.Value.StartLine,
                    endLine: kv.Value.EndLine,
                    modifiers: Microsoft.FSharp.Collections.FSharpList<string>.Empty,
                    docComment: Microsoft.FSharp.Core.FSharpOption<string>.None))));

        var callEdgeList = graph.CallEdges
            .Select(e => new Libr4.IDE.Domain.FSharp.CallEdge(
                fromSymbolId: e.Caller,
                toSymbolId: e.Callee,
                edgeKind: Libr4.IDE.Domain.FSharp.EdgeKind.DirectCall,
                line: 0))
            .ToList();

        var importEdgeList = graph.ImportEdges
            .Select(e => new Libr4.IDE.Domain.FSharp.ImportEdge(
                fromFile: e.From,
                toFile: e.To,
                alias: Microsoft.FSharp.Core.FSharpOption<string>.None,
                line: 0))
            .ToList();

        return new Libr4.IDE.Domain.FSharp.SymbolGraph(
            projectPath: projectPath,
            symbols: symbolMap,
            callEdges: Microsoft.FSharp.Collections.ListModule.OfSeq(callEdgeList),
            importEdges: Microsoft.FSharp.Collections.ListModule.OfSeq(importEdgeList),
            buildAt: DateTimeOffset.UtcNow,
            fileCount: graph.Symbols.Values.Select(s => s.FilePath).Distinct().Count(),
            symbolCount: graph.Symbols.Count);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }
}

// ── Internal state models ────────────────────────────────────────────────────

internal sealed class ProjectIndexState(string projectPath)
{
    public string ProjectPath { get; } = projectPath;
    public IndexState State { get; set; } = IndexState.NotIndexed;
    public Guid OperationId { get; set; }
    public int TotalFiles { get; set; }
    public int IndexedFiles { get; set; }
    public int ChunkCount { get; set; }
    public int SymbolCount { get; set; }
    public DateTimeOffset? LastIndexedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public ConcurrentDictionary<string, CodeChunk> CachedChunks { get; } = new();
}

internal sealed class ProjectSymbolGraph(string projectPath)
{
    public string ProjectPath { get; } = projectPath;
    public Dictionary<string, SymbolEntry> Symbols { get; } = new();
    public List<(string From, string To)> ImportEdges { get; } = [];
    public List<(string Caller, string Callee)> CallEdges { get; } = [];
}

internal sealed record SymbolEntry(
    string Id,
    string Name,
    string Kind,
    string FilePath,
    int StartLine,
    int EndLine,
    string Language);

public sealed class SemanticIndexOptions
{
    public int EmbeddingDimensions { get; set; } = 384;
    public bool AutoStartWatcher { get; set; } = true;
    public int MaxFileSizeBytes { get; set; } = 512 * 1024;
}
