using Libr4.Matching.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Libr4.Matching.Infrastructure.VectorStore;

public sealed class QdrantVectorIndex : IVectorIndex
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorIndex> _logger;

    private const string FreelancersCollection = "freelancers";
    private const string TasksCollection = "tasks";
    private const uint VectorDimension = 384;

    public QdrantVectorIndex(QdrantClient client, ILogger<QdrantVectorIndex> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnsureCollectionsAsync(CancellationToken ct = default)
    {
        var existing = await _client.ListCollectionsAsync(ct);
        foreach (var name in new[] { FreelancersCollection, TasksCollection })
        {
            if (!existing.Any(c => c == name))
            {
                await _client.CreateCollectionAsync(name, new VectorParams
                {
                    Size = VectorDimension,
                    Distance = Distance.Cosine,
                    OnDisk = true,
                }, cancellationToken: ct);
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
        var payload = metadata.ToDictionary(
            k => k.Key,
            v => new Value { StringValue = v.Value?.ToString() ?? string.Empty });

        await _client.UpsertAsync(FreelancersCollection, new[]
        {
            new PointStruct
            {
                Id = new PointId { Uuid = freelancerId.ToString() },
                Vectors = embedding,
                Payload = { payload },
            }
        }, cancellationToken: ct);
    }

    public async Task UpsertTaskAsync(
        Guid taskId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default)
    {
        var payload = metadata.ToDictionary(
            k => k.Key,
            v => new Value { StringValue = v.Value?.ToString() ?? string.Empty });

        await _client.UpsertAsync(TasksCollection, new[]
        {
            new PointStruct
            {
                Id = new PointId { Uuid = taskId.ToString() },
                Vectors = embedding,
                Payload = { payload },
            }
        }, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchFreelancersAsync(
        float[] queryEmbedding,
        int topK = 20,
        float minScore = 0.35f,
        SearchFilter? filter = null,
        CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(
            FreelancersCollection,
            queryEmbedding,
            limit: (ulong)topK,
            scoreThreshold: minScore,
            withPayload: true,
            cancellationToken: ct);

        return results.Select(r => new VectorSearchResult(
            Id: Guid.Parse(r.Id.Uuid),
            Score: r.Score,
            Payload: r.Payload.ToDictionary(
                k => k.Key,
                v => v.Value.StringValue)
        )).ToList();
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchTasksAsync(
        float[] queryEmbedding,
        int topK = 20,
        float minScore = 0.35f,
        CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(
            TasksCollection,
            queryEmbedding,
            limit: (ulong)topK,
            scoreThreshold: minScore,
            withPayload: true,
            cancellationToken: ct);

        return results.Select(r => new VectorSearchResult(
            Id: Guid.Parse(r.Id.Uuid),
            Score: r.Score,
            Payload: r.Payload.ToDictionary(
                k => k.Key,
                v => v.Value.StringValue)
        )).ToList();
    }
}
