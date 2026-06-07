namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public sealed class FlowDefinitionDocument
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? Description { get; set; }
    public List<FlowNodeDocument> Nodes { get; set; } = new();
    public List<FlowEdgeDocument> Edges { get; set; } = new();
}

public sealed class FlowNodeDocument
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "stage";
    public string? Stage { get; set; }
    public string? Phase { get; set; }
    public List<FlowPreconditionDocument> Preconditions { get; set; } = new();
    public int? MaxRetries { get; set; }
}

public sealed class FlowEdgeDocument
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string On { get; set; } = "success";
    public string? Action { get; set; }
    public int? MaxRetries { get; set; }
}

public sealed class FlowPreconditionDocument
{
    public string Kind { get; set; } = string.Empty;
    public List<string> Paths { get; set; } = new();
}

public sealed class FlowDefinition
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<FlowNode> Nodes { get; init; } = Array.Empty<FlowNode>();
    public IReadOnlyList<FlowEdge> Edges { get; init; } = Array.Empty<FlowEdge>();
}

public sealed class FlowNode
{
    public required string Id { get; init; }
    public FlowNodeType Type { get; init; } = FlowNodeType.Stage;
    public string? Stage { get; init; }
    public string? Phase { get; init; }
    public IReadOnlyList<FlowPrecondition> Preconditions { get; init; } = Array.Empty<FlowPrecondition>();
    public int MaxRetries { get; init; } = 1;
}

public enum FlowNodeType
{
    Stage,
    Gate,
    Parallel,
    Retry,
    Escalate
}

public sealed class FlowEdge
{
    public required string From { get; init; }
    public required string To { get; init; }
    public FlowEdgeOutcome On { get; init; } = FlowEdgeOutcome.Success;
    public FlowFailureAction? Action { get; init; }
    public int MaxRetries { get; init; } = 1;
}

public enum FlowEdgeOutcome
{
    Success,
    Failure
}

public enum FlowFailureAction
{
    Retry,
    Skip,
    Escalate,
    Abort
}

public sealed class FlowPrecondition
{
    public required string Kind { get; init; }
    public IReadOnlyList<string> Paths { get; init; } = Array.Empty<string>();
}

public sealed class FlowRuntimeContext
{
    public IReadOnlyList<string> WorkspaceFiles { get; init; } = Array.Empty<string>();
    public bool TestsPassed { get; init; }
    public bool VerifyPassed { get; init; }
}

public sealed record FlowProgress(
    Guid RunId,
    string FlowName,
    string? CurrentNodeId,
    string Status,
    IReadOnlyList<FlowNodeProgress> Nodes,
    DateTime UpdatedAtUtc);

public sealed record FlowNodeProgress(
    string NodeId,
    string Status = "pending",
    int Attempts = 0,
    string? LastError = null);

public sealed class FlowAdvanceResult
{
    public bool ShouldContinue { get; init; } = true;
    public bool ShouldAbort { get; init; }
    public string? NextNodeId { get; init; }
    public FlowFailureAction? RoutedAction { get; init; }
    public string? Message { get; init; }

    public static FlowAdvanceResult Ok(string? nextNodeId = null, string? message = null) =>
        new() { NextNodeId = nextNodeId, Message = message };

    public static FlowAdvanceResult Abort(string message) =>
        new() { ShouldContinue = false, ShouldAbort = true, Message = message };
}

public sealed class FlowEngineOptions
{
    public string FlowsDirectory { get; set; } = "Flows";
    public string RunsRoot { get; set; } = ".logs/runs";
    public bool EnableFlowOrchestration { get; set; } = true;
}
