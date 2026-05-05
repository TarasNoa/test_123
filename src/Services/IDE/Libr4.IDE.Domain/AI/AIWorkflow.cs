using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.IDE.Domain.AI;

public enum WorkflowStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Reflexion cycle status (from context-engineering-kit)
/// </summary>
public enum ReflexionStatus
{
    /// <summary>
    /// No reflection needed
    /// </summary>
    None,
    
    /// <summary>
    /// Reflection in progress
    /// </summary>
    Reflecting,
    
    /// <summary>
    /// Reflection completed with improvements
    /// </summary>
    Improved,
    
    /// <summary>
    /// Reflection found issues requiring fixes
    /// </summary>
    NeedsFix,
    
    /// <summary>
    /// Reflection completed without issues
    /// </summary>
    NoIssues
}

/// <summary>
/// Reflection cycle tracking (from context-engineering-kit Reflexion)
/// </summary>
public class ReflectionCycle
{
    public Guid Id { get; private set; }
    public int StepIndex { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public ReflexionStatus Status { get; private set; }
    public List<string> Findings { get; private set; }
    public List<string> Suggestions { get; private set; }
    public List<string> MemorizedInsights { get; private set; }
    public int CycleNumber { get; private set; }
    
    public ReflectionCycle(int stepIndex, int cycleNumber)
    {
        Id = Guid.NewGuid();
        StepIndex = stepIndex;
        CycleNumber = cycleNumber;
        StartedAt = DateTime.UtcNow;
        Status = ReflexionStatus.Reflecting;
        Findings = new List<string>();
        Suggestions = new List<string>();
        MemorizedInsights = new List<string>();
    }
    
    public void Complete(ReflexionStatus status)
    {
        Status = status;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void AddFinding(string finding)
    {
        Findings.Add(finding);
    }
    
    public void AddSuggestion(string suggestion)
    {
        Suggestions.Add(suggestion);
    }
    
    public void AddMemorizedInsight(string insight)
    {
        MemorizedInsights.Add(insight);
    }
}

public class AIWorkflow : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public List<WorkflowStep> Steps { get; private set; }
    public int CurrentStep { get; private set; }
    public Dictionary<string, object>? InputData { get; private set; }
    public Dictionary<string, object>? OutputData { get; private set; }
    public Dictionary<string, object>? StepResults { get; private set; }
    public WorkflowStatus Status { get; private set; }
    public float ProgressPercentage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Reflection cycles for each step (from context-engineering-kit)
    /// </summary>
    public List<ReflectionCycle> ReflectionCycles { get; private set; }
    
    /// <summary>
    /// Whether reflexion is enabled for this workflow
    /// </summary>
    public bool ReflexionEnabled { get; private set; }
    
    /// <summary>
    /// Maximum number of reflection cycles per step
    /// </summary>
    public int MaxReflectionCycles { get; private set; }
    
    /// <summary>
    /// Isolation context for this workflow (from Archon)
    /// </summary>
    public IsolationContext? Isolation { get; private set; }

    private AIWorkflow() { }

    public static Result<AIWorkflow> Create(
        Guid userId,
        string name,
        List<WorkflowStep> steps,
        string? description = null,
        Dictionary<string, object>? inputData = null,
        bool reflexionEnabled = true,
        int maxReflectionCycles = 3)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<AIWorkflow>(Error.Validation("Workflow.Name.Required", "Workflow name is required"));

        if (steps == null || steps.Count == 0)
            return Result.Failure<AIWorkflow>(Error.Validation("Workflow.Steps.Required", "Workflow must have at least one step"));

        var workflow = new AIWorkflow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            Steps = steps,
            CurrentStep = 0,
            InputData = inputData,
            OutputData = new Dictionary<string, object>(),
            StepResults = new Dictionary<string, object>(),
            Status = WorkflowStatus.Pending,
            ProgressPercentage = 0.0f,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            ReflectionCycles = new List<ReflectionCycle>(),
            ReflexionEnabled = reflexionEnabled,
            MaxReflectionCycles = maxReflectionCycles
        };

