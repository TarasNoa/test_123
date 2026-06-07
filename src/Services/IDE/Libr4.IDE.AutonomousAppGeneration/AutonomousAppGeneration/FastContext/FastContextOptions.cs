namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class FastContextOptions
{
    public const string SectionName = "AutonomousAppGeneration:FastContext";

    public string RunsRoot { get; set; } = ".logs/runs";
    public int MaxSnippetLines { get; set; } = 12;
    public int MaxSnippetChars { get; set; } = 4000;
    public double RipgrepWeight { get; set; } = 1.0;
    public double GraphWeight { get; set; } = 0.6;
    public double PathHeuristicWeight { get; set; } = 0.3;
    public int RrfK { get; set; } = 60;
    public bool Enabled { get; set; } = true;
    public int MaxPrefetchQueries { get; set; } = 3;
    public int MaxPrefetchHits { get; set; } = 8;
    public double MinConfidenceForContextPack { get; set; } = 0.7;
    public bool EnableEmbeddingIndex { get; set; } = true;
    public int EmbeddingMinChunkLines { get; set; } = 40;
    public int EmbeddingMaxChunkLines { get; set; } = 80;
    public double EmbeddingMinScore { get; set; } = 0.25;
    public int EmbeddingMaxFilesPerIndex { get; set; } = 500;
}
