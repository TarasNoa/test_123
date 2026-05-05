using Libr4.Shared.Kernel.Domain;

namespace Libr4.Projects.Domain.Workflows;

public enum WorkflowStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Archived
}

public enum TriggerType
{
    Manual,
    Automatic,
    Scheduled,
    EventBased
}

public class Workflow : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid ProjectId { get; private set; }
    public WorkflowStatus Status { get; private set; }
    public TriggerType TriggerType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<WorkflowStep> _steps = new();
    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    private readonly List<WorkflowExecution> _executions = new();
    public IReadOnlyCollection<WorkflowExecution> Executions => _executions.AsReadOnly();

    private Workflow() { }

    public static Workflow Create(string name, Guid projectId, TriggerType triggerType = TriggerType.Manual)
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = name,
            ProjectId = projectId,
            Status = WorkflowStatus.Draft,
            TriggerType = triggerType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = WorkflowStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pause()
    {
        Status = WorkflowStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = WorkflowStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = WorkflowStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStep(WorkflowStep step)
    {
        _steps.Add(step);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStep(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step != null)
        {
            _steps.Remove(step);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddExecution(WorkflowExecution execution)
    {
        _executions.Add(execution);
        UpdatedAt = DateTime.UtcNow;
    }
}

public class WorkflowStep : Entity<Guid>
{
    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public string? Parameters { get; private set; }
    public bool IsRequired { get; private set; }

    private WorkflowStep() { }

    public static WorkflowStep Create(Guid workflowId, string name, int order, string actionType, string? parameters = null)
    {
        return new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Name = name,
            Order = order,
            ActionType = actionType,
            Parameters = parameters,
            IsRequired = true
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateOrder(int order)
    {
        Order = order;
    }

    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
    }
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class WorkflowExecution : Entity<Guid>
{
    public Guid WorkflowId { get; private set; }
    public Guid? TriggeredByUserId { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private readonly List<StepExecution> _stepExecutions = new();
    public IReadOnlyCollection<StepExecution> StepExecutions => _stepExecutions.AsReadOnly();

    private WorkflowExecution() { }

    public static WorkflowExecution Create(Guid workflowId, Guid? triggeredByUserId = null)
    {
        return new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            TriggeredByUserId = triggeredByUserId,
            Status = ExecutionStatus.Pending,
            StartedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        Status = ExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void Cancel()
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public void AddStepExecution(StepExecution stepExecution)
    {
        _stepExecutions.Add(stepExecution);
    }
}

public class StepExecution : Entity<Guid>
{
    public Guid WorkflowExecutionId { get; private set; }
    public Guid WorkflowStepId { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Result { get; private set; }
    public string? ErrorMessage { get; private set; }

    private StepExecution() { }

    public static StepExecution Create(Guid workflowExecutionId, Guid workflowStepId)
    {
        return new StepExecution
        {
            Id = Guid.NewGuid(),
            WorkflowExecutionId = workflowExecutionId,
            WorkflowStepId = workflowStepId,
            Status = ExecutionStatus.Pending,
            StartedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        Status = ExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(string? result = null)
    {
        Status = ExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void Cancel()
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }
}
