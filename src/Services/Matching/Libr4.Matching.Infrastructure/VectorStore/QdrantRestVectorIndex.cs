using System.Net.Http.Json;
using System.Text.Json;
using Libr4.Matching.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Infrastructure.VectorStore;

public sealed class QdrantRestVectorIndex : IVectorIndex
{
    private readonly HttpClient _http;
    private readonly ILogger<QdrantRestVectorIndex> _logger;

    private const string FreelancersCollection = "freelancers";
    private const string TasksCollection = "tasks";
    private const uint VectorDimension = 768;

    public QdrantRestVectorIndex(HttpClient http, ILogger<QdrantRestVectorIndex> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task EnsureCollectionsAsync(CancellationToken ct = default)
    {
        foreach (var name in new[] { FreelancersCollection, TasksCollection })
        {
            var resp = await _http.GetAsync($"/collections/{name}", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var body = new
                {
                    vectors = new
                    {
                        size = VectorDimension,
                        distance = "Cosine",
                        on_disk = true,
                    }
                };
                var createResp = await _http.PutAsJsonAsync($"/collections/{name}", body, ct);
                createResp.EnsureSuccessStatusCode();
                _logger.LogInformation("Created Qdrant collection: {Name}", name);
            }
        }
    }

    public async Task UpsertFreelancerAsync(
        Guid freelancerId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        await UpsertAsync(FreelancersCollection, freelancerId, embedding, metadata, ct);
    }

    public async Task UpsertTaskAsync(
        Guid taskId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        await UpsertAsync(TasksCollection, taskId, embedding, metadata, ct);
    }

    private async Task UpsertAsync(
        string collection,
        Guid id,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct)
    {
        var payload = metadata.ToDictionary(
            k => k.Key,
            v => JsonSerializer.SerializeToElement(v.Value));

        var body = new
        {
            points = new[]
            {
                new
                {
                    id = id.ToString(),
                    vector = embedding,
                    payload,
                }
            }
        };

        var resp = await _http.PutAsJsonAsync($"/collections/{collection}/points?wait=true", body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Qdrant upsert failed {Status}: {Body}", resp.StatusCode, errorBody);
            resp.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchFreelancersAsync(
        float[] queryEmbedding,
        int topK = 20,
        float minScore = 0.35f,
        SearchFilter? filter = null,
        CancellationToken ct = default)
    {
        return await SearchAsync(FreelancersCollection, queryEmbedding, topK, minScore, ct);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchTasksAsync(
        float[] queryEmbedding,
        int topK = 20,
        float minScore = 0.35f,
        CancellationToken ct = default)
    {
        return await SearchAsync(TasksCollection, queryEmbedding, topK, minScore, ct);
    }

    private async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collection,
        float[] queryEmbedding,
        int topK,
        float minScore,
        CancellationToken ct)
    {
        var body = new
        {
            vector = queryEmbedding,
            limit = topK,
            score_threshold = (double)minScore,
            with_payload = true,
        };

        var resp = await _http.PostAsJsonAsync($"/collections/{collection}/points/search", body, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = json.GetProperty("result");

        var list = new List<VectorSearchResult>();
        foreach (var item in results.EnumerateArray())
        {
            var id = item.GetProperty("id").GetString() ?? string.Empty;
            var score = item.GetProperty("score").GetDouble();
            var payload = new Dictionary<string, string>();
            if (item.TryGetProperty("payload", out var p))
            {
                foreach (var prop in p.EnumerateObject())
                {
                    payload[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
            list.Add(new VectorSearchResult(Guid.Parse(id), (float)score, payload));
        }
        return list;
    }
}
