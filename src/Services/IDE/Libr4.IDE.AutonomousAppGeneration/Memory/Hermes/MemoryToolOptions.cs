namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class MemoryToolOptions
{
    public int MaxSummaryChars { get; set; } = 2_000;

    public int MaxKeyChars { get; set; } = 128;

    public int MaxPayloadChars { get; set; } = 8_000;

    public int MaxReadTopK { get; set; } = 16;

    public int MaxWritesPerSession { get; set; } = 32;
}
