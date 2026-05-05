using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// Production Qdrant vector store.
/// Falls back to InProcessVectorMemoryStore when Qdrant is unavailable.
/// </summary>
public sealed class QdrantVectorMemoryStore : IVectorMemoryStore
{
    private readonly HttpClient _http;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorMemoryStore> _logger;
    private readonly InProcessVectorMemoryStore _fallback = new();
    private readonly HashSet<string> _ensuredCollections = new();
    private bool _qdrantAvailable = true;

    public QdrantVectorMemoryStore(
        HttpClient http,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorMemoryStore> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task UpsertAsync(VectorRecord record, CancellationToken ct = default)
    {
        if (!_qdrantAvailable) { await _fallback.UpsertAsync(record, ct); return; }
        try
        {
            await EnsureCollectionAsync(record.CollectionId, record.Embedding.Length, ct);
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
                $"{_options.BaseUrl}/collections/{record.CollectionId}/points?wait=false", point, ct);

            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("[Qdrant] Upsert failed {Status}", resp.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "[Qdrant] Unavailable, switching to in-process fallback");
            _qdrantAvailable = false;
            await _fallback.UpsertAsync(record, ct);
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
            return await _fallback.SearchAsync(queryEmbedding, collectionId, topK, minScore, ct);

        try
        {
            var body = new { vector = queryEmbedding, limit = topK, score_threshold = minScore, with_payload = true };
            var resp = await _http.PostAsJsonAsync(
                $"{_options.BaseUrl}/collections/{collectionId}/points/search", body, ct);

            if (!resp.IsSuccessStatusCode)
                return await _fallback.SearchAsync(queryEmbedding, collectionId, topK, minScore, ct);

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var results = new List<VectorSearchResult>();

            foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                var score = item.GetProperty("score").GetDouble();
                var payload = item.GetProperty("payload");
                var id = item.GetProperty("id").ToString();
                var text = payload.TryGetProperty("_text", out var t) ? t.GetString() ?? "" : "";

                var meta = new Dictionary<string, string>();
                foreach (var p in payload.EnumerateObject())
                    if (p.Name != "_text") meta[p.Name] = p.Value.ToString();

                results.Add(new VectorSearchResult(
                    new VectorRecord(id, collectionId, queryEmbedding, text, meta), score));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Qdrant] Search failed, using fallback");
            return await _fallback.SearchAsync(queryEmbedding, collectionId, topK, minScore, ct);
        }
    }

    public async Task DeleteCollectionAsync(string collectionId, CancellationToken ct = default)
    {
        await _fallback.DeleteCollectionAsync(collectionId, ct);
        if (!_qdrantAvailable) return;
        try
        {
            await _http.DeleteAsync($"{_options.BaseUrl}/collections/{collectionId}", ct);
            _ensuredCollections.Remove(collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Qdrant] Delete collection failed");
        }
    }

    private async Task EnsureCollectionAsync(string collectionId, int dimensions, CancellationToken ct)
    {
        if (_ensuredCollections.Contains(collectionId)) return;

        var checkResp = await _http.GetAsync($"{_options.BaseUrl}/collections/{collectionId}", ct);
        if (checkResp.IsSuccessStatusCode) { _ensuredCollections.Add(collectionId); return; }

        var createBody = new
        {
            vectors = new { size = dimensions, distance = "Cosine" },
            hnsw_config = new { m = 16, ef_construct = 100 },
            optimizers_config = new { indexing_threshold = 20000 }
        };

        var createResp = await _http.PutAsJsonAsync(
            $"{_options.BaseUrl}/collections/{collectionId}", createBody, ct);

        if (createResp.IsSuccessStatusCode)
        {
            _ensuredCollections.Add(collectionId);
            _logger.LogInformation("[Qdrant] Created collection {Col} dim={Dim}", collectionId, dimensions);
        }
    }

    private static string ToQdrantId(string id)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(id));
        return new Guid(bytes).ToString();
    }
}

public sealed class QdrantOptions
{
    public string BaseUrl { get; set; } = "http://localhost:6333";
    public string? ApiKey { get; set; }
}
