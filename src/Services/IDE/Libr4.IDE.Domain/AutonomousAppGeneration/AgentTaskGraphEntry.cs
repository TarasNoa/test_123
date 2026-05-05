namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record AgentTaskGraphEntry(
    string TaskId,
    string Title,
    IReadOnlyList<string> BlockedByTaskIds,
    AgentTaskState State,
    IReadOnlyList<string> EvidencePaths,
    string? Notes);
