namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public enum McpExecutionLaneKind
{
    Internal = 0,
    Browser = 1,
    N8n = 2,
    Workflow = 3,
}

public enum McpToolRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}

public enum MemoryKind
{
    Episodic = 0,
    Semantic = 1,
    Procedural = 2,
}

public enum AgentTaskState
{
    Pending = 0,
    Ready = 1,
    Running = 2,
    Done = 3,
    Failed = 4,
    Blocked = 5,
}
