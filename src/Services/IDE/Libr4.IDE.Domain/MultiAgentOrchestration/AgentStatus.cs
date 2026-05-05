namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Agent status enum
/// </summary>
public enum AgentStatus
{
    Idle,
    Thinking,
    Working,
    Waiting,
    Blocked,
    Completed,
    Failed
}
