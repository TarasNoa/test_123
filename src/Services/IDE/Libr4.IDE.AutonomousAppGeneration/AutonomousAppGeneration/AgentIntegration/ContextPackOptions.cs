namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class ContextPackOptions
{
    public bool UseRepoGraphOrdering { get; init; } = true;

    public int DefaultMaxChars { get; init; } = 8_000;
    public int PlanningMaxChars { get; init; } = 9_000;
    public int GenerationMaxChars { get; init; } = 16_000;
    public int ConsistencyMaxChars { get; init; } = 7_000;
    public int FixingMaxChars { get; init; } = 16_000;
    public int VerifyMaxChars { get; init; } = 8_000;
    public int BuildMaxChars { get; init; } = 7_000;
    public int ExecutionMaxChars { get; init; } = 7_000;

    public int MaxFilesListed { get; init; } = 40;
    public int MaxRelatedFilesInPack { get; init; } = 24;
    public int MaxRecentErrors { get; init; } = 8;
    public int MemoryTopK { get; init; } = 12;
    public int MaxMemoryItemsInPack { get; init; } = 12;
}

