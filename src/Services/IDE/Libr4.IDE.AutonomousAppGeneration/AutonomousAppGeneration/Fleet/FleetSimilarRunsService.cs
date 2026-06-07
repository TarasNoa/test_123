using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetSimilarRunsService
{
    Task IndexAsync(FleetSessionIndexDocument document, CancellationToken ct = default);
    Task RemoveAsync(Guid runId, CancellationToken ct = default);
    Task<FleetSimilarRunsResult> FindSimilarAsync(Guid runId, int? limit = null, CancellationToken ct = default);
}

public sealed record FleetSimilarRunHit(
    Guid RunId,
    string Title,
    AgentFleetStatus Status,
    string? Stack,
    string? SpaceId,
    double Score,
    string Snippet,
    DateTime LastActivityAtUtc,
    bool Pinned);

public sealed record FleetSimilarRunsResult(
    Guid SourceRunId,
    IReadOnlyList<FleetSimilarRunHit> Hits,
    string Method);

public sealed class FleetSimilarRunsService : IFleetSimilarRunsService
{
    private readonly IVectorMemoryStore _vectorStore;
    private readonly IEmbeddingService _embeddings;
    private readonly IAgentFleetIndexStore _fleetIndex;
    private readonly FleetSimilarRunsOptions _options;
    private readonly ILogger<FleetSimilarRunsService> _logger;

    public FleetSimilarRunsService(
        IVectorMemoryStore vectorStore,
        IEmbeddingService embeddings,
        IAgentFleetIndexStore fleetIndex,
        IOptions<FleetSimilarRunsOptions> options,
        ILogger<FleetSimilarRunsService> logger)
    {
        _vectorStore = vectorStore;
        _embeddings = embeddings;
        _fleetIndex = fleetIndex;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IndexAsync(FleetSessionIndexDocument document, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        var text = SqliteFleetSessionSearchService.BuildBody(document);
        if (string.IsNullOrWhiteSpace(text))
            return;

        var embedding = await _embeddings.EmbedAsync(text, ct).ConfigureAwait(false);
        var runId = document.RunId.ToString("D");
        await _vectorStore.UpsertAsync(
            new VectorRecord(
                runId,
                _options.CollectionId,
                embedding,
                text,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["run_id"] = runId,
                    ["title"] = document.Title,
                    ["outcome"] = document.Outcome,
                    ["stack"] = document.StackTags ?? string.Empty,
                    ["space_id"] = document.SpaceName ?? string.Empty
                }),
            ct).ConfigureAwait(false);
    }

    public Task RemoveAsync(Guid runId, CancellationToken ct = default) =>
        _options.Enabled
            ? _vectorStore.DeleteAsync(runId.ToString("D"), _options.CollectionId, ct)
            : Task.CompletedTask;

    public async Task<FleetSimilarRunsResult> FindSimilarAsync(
        Guid runId,
        int? limit = null,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new FleetSimilarRunsResult(runId, [], "disabled");

        var take = Math.Clamp(limit ?? _options.DefaultLimit, 1, 25);
        var sourceEntry = await _fleetIndex.GetAsync(runId, ct).ConfigureAwait(false);
        var sourceText = sourceEntry is null
            ? string.Empty
            : SqliteFleetSessionSearchService.BuildBody(new FleetSessionIndexDocument(
                runId,
                sourceEntry.Title,
                UserRequest: null,
                ErrorSignature: sourceEntry.FailureReason,
                FilesTouched: null,
                SpaceName: sourceEntry.SpaceId,
                StackTags: sourceEntry.Stack,
                Outcome: SqliteFleetSessionSearchService.ToOutcome(sourceEntry.Status),
                sourceEntry.LastActivityAtUtc,
                sourceEntry.Pinned));

        if (string.IsNullOrWhiteSpace(sourceText))
            return new FleetSimilarRunsResult(runId, [], "empty_source");

        var queryEmbedding = await _embeddings.EmbedAsync(sourceText, ct).ConfigureAwait(false);
        var vectorHits = await _vectorStore.SearchAsync(
            queryEmbedding,
            _options.CollectionId,
            topK: take + 1,
            minScore: _options.MinScore,
            ct).ConfigureAwait(false);

        var hits = new List<FleetSimilarRunHit>();
        foreach (var vectorHit in vectorHits)
        {
            if (!TryParseRunId(vectorHit.Record, out var hitRunId) || hitRunId == runId)
                continue;

            var entry = await _fleetIndex.GetAsync(hitRunId, ct).ConfigureAwait(false);
            hits.Add(new FleetSimilarRunHit(
                hitRunId,
                entry?.Title ?? ReadMetadata(vectorHit.Record, "title") ?? $"Run {hitRunId.ToString()[..8]}",
                entry?.Status ?? AgentFleetStatus.Queued,
                entry?.Stack ?? NullIfEmpty(ReadMetadata(vectorHit.Record, "stack")),
                entry?.SpaceId ?? NullIfEmpty(ReadMetadata(vectorHit.Record, "space_id")),
                vectorHit.Score,
                TruncateSnippet(vectorHit.Record.Text),
                entry?.LastActivityAtUtc ?? DateTime.UtcNow,
                entry?.Pinned ?? false));

            if (hits.Count >= take)
                break;
        }

        _logger.LogDebug("Similar runs for {RunId}: {Count} hit(s)", runId, hits.Count);
        return new FleetSimilarRunsResult(runId, hits, "embedding");
    }

    private static bool TryParseRunId(VectorRecord record, out Guid runId)
    {
        runId = Guid.Empty;
        if (Guid.TryParse(record.Id, out runId))
            return true;

        var meta = ReadMetadata(record, "run_id");
        return !string.IsNullOrWhiteSpace(meta) && Guid.TryParse(meta, out runId);
    }

    private static string? ReadMetadata(VectorRecord record, string key) =>
        record.Metadata is not null && record.Metadata.TryGetValue(key, out var value)
            ? value
            : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string TruncateSnippet(string text, int max = 160) =>
        text.Length <= max ? text : text[..max] + "…";
}
