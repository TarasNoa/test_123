namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Interface for autonomous agents that can execute tasks
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Execute the agent with the given context
    /// </summary>
    Task<AgentResult> ExecuteAsync(AgentContext context);
}

/// <summary>
/// Agent execution context
/// </summary>
public class AgentContext
{
    public string ApplicationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TechStack { get; set; } = string.Empty;
    public GeneratedFile[]? GeneratedFiles { get; set; }
    /// <summary>When set, the model must return only these repo-relative paths (incremental generation).</summary>
    public string[] TargetRelativePaths { get; set; } = Array.Empty<string>();
    /// <summary>Restrict output to <see cref="TargetRelativePaths"/>; do not delegate nested subagents.</summary>
    public bool ScopedOutputOnly { get; set; }
    /// <summary>All planned paths in the current phase (navigation only — do not emit these unless assigned as target).</summary>
    public string[] PlannedPhasePaths { get; set; } = Array.Empty<string>();
    public string? Feedback { get; set; }
    public AgentTask? Task { get; set; }

    public AgentContext() { }

    public AgentContext(AgentTask task, AgentResult? result = null)
    {
        Task = task;
        Feedback = result?.Content;
    }

    public AgentContext(AgentTask task, string? feedback)
    {
        Task = task;
        Feedback = feedback;
    }
}

/// <summary>
/// Agent task
/// </summary>
public class AgentTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ParentTaskId { get; set; }
    public string Description { get; set; } = string.Empty;
    public AgentContext Context { get; set; } = new();
    public List<AgentTask> Subtasks { get; set; } = new();
    /// <summary>When set via @subagent directive, routes task to YAML agent spec.</summary>
    public string? SubagentSpecName { get; set; }
}

/// <summary>
/// Agent execution result
/// </summary>
public class AgentResult
{
    public bool IsSuccess { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public List<AgentTask>? SuggestedSubtasks { get; set; }
    public object? DatabaseDesign { get; set; }
    public object? CICDPipeline { get; set; }
    public object? PerformanceProfile { get; set; }
    public object? TechDebt { get; set; }
    public object? Observability { get; set; }
    public bool IsApproved => IsSuccess;
}

/// <summary>
/// Generated file representation
/// </summary>
public class GeneratedFile
{
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
