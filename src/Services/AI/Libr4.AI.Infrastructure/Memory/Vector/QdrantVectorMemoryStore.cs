/*
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Libr4.AI.Infrastructure.Memory.Vector;

/// <summary>
/// Production-ready vector memory store using Qdrant.
/// Supports semantic search with cosine similarity.
/// </summary>
public sealed class QdrantVectorMemoryStore : IVectorMemoryStore, IAsyncDisposable
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorMemoryStore> _logger;
    private readonly string _defaultCollection;
    private readonly int _vectorSize;

    public QdrantVectorMemoryStore(
        string host,
        int port,
        string apiKey,
        ILogger<QdrantVectorMemoryStore> logger,
        string defaultCollection = "libr4_memory",
        int vectorSize = 1536)
    {
        _logger = logger;
        _defaultCollection = defaultCollection;
        _vectorSize = vectorSize;

        var address = $"http://{host}:{port}";
        _client = string.IsNullOrEmpty(apiKey)
            ? new QdrantClient(host, port)
            : new QdrantClient(host, port, apiKey: apiKey);
    }

    /// <summary>
    /// Creates default collection if it doesn't exist.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            var collections = await _client.ListCollectionsAsync(ct);
            if (!collections.Contains(_defaultCollection))
            {
                _logger.LogInformation("Creating Qdrant collection: {Collection}", _defaultCollection);
                await _client.CreateCollectionAsync(
                    _defaultCollection,
                    new VectorParams
                    {
                        Size = (ulong)_vectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Qdrant collection");
            throw;
        }
    }

    public async Task UpsertAsync(VectorRecord record, CancellationToken ct = default)
    {
        var collectionId = record.CollectionId ?? _defaultCollection;
        
        // Ensure collection exists
        await EnsureCollectionAsync(collectionId, ct);

        var point = new PointStruct
        {
            Id = new PointId { Uuid = record.Id },
            Vectors = new Vectors { Vector = new Vector { Data = { record.Embedding } } },
            Payload =
            {
                ["text"] = record.Text,
                ["collection_id"] = collectionId
            }
        };

        // Add metadata if present
        if (record.Metadata != null)
        {
            foreach (var (key, value) in record.Metadata)
            {
                point.Payload[key] = value;
            }
        }

        await _client.UpsertAsync(collectionId, new[] { point }, cancellationToken: ct);
        _logger.LogDebug("Upserted vector record {Id} to collection {Collection}", record.Id, collectionId);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        string? collectionId = null,
        int topK = 10,
        double minScore = 0.0,
        CancellationToken ct = default)
    {
        var targetCollection = collectionId ?? _defaultCollection;
        
        var searchResult = await _client.QueryAsync(
            targetCollection,
            queryEmbedding,
            limit: (ulong)topK,
            scoreThreshold: minScore,
            cancellationToken: ct);

        var results = new List<VectorSearchResult>();
        
        foreach (var scoredPoint in searchResult)
        {
            var record = new VectorRecord(
                Id: scoredPoint.Id.Uuid,
                CollectionId: scoredPoint.Payload.TryGetValue("collection_id", out var colValue) 
                    ? colValue.StringValue 
                    : targetCollection,
                Embedding: scoredPoint.Vectors.Vector.Data.ToArray(),
                Text: scoredPoint.Payload.TryGetValue("text", out var textValue) 
                    ? textValue.StringValue 
                    : "",
                Metadata: scoredPoint.Payload
                    .Where(p => p.Key != "text" && p.Key != "collection_id")
                    .ToDictionary(p => p.Key, p => p.Value.StringValue) as IReadOnlyDictionary<string, string>
            );

            results.Add(new VectorSearchResult(record, scoredPoint.Score));
        }

        return results;
    }

    public async Task DeleteCollectionAsync(string collectionId, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteCollectionAsync(collectionId, cancellationToken: ct);
            _logger.LogInformation("Deleted Qdrant collection: {Collection}", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Qdrant collection: {Collection}", collectionId);
            throw;
        }
    }

    public async Task DeleteAsync(string id, string? collectionId = null, CancellationToken ct = default)
    {
        var targetCollection = collectionId ?? _defaultCollection;
        await _client.DeleteAsync(targetCollection, new[] { new PointId { Uuid = id } }, cancellationToken: ct);
        _logger.LogDebug("Deleted vector record {Id} from collection {Collection}", id, targetCollection);
    }

    private async Task EnsureCollectionAsync(string collectionId, CancellationToken ct)
    {
        if (collectionId == _defaultCollection) return;

        var collections = await _client.ListCollectionsAsync(ct);
        if (!collections.Contains(collectionId))
        {
            _logger.LogInformation("Creating Qdrant collection: {Collection}", collectionId);
            await _client.CreateCollectionAsync(
                collectionId,
                new VectorParams
                {
                    Size = (ulong)_vectorSize,
                    Distance = Distance.Cosine
                },
                cancellationToken: ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// Configuration options for Qdrant vector store.
/// </summary>
public sealed record QdrantOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 6334;
    public string? ApiKey { get; init; }
    public string DefaultCollection { get; init; } = "libr4_memory";
    public int VectorSize { get; init; } = 1536;
    public bool UseGrpc { get; init; } = true;
}
*/
