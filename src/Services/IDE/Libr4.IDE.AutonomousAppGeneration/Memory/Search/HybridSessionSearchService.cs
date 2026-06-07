using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;

public sealed class HybridSessionSearchService : ISessionSearchService
{
    private readonly IRolloutRecorder _rollout;
    private readonly IHermesMemoryStore _memory;
    private readonly IVectorMemoryStore _vectorStore;
    private readonly IEmbeddingService _embeddings;
    private readonly QdrantSyncOptions _options;

    public HybridSessionSearchService(
        IRolloutRecorder rollout,
        IHermesMemoryStore memory,
        IVectorMemoryStore vectorStore,
        IEmbeddingService embeddings,
        IOptions<QdrantSyncOptions> options)
    {
        _rollout = rollout;
        _memory = memory;
        _vectorStore = vectorStore;
        _embeddings = embeddings;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<SessionSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SessionSearchHit>();

        var candidateCount = Math.Max(limit, limit * Math.Max(1, _options.HybridSearchCandidateMultiplier));
        var rolloutTask = _rollout.SearchAsync(FtsQueryHelper.ToMatchExpression(query), candidateCount, ct);
        var memoryTask = SearchMemoryHybridAsync(query, candidateCount, ct);
        await Task.WhenAll(rolloutTask, memoryTask).ConfigureAwait(false);

        var hits = new List<SessionSearchHit>(limit);
        foreach (var rolloutHit in rolloutTask.Result)
        {
            hits.Add(new SessionSearchHit(
                Source: "rollout",
                RunId: rolloutHit.RunId,
                StepNumber: rolloutHit.StepNumber,
                ToolName: rolloutHit.ToolName,
                MemoryKey: null,
                MemoryKind: null,
                Snippet: rolloutHit.Snippet,
                Score: rolloutHit.Score));
        }

        foreach (var memoryHit in memoryTask.Result)
        {
            hits.Add(new SessionSearchHit(
                Source: "memory",
                RunId: memoryHit.RunId,
                StepNumber: null,
                ToolName: null,
                MemoryKey: memoryHit.Key,
                MemoryKind: memoryHit.Kind,
                Snippet: memoryHit.Snippet,
                Score: memoryHit.Score));
        }

        return hits
            .OrderByDescending(hit => hit.Score)
            .ThenByDescending(hit => hit.RunId)
            .Take(limit)
            .ToList();
    }

    private async Task<IReadOnlyList<HermesMemorySearchHit>> SearchMemoryHybridAsync(
        string query,
        int candidateCount,
        CancellationToken ct)
    {
        var ftsHits = await _memory.SearchSummariesAsync(query, candidateCount, ct).ConfigureAwait(false);
        if (!_options.UseQdrantSync)
            return ftsHits;

        var queryEmbedding = await _embeddings.EmbedAsync(query, ct).ConfigureAwait(false);
        var vectorHits = await _vectorStore.SearchAsync(
            queryEmbedding,
            _options.CollectionId,
            candidateCount,
            minScore: 0.0,
            ct).ConfigureAwait(false);

        var ftsRanked = ftsHits.Select(hit => hit.Key).ToList();

        var vectorRanked = vectorHits
            .Select(hit => hit.Record.Metadata is not null
                           && hit.Record.Metadata.TryGetValue("key", out var key)
                ? key
                : hit.Record.Id)
            .ToList();

        var fused = ReciprocalRankFusion.Fuse([ftsRanked, vectorRanked]);
        if (fused.Count == 0)
            return ftsHits;

        var hitById = new Dictionary<string, HermesMemorySearchHit>(StringComparer.Ordinal);
        foreach (var ftsHit in ftsHits)
            hitById[ftsHit.Key] = ftsHit;

        foreach (var vectorHit in vectorHits)
        {
            var id = vectorHit.Record.Metadata is not null
                     && vectorHit.Record.Metadata.TryGetValue("key", out var key)
                ? key
                : vectorHit.Record.Id;

            if (hitById.ContainsKey(id))
                continue;

            var runId = Guid.Empty;
            var kind = "semantic";
            var memoryKey = vectorHit.Record.Id;
            if (vectorHit.Record.Metadata is not null)
            {
                if (vectorHit.Record.Metadata.TryGetValue("run_id", out var runIdText)
                    && Guid.TryParse(runIdText, out var parsedRunId))
                {
                    runId = parsedRunId;
                }

                if (vectorHit.Record.Metadata.TryGetValue("kind", out var kindText))
                    kind = kindText;
                if (vectorHit.Record.Metadata.TryGetValue("key", out var keyText))
                    memoryKey = keyText;
            }

            hitById[id] = new HermesMemorySearchHit(
                runId,
                kind,
                memoryKey,
                vectorHit.Record.Text,
                vectorHit.Score);
        }

        return fused
            .Select(item => hitById.TryGetValue(item.Id, out var hit)
                ? hit with { Score = item.Score }
                : null)
            .Where(hit => hit is not null)
            .Cast<HermesMemorySearchHit>()
            .Take(candidateCount)
            .ToList();
    }
}
