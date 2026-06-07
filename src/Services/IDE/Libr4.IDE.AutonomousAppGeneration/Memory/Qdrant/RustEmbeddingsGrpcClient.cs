using Grpc.Net.Client;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant.Grpc;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

/// <summary>Wave 3.4: Rust libr4-embeddings gRPC client for Hermes vector sync.</summary>
public sealed class RustEmbeddingsGrpcClient : IEmbeddingService, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly EmbeddingService.EmbeddingServiceClient _client;
    private readonly MemoryEmbeddingOptions _options;
    private readonly ILogger<RustEmbeddingsGrpcClient> _logger;

    public RustEmbeddingsGrpcClient(
        IOptions<QdrantSyncOptions> options,
        ILogger<RustEmbeddingsGrpcClient> logger)
    {
        _options = options.Value.Embeddings;
        _logger = logger;
        _channel = GrpcChannel.ForAddress(_options.GrpcAddress);
        _client = new EmbeddingService.EmbeddingServiceClient(_channel);
    }

    public int Dimensions => _options.Dimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var response = await _client.EmbedAsync(new EmbedRequest
        {
            Text = TruncateText(text),
            Model = EmbeddingModel.MultilingualE5Small,
            Normalize = true
        }, cancellationToken: ct).ConfigureAwait(false);

        return response.Embedding.ToArray();
    }

    public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var response = await _client.EmbedBatchAsync(new EmbedBatchRequest
        {
            Texts = { texts.Select(TruncateText) },
            Model = EmbeddingModel.MultilingualE5Small,
            Normalize = true,
            BatchSize = Math.Min(32, Math.Max(1, texts.Count))
        }, cancellationToken: ct).ConfigureAwait(false);

        return response.Embeddings
            .Select(e => e.Embedding.ToArray())
            .ToArray();
    }

    public void Dispose() => _channel.Dispose();

    private static string TruncateText(string text, int maxChars = 2000) =>
        text.Length <= maxChars ? text : text[..maxChars];
}