        workflow.RaiseDomainEvent(new AIWorkflowCreatedEvent(workflow.Id, userId, name));
        return Result.Success(workflow);
    }

    public Result Start()
    {
        if (Status != WorkflowStatus.Pending)
            return Result.Failure(Error.Validation("Workflow.InvalidStatus", "Workflow is not in pending status"));

        Status = WorkflowStatus.Running;
        StartedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AIWorkflowStartedEvent(Id, UserId));
        return Result.Success();
    }

    public Result CompleteStep(int stepIndex, Dictionary<string, object> stepResult)
    {
        if (Status != WorkflowStatus.Running)
            return Result.Failure(Error.Validation("Workflow.InvalidStatus", "Workflow is not running"));

        if (stepIndex < 0 || stepIndex >= Steps.Count)
            return Result.Failure(Error.Validation("Workflow.InvalidStepIndex", "Invalid step index"));

        if (stepIndex != CurrentStep)
            return Result.Failure(Error.Validation("Workflow.InvalidStepOrder", "Cannot complete step out of order"));

        StepResults[$"step_{stepIndex}"] = stepResult;
        CurrentStep++;
        ProgressPercentage = (float)CurrentStep / Steps.Count * 100;

        if (CurrentStep >= Steps.Count)
        {
            Status = WorkflowStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            RaiseDomainEvent(new AIWorkflowCompletedEvent(Id, UserId));
        }

        return Result.Success();
    }
    
    /// <summary>
    /// Start a reflection cycle for a step (from context-engineering-kit)
    /// </summary>
    public Result StartReflection(int stepIndex)
    {
        if (!ReflexionEnabled)
            return Result.Failure(Error.Validation("Workflow.ReflexionDisabled", "Reflexion is not enabled for this workflow"));
        
        var existingCycles = ReflectionCycles.Count(c => c.StepIndex == stepIndex);
        if (existingCycles >= MaxReflectionCycles)
            return Result.Failure(Error.Validation("Workflow.MaxReflectionCycles", "Maximum reflection cycles exceeded"));
        
        var cycle = new ReflectionCycle(stepIndex, existingCycles + 1);
        ReflectionCycles.Add(cycle);
        RaiseDomainEvent(new AIWorkflowReflectionStartedEvent(Id, UserId, stepIndex, cycle.Id));
        
        return Result.Success();
    }
    
    /// <summary>
    /// Complete a reflection cycle (from context-engineering-kit)
    /// </summary>
    public Result CompleteReflection(Guid reflectionId, ReflexionStatus status, List<string>? findings = null, List<string>? suggestions = null)
    {
        var cycle = ReflectionCycles.FirstOrDefault(c => c.Id == reflectionId);
        if (cycle == null)
            return Result.Failure(Error.Validation("Workflow.ReflectionNotFound", "Reflection cycle not found"));
        
        cycle.Complete(status);
        if (findings != null)
        {
            foreach (var finding in findings)
                cycle.AddFinding(finding);
        }
        if (suggestions != null)
        {
            foreach (var suggestion in suggestions)
                cycle.AddSuggestion(suggestion);
        }
        
        RaiseDomainEvent(new AIWorkflowReflectionCompletedEvent(Id, UserId, reflectionId, status));
        return Result.Success();
    }
    
    /// <summary>
    /// Memorize insights from reflection (from context-engineering-kit)
    /// </summary>
    public Result MemorizeInsights(Guid reflectionId, List<string> insights)
    {
        var cycle = ReflectionCycles.FirstOrDefault(c => c.Id == reflectionId);
        if (cycle == null)
            return Result.Failure(Error.Validation("Workflow.ReflectionNotFound", "Reflection cycle not found"));
        
        foreach (var insight in insights)
            cycle.AddMemorizedInsight(insight);
        
        RaiseDomainEvent(new AIWorkflowInsightsMemorizedEvent(Id, UserId, reflectionId, insights));
        return Result.Success();
    }

    public Result Fail(string errorMessage)
    {
        Status = WorkflowStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AIWorkflowFailedEvent(Id, UserId, errorMessage));
        return Result.Success();
    }

    public Result Retry()
    {
        if (Status != WorkflowStatus.Failed)
            return Result.Failure(Error.Validation("Workflow.InvalidStatus", "Can only retry failed workflows"));

        if (RetryCount >= 3)
            return Result.Failure(Error.Validation("Workflow.MaxRetriesExceeded", "Maximum retry attempts exceeded"));

        RetryCount++;
        Status = WorkflowStatus.Pending;
        CurrentStep = 0;
        StepResults = new Dictionary<string, object>();
        ErrorMessage = null;
        ReflectionCycles.Clear();
        return Result.Success();
    }
}

/// <summary>
/// Workflow node type (from Archon)
/// </summary>
public enum WorkflowNodeType
{
    /// <summary>
    /// AI-powered node (planning, code generation, review)
    /// </summary>
    AI,
    
    /// <summary>
    /// Deterministic node (bash scripts, tests, git ops)
    /// </summary>
    Deterministic,
    
    /// <summary>
    /// Interactive gate for human approval
    /// </summary>
    Interactive,
    
    /// <summary>
    /// Loop node for iteration
    /// </summary>
    Loop
}

/// <summary>
/// Loop configuration (from Archon)
/// </summary>
public class LoopConfiguration
{
    public string UntilCondition { get; set; } = string.Empty;
    public bool FreshContext { get; set; } = true;
    public int MaxIterations { get; set; } = 10;
    public int CurrentIteration { get; set; } = 0;
    public bool IsComplete => false;
}

/// <summary>
/// Isolation context (from Archon - git worktree isolation)
/// </summary>
public class IsolationContext
{
    public string WorktreePath { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    
    public IsolationContext(string worktreePath, string branchName)
    {
        WorktreePath = worktreePath;
        BranchName = branchName;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}

public record WorkflowStep(
    string StepType,
    string Description,
    Dictionary<string, object> Parameters,
    bool RequiresConfirmation = false,
    bool EnableReflexion = true,
    WorkflowNodeType NodeType = WorkflowNodeType.AI,
    LoopConfiguration? LoopConfig = null,
    List<int>? DependsOn = null
);

public record AIWorkflowCreatedEvent(Guid WorkflowId, Guid UserId, string Name) : DomainEvent;
public record AIWorkflowStartedEvent(Guid WorkflowId, Guid UserId) : DomainEvent;
public record AIWorkflowCompletedEvent(Guid WorkflowId, Guid UserId) : DomainEvent;
public record AIWorkflowFailedEvent(Guid WorkflowId, Guid UserId, string ErrorMessage) : DomainEvent;
public record AIWorkflowReflectionStartedEvent(Guid WorkflowId, Guid UserId, int StepIndex, Guid ReflectionId) : DomainEvent;
public record AIWorkflowReflectionCompletedEvent(Guid WorkflowId, Guid UserId, Guid ReflectionId, ReflexionStatus Status) : DomainEvent;
public record AIWorkflowInsightsMemorizedEvent(Guid WorkflowId, Guid UserId, Guid ReflectionId, List<string> Insights) : DomainEvent;
