using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.IDEAIAgent;

public enum AgentType { Planner, Coder, Reviewer, Debugger, Tester, Documentation }
public enum AgentStatus { Idle, Planning, Executing, WaitingConfirmation, Completed, Failed }
public enum ToolType { FileOps, Search, CodeExec, Terminal, Browser, Git, Linter }
public enum TaskComplexity { Simple, Medium, Complex, Multi }

public class AIAgent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AgentType Type { get; set; }
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    public string SystemPrompt { get; set; } = string.Empty;
    public List<AgentTool> Tools { get; set; } = new List<AgentTool>();
    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public float SuccessRate => (SuccessCount + FailureCount) > 0 ? (float)SuccessCount / (SuccessCount + FailureCount) * 100 : 0;

    // Domain methods
    public void SetStatus(AgentStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSuccess()
    {
        SuccessCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailure()
    {
        FailureCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddTool(AgentTool tool)
    {
        Tools.Add(tool);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveTool(Guid toolId)
    {
        Tools.RemoveAll(t => t.Id == toolId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateConfig(string key, object value)
    {
        Config[key] = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class AgentTool
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ToolType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Schema { get; set; } = new Dictionary<string, object>();
    public bool RequiresConfirmation { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class AgentSession
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid UserId { get; set; }
    public string Goal { get; set; } = string.Empty;
    public TaskComplexity Complexity { get; set; }
    public List<AgentStep> Steps { get; set; } = new List<AgentStep>();
    public int CurrentStep { get; set; }
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    public string? FinalResult { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
    public int ToolCallsCount { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue ? CompletedAt - StartedAt : null;

    // Domain methods
    public void Start()
    {
        Status = AgentStatus.Executing;
        StartedAt = DateTimeOffset.UtcNow;
        CurrentStep = 0;
    }

    public void AddStep(AgentStep step)
    {
        Steps.Add(step);
    }

    public void AdvanceStep()
    {
        if (CurrentStep < Steps.Count - 1)
        {
            CurrentStep++;
        }
    }

    public void Complete(string result)
    {
        Status = AgentStatus.Completed;
        FinalResult = result;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = AgentStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void WaitForConfirmation()
    {
        Status = AgentStatus.WaitingConfirmation;
    }

    public void RecordToolCall()
    {
        ToolCallsCount++;
    }

    public void RecordTokensUsed(int tokens)
    {
        TokensUsed += tokens;
    }

    public void AddContext(string key, object value)
    {
        Context[key] = value;
    }
}

public class AgentStep
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int Order { get; set; }
    public string Thought { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> ActionInput { get; set; } = new Dictionary<string, object>();
    public string? Observation { get; set; }
    public Dictionary<string, object>? ToolResult { get; set; }
    public bool WasApproved { get; set; }
    public bool WasExecuted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }

    // Domain methods
    public void Approve()
    {
        WasApproved = true;
    }

    public void Execute(Dictionary<string, object> result)
    {
        WasExecuted = true;
        ToolResult = result;
        ExecutedAt = DateTimeOffset.UtcNow;
    }

    public void SetObservation(string observation)
    {
        Observation = observation;
    }
}

public class AgentPlan
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<Dictionary<string, object>> TaskList { get; set; } = new List<Dictionary<string, object>>();
    public Dictionary<string, object> EstimatedResources { get; set; } = new Dictionary<string, object>();
    public bool WasApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Domain methods
    public void AddTask(Dictionary<string, object> task)
    {
        TaskList.Add(task);
    }

    public void Approve()
    {
        WasApproved = true;
    }

    public void SetEstimatedResources(string resource, object value)
    {
        EstimatedResources[resource] = value;
    }

    public int EstimatedSteps => TaskList.Count;
}

public class Agent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] Tools { get; set; } = Array.Empty<string>();
}
