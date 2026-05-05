using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.Conversations;

public enum AIWorkflowStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Failed
}

public enum AIWorkflowTriggerType
{
    Manual,
    Scheduled,
    EventBased,
    Webhook
}

public class AIWorkflow : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AIWorkflowStatus Status { get; private set; }
    
    // Trigger configuration
    public AIWorkflowTriggerType TriggerType { get; private set; }
    public string? TriggerConfig { get; private set; } // JSON config for trigger
    
    // Workflow steps
    public List<AIWorkflowStep> Steps { get; private set; } = new();
    
    // Execution tracking
    public int ExecutionCount { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public DateTimeOffset? LastExecutedAt { get; private set; }
    
    // Scheduling
    public string? CronExpression { get; private set; }
    public DateTimeOffset? NextExecutionAt { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AIWorkflow() { }

    public static AIWorkflow Create(
        Guid userId,
        string name,
        string description,
        AIWorkflowTriggerType triggerType)
    {
        return new AIWorkflow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            Status = AIWorkflowStatus.Draft,
            TriggerType = triggerType,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddStep(AIWorkflowStep step)
    {
        Steps.Add(step);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveStep(Guid stepId)
    {
        Steps.RemoveAll(s => s.Id == stepId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Status = AIWorkflowStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Pause()
    {
        Status = AIWorkflowStatus.Paused;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        Status = AIWorkflowStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fail()
    {
        Status = AIWorkflowStatus.Failed;
        FailureCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordExecution(bool success)
    {
        ExecutionCount++;
        if (success)
            SuccessCount++;
        else
            FailureCount++;
        LastExecutedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSchedule(string cronExpression, DateTimeOffset nextExecution)
    {
        CronExpression = cronExpression;
        NextExecutionAt = nextExecution;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public class AIWorkflowStep : Entity<Guid>
{
    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    
    // Step type
    public string Type { get; private set; } = string.Empty; // "ai_call", "code_execution", "data_processing", "notification", etc.
    
    // Step configuration
    public string Config { get; private set; } = string.Empty; // JSON config
    
    // Execution order
    public int Order { get; private set; }
    
    // Dependencies
    public List<Guid> DependsOnStepIds { get; private set; } = new();
    
    // Execution tracking
    public int ExecutionCount { get; private set; }
    public int SuccessCount { get; private set; }
    public TimeSpan? AverageExecutionTime { get; private set; }

    private AIWorkflowStep() { }

    public static AIWorkflowStep Create(
        Guid workflowId,
        string name,
        string type,
        string config,
        int order)
    {
        return new AIWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Name = name,
            Type = type,
            Config = config,
            Order = order
        };
    }

    public void AddDependency(Guid stepId)
    {
        if (!DependsOnStepIds.Contains(stepId))
            DependsOnStepIds.Add(stepId);
    }

    public void RecordExecution(bool success, TimeSpan? executionTime)
    {
        ExecutionCount++;
        if (success)
            SuccessCount++;
        
        if (executionTime.HasValue)
        {
            if (AverageExecutionTime.HasValue)
                AverageExecutionTime = TimeSpan.FromTicks((long)((AverageExecutionTime.Value.Ticks * (ExecutionCount - 1) + executionTime.Value.Ticks) / ExecutionCount));
            else
                AverageExecutionTime = executionTime;
        }
    }
}
