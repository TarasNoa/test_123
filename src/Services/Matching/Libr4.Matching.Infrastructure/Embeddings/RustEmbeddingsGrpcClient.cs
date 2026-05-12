using Grpc.Net.Client;
using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Infrastructure.Embeddings;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Infrastructure.Embeddings;

public sealed class RustEmbeddingsGrpcClient : IEmbeddingService
{
    private readonly EmbeddingService.EmbeddingServiceClient _client;
    private readonly ILogger<RustEmbeddingsGrpcClient> _logger;

    public RustEmbeddingsGrpcClient(GrpcChannel channel, ILogger<RustEmbeddingsGrpcClient> logger)
    {
        _client = new EmbeddingService.EmbeddingServiceClient(channel);
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var response = await _client.EmbedAsync(new EmbedRequest
        {
            Text = text,
            Model = EmbeddingModel.MultilingualE5Small,
            Normalize = true,
        }, cancellationToken: ct);

        return response.Embedding.ToArray();
    }

    public async Task<float[][]> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var response = await _client.EmbedBatchAsync(new EmbedBatchRequest
        {
            Texts = { texts },
            Model = EmbeddingModel.MultilingualE5Small,
            Normalize = true,
            BatchSize = 32,
        }, cancellationToken: ct);

        return response.Embeddings
            .Select(e => e.Embedding.ToArray())
            .ToArray();
    }

    public async Task<float> SimilarityAsync(
        float[] vectorA,
        float[] vectorB,
        CancellationToken ct = default)
    {
        var response = await _client.SimilarityAsync(new SimilarityRequest
        {
            VectorA = { vectorA },
            VectorB = { vectorB },
        }, cancellationToken: ct);

        return response.CosineSimilarity;
    }
}
