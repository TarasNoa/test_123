namespace Libr4.IDE.Domain.MultiAgentOrchestration;

public enum PhaseStatus
{
    NotStarted,
    InProgress,
    WaitingForGate,
    Completed,
    Failed,
    Skipped
}
