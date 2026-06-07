namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public sealed class QdrantSyncOptions
{
    /// <summary>Opt-in L2 vector index. Production default: false (FTS-only until explicitly enabled).</summary>
    public bool UseQdrantSync { get; set; } = false;

    public string CollectionId { get; set; } = "hermes-memory-l2";

    public QdrantConnectionOptions Qdrant { get; set; } = new();

    public MemoryEmbeddingOptions Embeddings { get; set; } = new();

    public int BackfillBatchSize { get; set; } = 200;

    public int HybridSearchCandidateMultiplier { get; set; } = 3;
}

public sealed class QdrantConnectionOptions
{
    public string BaseUrl { get; set; } = "http://localhost:6333";

    public string? ApiKey { get; set; }
}

public sealed class MemoryEmbeddingOptions
{
    /// <summary>ollama | grpc | ort-cpp</summary>
    public string Provider { get; set; } = "ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string GrpcAddress { get; set; } = "http://localhost:50061";

    public string Model { get; set; } = "nomic-embed-text";

    public int Dimensions { get; set; } = 384;

    /// <summary>Wave 6.2: path to ONNX embedding model (e.g. all-MiniLM-L6-v2).</summary>
    public string? OnnxModelPath { get; set; }

    /// <summary>WordPiece vocab.txt for BERT-style ONNX models.</summary>
    public string? TokenizerPath { get; set; }

    /// <summary>ORT EP preference: cpu | cuda | dml | directml.</summary>
    public string OrtExecutionProvider { get; set; } = "cpu";

    public int MaxSequenceLength { get; set; } = 256;
}
