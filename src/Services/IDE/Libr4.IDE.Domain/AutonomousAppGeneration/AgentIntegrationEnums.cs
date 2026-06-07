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
    /// <summary>L0 — turn-level observations.</summary>
    Episodic = 0,
    /// <summary>L2 — distilled facts and stack knowledge.</summary>
    Semantic = 1,
    /// <summary>L1 — repeatable fix/playbook patterns.</summary>
    Procedural = 2,
    /// <summary>L3 — run-level strategy and routing decisions.</summary>
    Strategic = 3,
    /// <summary>L4 — meta-lessons about agent behaviour.</summary>
    Meta = 4,
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
