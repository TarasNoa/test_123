namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class PostRunExtractionOptions
{
    public bool Enabled { get; set; } = true;

    public bool UseLlmExtractor { get; set; } = true;

    public int MaxRolloutLines { get; set; } = 48;

    public int MaxRolloutCharsPerLine { get; set; } = 1_200;

    public int MaxLessonsPerRun { get; set; } = 12;

    public int QueueCapacity { get; set; } = 64;
}
