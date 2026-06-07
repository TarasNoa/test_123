using System.Net.Sockets;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Wave 3.4 smoke tests against a running libr4-embeddings gRPC service.
/// Skips when <c>LIBR4_EMBEDDINGS_GRPC</c> endpoint is unreachable (default CI path).
/// </summary>
public sealed class RustEmbeddingsGrpcClientIntegrationTests : IDisposable
{
    private readonly RustEmbeddingsGrpcClient? _client;

    public RustEmbeddingsGrpcClientIntegrationTests()
    {
        if (!TryResolveGrpcEndpoint(out var address))
            return;

        _client = new RustEmbeddingsGrpcClient(
            Options.Create(new QdrantSyncOptions
            {
                Embeddings = new MemoryEmbeddingOptions
                {
                    Provider = "grpc",
                    GrpcAddress = address,
                    Dimensions = 384
                }
            }),
            NullLogger<RustEmbeddingsGrpcClient>.Instance);
    }

    public void Dispose() => _client?.Dispose();

    [Fact]
    public void Client_WhenEndpointUnreachable_IsSkippedInPositiveTests()
    {
        if (_client is not null)
            return;

        TryResolveGrpcEndpoint(out _).Should().BeFalse(
            "set LIBR4_EMBEDDINGS_GRPC=http://host:50061 to run live embeddings smoke tests");
    }

    [Fact]
    public async Task EmbedAsync_WhenServiceAvailable_ReturnsNonEmptyVector()
    {
        if (_client is null)
            return;

        var vector = await _client.EmbedAsync("libr4 embeddings smoke test");

        vector.Should().NotBeNullOrEmpty();
        vector.Length.Should().Be(_client.Dimensions);
        vector.Should().Contain(v => Math.Abs(v) > 1e-6f);
    }

    [Fact]
    public async Task EmbedBatchAsync_WhenServiceAvailable_ReturnsAlignedBatch()
    {
        if (_client is null)
            return;

        var texts = new[] { "first chunk", "second chunk" };
        var batch = await _client.EmbedBatchAsync(texts);

        batch.Should().HaveCount(texts.Length);
        batch.Should().AllSatisfy(v =>
        {
            v.Should().NotBeNullOrEmpty();
            v.Length.Should().Be(_client.Dimensions);
        });
    }

    private static bool TryResolveGrpcEndpoint(out string address)
    {
        address = Environment.GetEnvironmentVariable("LIBR4_EMBEDDINGS_GRPC")
            ?? new MemoryEmbeddingOptions().GrpcAddress;

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            return false;

        var port = uri.Port > 0 ? uri.Port : 50061;
        var host = uri.Host;

        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(host, port);
            return connect.Wait(TimeSpan.FromSeconds(2)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
