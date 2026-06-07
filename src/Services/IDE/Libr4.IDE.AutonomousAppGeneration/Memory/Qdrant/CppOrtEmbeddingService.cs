using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.Tokenizers;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

/// <summary>
/// Wave 6.2: local ONNX embeddings via C++ ORT Direct EP (CPU / CUDA / DirectML when available).
/// Requires <see cref="MemoryEmbeddingOptions.OnnxModelPath"/> and <see cref="MemoryEmbeddingOptions.TokenizerPath"/>.
/// </summary>
public sealed class CppOrtEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly MemoryEmbeddingOptions _options;
    private readonly ILogger<CppOrtEmbeddingService> _logger;
    private readonly object _initLock = new();
    private Tokenizer? _tokenizer;
    private IntPtr _session;
    private int _dimensions;
    private bool _initialized;
    private bool _initFailed;

    public CppOrtEmbeddingService(
        IOptions<QdrantSyncOptions> options,
        ILogger<CppOrtEmbeddingService> logger)
    {
        _options = options.Value.Embeddings;
        _logger = logger;
        _dimensions = _options.Dimensions;
    }

    public int Dimensions => _dimensions;

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!EnsureReady())
            throw new InvalidOperationException("CppOrtEmbeddingService is not available");

        BuildBatch([text], out var seqLen, out var inputIds, out var mask, out var types);
        if (!CppOrtEpBridge.TryBertEmbed(
                _session,
                inputIds,
                mask,
                types,
                batch: 1,
                seqLen,
                _logger,
                out var embeddings,
                out var hidden))
        {
            throw new InvalidOperationException("ONNX BERT embed failed");
        }

        _dimensions = hidden;
        return Task.FromResult(ExtractRow(embeddings, hidden, 0));
    }

    public async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!EnsureReady())
            throw new InvalidOperationException("CppOrtEmbeddingService is not available");

        var results = new float[texts.Count][];
        const int chunkSize = 8;
        for (var offset = 0; offset < texts.Count; offset += chunkSize)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = texts.Skip(offset).Take(chunkSize).ToArray();
            BuildBatch(chunk, out var seqLen, out var inputIds, out var mask, out var types);

            if (!CppOrtEpBridge.TryBertEmbed(
                    _session,
                    inputIds,
                    mask,
                    types,
                    chunk.Length,
                    seqLen,
                    _logger,
                    out var embeddings,
                    out var hidden))
            {
                throw new InvalidOperationException("ONNX BERT batch embed failed");
            }

            _dimensions = hidden;
            for (var i = 0; i < chunk.Length; i++)
                results[offset + i] = ExtractRow(embeddings, hidden, i);
        }

        return results;
    }

    public void Dispose()
    {
        if (_session != IntPtr.Zero)
        {
            CppOrtEpBridge.DestroySession(_session);
            _session = IntPtr.Zero;
        }
    }

    private bool EnsureReady()
    {
        if (_initialized)
            return true;
        if (_initFailed)
            return false;

        lock (_initLock)
        {
            if (_initialized)
                return true;
            if (_initFailed)
                return false;

            if (!CppOrtEpBridge.IsAvailable)
            {
                _initFailed = true;
                _logger.LogWarning("[CppOrtEp] libr4_ort_ep native library unavailable");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_options.OnnxModelPath)
                || !File.Exists(_options.OnnxModelPath))
            {
                _initFailed = true;
                _logger.LogWarning("[CppOrtEp] ONNX model path missing: {Path}", _options.OnnxModelPath);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_options.TokenizerPath)
                || !File.Exists(_options.TokenizerPath))
            {
                _initFailed = true;
                _logger.LogWarning("[CppOrtEp] vocab.txt missing: {Path}", _options.TokenizerPath);
                return false;
            }

            try
            {
                _tokenizer = BertTokenizer.Create(_options.TokenizerPath);

                if (!CppOrtEpBridge.TryCreateSession(
                        _options.OnnxModelPath,
                        _options.OrtExecutionProvider,
                        _logger,
                        out _session))
                {
                    _initFailed = true;
                    return false;
                }

                if (CppOrtEpBridge.TryListProviders(_logger, out var providers))
                {
                    _logger.LogInformation(
                        "[CppOrtEp] ORT providers: {Providers}; using EP preference {Ep}",
                        string.Join(", ", providers),
                        _options.OrtExecutionProvider);
                }

                _initialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _initFailed = true;
                _logger.LogWarning(ex, "[CppOrtEp] initialization failed");
                return false;
            }
        }
    }

    private void BuildBatch(
        IReadOnlyList<string> texts,
        out int seqLen,
        out long[] inputIds,
        out long[] attentionMask,
        out long[] tokenTypeIds)
    {
        if (_tokenizer is null)
            throw new InvalidOperationException("Tokenizer not initialized");

        seqLen = Math.Clamp(_options.MaxSequenceLength, 8, 512);
        var batch = texts.Count;
        inputIds = new long[batch * seqLen];
        attentionMask = new long[batch * seqLen];
        tokenTypeIds = new long[batch * seqLen];

        for (var b = 0; b < batch; b++)
        {
            var ids = _tokenizer.EncodeToIds(texts[b]);
            var length = Math.Min(ids.Count, seqLen);
            for (var i = 0; i < length; i++)
            {
                var idx = b * seqLen + i;
                inputIds[idx] = ids[i];
                attentionMask[idx] = 1;
                tokenTypeIds[idx] = 0;
            }
        }

    }

    private static float[] ExtractRow(float[] batchEmbeddings, int hidden, int row)
    {
        var vec = new float[hidden];
        Array.Copy(batchEmbeddings, row * hidden, vec, 0, hidden);
        return vec;
    }
}
