using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIOrchestrator;

public enum WorkflowStatus { Pending, Running, Paused, Completed, Failed }
public enum StepStatus { Pending, Running, Completed, Failed, Skipped }
public enum StepType { AICall, ToolCall, Decision, Parallel, Sequential, Loop }

public class WorkflowStep
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public StepType Type { get; set; }
    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object>? Input { get; set; }
    public Dictionary<string, object>? Output { get; set; }
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public bool CanRetry => Status == StepStatus.Failed && RetryCount < MaxRetries;
}

public class Workflow
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public int CurrentStepIndex { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public Dictionary<string, object>? InputData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    public string? ErrorMessage { get; set; }
    public float ProgressPercentage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public WorkflowStep? CurrentStep => CurrentStepIndex < Steps.Count ? Steps[CurrentStepIndex] : null;

    public void Start(DateTimeOffset now) { Status = WorkflowStatus.Running; StartedAt = now; }
    public void Complete(DateTimeOffset now) { Status = WorkflowStatus.Completed; CompletedAt = now; ProgressPercentage = 100; }
    public void Fail(string error, DateTimeOffset now) { Status = WorkflowStatus.Failed; ErrorMessage = error; CompletedAt = now; }
    public void AdvanceStep()
    {
        CurrentStepIndex++;
        if (Steps.Count > 0) ProgressPercentage = Math.Min(100, (float)CurrentStepIndex / Steps.Count * 100);
    }
}

public class AgentCoordination
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public List<Guid> AgentIds { get; set; } = new List<Guid>();
    public string CoordinationStrategy { get; set; } = "sequential"; // sequential, parallel, hierarchical
    public Dictionary<string, object> SharedContext { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; set; }
}
