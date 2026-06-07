using System.Net.Http.Json;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public sealed class QdrantVectorMemoryStore : IVectorMemoryStore
{
    private readonly HttpClient _http;
    private readonly QdrantSyncOptions _options;
    private readonly ILogger<QdrantVectorMemoryStore> _logger;
    private readonly InProcessVectorMemoryStore _fallback = new();
    private readonly HashSet<string> _ensuredCollections = new(StringComparer.Ordinal);
    private bool _qdrantAvailable = true;

    public QdrantVectorMemoryStore(
        HttpClient http,
        IOptions<QdrantSyncOptions> options,
        ILogger<QdrantVectorMemoryStore> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task UpsertAsync(VectorRecord record, CancellationToken ct = default)
    {
        if (!_qdrantAvailable)
        {
            await _fallback.UpsertAsync(record, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            await EnsureCollectionAsync(record.CollectionId, record.Embedding.Length, ct).ConfigureAwait(false);
            var payload = record.Metadata?.ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                          ?? new Dictionary<string, object>();
            payload["_text"] = record.Text;

            var point = new
            {
                points = new[]
                {
                    new
                    {
                        id = ToQdrantId(record.Id),
                        vector = record.Embedding,
                        payload
                    }
                }
            };

            var resp = await _http.PutAsJsonAsync(
                $"{_options.Qdrant.BaseUrl}/collections/{record.CollectionId}/points?wait=false",
                point,
                ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("[Qdrant] Upsert failed {Status}", resp.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "[Qdrant] Unavailable, switching to in-process fallback");
            _qdrantAvailable = false;
            await _fallback.UpsertAsync(record, ct).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        string? collectionId = null,
        int topK = 10,
        double minScore = 0.0,
        CancellationToken ct = default)
    {
        if (!_qdrantAvailable || string.IsNullOrEmpty(collectionId))
            return await _fallback.SearchAsync(queryEmbedding, collectionId, topK, minScore, ct).ConfigureAwait(false);

        try
        {
            var body = new
            {
                vector = queryEmbedding,
                limit = topK,
                score_threshold = minScore,
                with_payload = true
            };
            var resp = await _http.PostAsJsonAsync(
                $"{_options.Qdrant.BaseUrl}/collections/{collectionId}/points/search",
                body,
                ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return await _fallback.SearchAsync(queryEmbedding, collectionId, topK, minScore, ct).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var results = new List<VectorSearchResult>();

            foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                var score = item.GetProperty("score").GetDouble();
                var payload = item.GetProperty("payload");
                var id = item.GetProperty("id").ToString();
                var text = payload.TryGetProperty("_text", out var textNode) ? textNode.GetString() ?? "" : "";

                var meta = new Dictionary<string, string>();
                foreach (var property in payload.EnumerateObject())
                {
                    if (property.Name != "_text")
                        meta[property.Name] = property.Value.ToString();
                }

                results.Add(new VectorSearchResult(
                    new VectorRecord(id, collectionId, queryEmbedding, text, meta),
                    score));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Qdrant] Search failed, using fallback");
            return await _fallback.SearchAsync(queryEmbedding, collectionId, topK, minScore, ct).ConfigureAwait(false);
        }
    }

    public async Task DeleteCollectionAsync(string collectionId, CancellationToken ct = default)
    {
        await _fallback.DeleteCollectionAsync(collectionId, ct).ConfigureAwait(false);
        if (!_qdrantAvailable)
            return;

        try
        {
            await _http.DeleteAsync($"{_options.Qdrant.BaseUrl}/collections/{collectionId}", ct).ConfigureAwait(false);
            _ensuredCollections.Remove(collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Qdrant] Delete collection failed");
        }
    }

    public async Task DeleteAsync(string id, string collectionId, CancellationToken ct = default)
    {
        await _fallback.DeleteAsync(id, collectionId, ct).ConfigureAwait(false);
        if (!_qdrantAvailable)
            return;

        try
        {
            var body = new { points = new[] { ToQdrantId(id) } };
            await _http.PostAsJsonAsync(
                $"{_options.Qdrant.BaseUrl}/collections/{collectionId}/points/delete?wait=false",
                body,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Qdrant] Delete point failed");
        }
    }

    private async Task EnsureCollectionAsync(string collectionId, int dimensions, CancellationToken ct)
    {
        if (_ensuredCollections.Contains(collectionId))
            return;

        var checkResp = await _http.GetAsync($"{_options.Qdrant.BaseUrl}/collections/{collectionId}", ct).ConfigureAwait(false);
        if (checkResp.IsSuccessStatusCode)
        {
            _ensuredCollections.Add(collectionId);
            return;
        }

        var createBody = new
        {
            vectors = new { size = dimensions, distance = "Cosine" },
            hnsw_config = new { m = 16, ef_construct = 100 },
            optimizers_config = new { indexing_threshold = 20000 }
        };

        var createResp = await _http.PutAsJsonAsync(
            $"{_options.Qdrant.BaseUrl}/collections/{collectionId}",
            createBody,
            ct).ConfigureAwait(false);

        if (createResp.IsSuccessStatusCode)
        {
            _ensuredCollections.Add(collectionId);
            _logger.LogInformation("[Qdrant] Created collection {Collection} dim={Dimensions}", collectionId, dimensions);
        }
    }

    private static string ToQdrantId(string id)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(id));
        return new Guid(bytes).ToString();
    }
}
