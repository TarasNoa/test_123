using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CppOrtEpBridgeSmokeTests
{
    [Fact]
    public void IsAvailable_DoesNotThrow()
    {
        var act = () => _ = CppOrtEpBridge.IsAvailable;
        act.Should().NotThrow();
    }

    [Fact]
    public void TryListProviders_WhenNativePresent_ReturnsCpuProvider()
    {
        if (!CppOrtEpBridge.IsAvailable)
            return;

        var ok = CppOrtEpBridge.TryListProviders(NullLogger.Instance, out var providers);
        ok.Should().BeTrue();
        providers.Should().NotBeEmpty();
        providers.Should().Contain(p => p.Contains("CPU", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CppOrtEmbeddingService_WhenModelPresent_ProducesNormalizedVector()
    {
        var modelPath = Environment.GetEnvironmentVariable("LIBR4_ONNX_MODEL_PATH");
        var vocabPath = Environment.GetEnvironmentVariable("LIBR4_ONNX_VOCAB_PATH");
        if (string.IsNullOrWhiteSpace(modelPath) || string.IsNullOrWhiteSpace(vocabPath))
            return;

        if (!CppOrtEpBridge.IsAvailable)
            return;

        if (!File.Exists(modelPath) || !File.Exists(vocabPath))
            return;

        var options = Options.Create(new QdrantSyncOptions
        {
            Embeddings = new MemoryEmbeddingOptions
            {
                Provider = "ort-cpp",
                OnnxModelPath = modelPath,
                TokenizerPath = vocabPath,
                OrtExecutionProvider = "cpu",
                Dimensions = 384,
                MaxSequenceLength = 128
            }
        });

        using var service = new CppOrtEmbeddingService(
            options,
            NullLogger<CppOrtEmbeddingService>.Instance);

        var vec = await service.EmbedAsync("hello world");
        vec.Should().NotBeNull();
        vec.Length.Should().BeGreaterThan(0);
        vec.Length.Should().Be(service.Dimensions);

        var norm = Math.Sqrt(vec.Sum(v => v * v));
        norm.Should().BeApproximately(1.0, 0.05);
    }
}
