namespace Libr4.AI.Domain.MLResearch;

/// <summary>
/// Status of verification plan
/// </summary>
public enum VerificationPlanStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

/// <summary>
/// Plan for mechanical verification of research results
/// </summary>
public class MechanicalVerificationPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string ResearchTaskId { get; set; } = string.Empty;
    public List<VerificationStep> Steps { get; set; } = new();
    public VerificationPlanStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public List<string> FailedSteps { get; set; } = new();
    public List<string> SkippedSteps { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public MechanicalVerificationPlan()
    {
    }
    
    public MechanicalVerificationPlan(string planId, string researchTaskId)
    {
        PlanId = planId;
        ResearchTaskId = researchTaskId;
        Status = VerificationPlanStatus.NotStarted;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add a verification step
    /// </summary>
    public void AddStep(VerificationStep step)
    {
        Steps.Add(step);
    }
    
    /// <summary>
    /// Get steps in execution order
    /// </summary>
    public List<VerificationStep> GetOrderedSteps()
    {
        return Steps.OrderBy(s => s.Order).ToList();
    }
    
    /// <summary>
    /// Get next executable step
    /// </summary>
    public VerificationStep? GetNextExecutableStep()
    {
        return GetOrderedSteps()
            .FirstOrDefault(s => s.Status == VerificationStatus.Pending && s.CanExecute(CompletedSteps));
    }
    
    /// <summary>
    /// Mark step as completed
    /// </summary>
    public void MarkStepCompleted(string stepId)
    {
        if (!CompletedSteps.Contains(stepId))
        {
            CompletedSteps.Add(stepId);
        }
        
        var step = Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step != null && step.Result?.Passed == true)
        {
            UpdateStatus();
        }
    }
    
    /// <summary>
    /// Mark step as failed
    /// </summary>
    public void MarkStepFailed(string stepId)
    {
        if (!FailedSteps.Contains(stepId))
        {
            FailedSteps.Add(stepId);
        }
        
        var step = Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step != null && step.IsRequired)
        {
            Status = VerificationPlanStatus.Failed;
        }
        else
        {
            UpdateStatus();
        }
    }
    
    /// <summary>
    /// Mark step as skipped
    /// </summary>
    public void MarkStepSkipped(string stepId)
    {
        if (!SkippedSteps.Contains(stepId))
        {
            SkippedSteps.Add(stepId);
        }
        
        UpdateStatus();
    }
    
    /// <summary>
    /// Update plan status based on step statuses
    /// </summary>
    private void UpdateStatus()
    {
        if (Status == VerificationPlanStatus.Failed)
        {
            return;
        }
        
        var allSteps = Steps;
        var completed = allSteps.Count(s => s.Status == VerificationStatus.Passed);
        var failed = allSteps.Count(s => s.Status == VerificationStatus.Failed);
        var skipped = allSteps.Count(s => s.Status == VerificationStatus.Skipped);
        var pending = allSteps.Count(s => s.Status == VerificationStatus.Pending);
        
        if (pending == 0)
        {
            if (failed == 0)
            {
                Status = VerificationPlanStatus.Completed;
                CompletedAt = DateTime.UtcNow;
            }
            else
            {
                Status = VerificationPlanStatus.PartiallyCompleted;
            }
        }
        else if (completed > 0 || skipped > 0)
        {
            Status = VerificationPlanStatus.InProgress;
        }
    }
    
    /// <summary>
    /// Check if plan is completed successfully
    /// </summary>
    public bool IsSuccessfullyCompleted => 
        Status == VerificationPlanStatus.Completed && FailedSteps.Count == 0;
    
    /// <summary>
    /// Get verification summary
    /// </summary>
    public VerificationSummary GetSummary()
    {
        return new VerificationSummary
        {
            PlanId = PlanId,
            ResearchTaskId = ResearchTaskId,
            Status = Status,
            TotalSteps = Steps.Count,
            CompletedSteps = CompletedSteps.Count,
            FailedSteps = FailedSteps.Count,
            SkippedSteps = SkippedSteps.Count,
            PendingSteps = Steps.Count(s => s.Status == VerificationStatus.Pending),
            RequiredSteps = Steps.Count(s => s.IsRequired),
            RequiredStepsPassed = Steps.Count(s => s.IsRequired && s.Result?.Passed == true),
            OverallPassRate = Steps.Count > 0 
                ? (double)CompletedSteps.Count / Steps.Count 
                : 0.0
        };
    }
}

/// <summary>
/// Summary of verification results
/// </summary>
public class VerificationSummary
{
    public string PlanId { get; set; } = string.Empty;
    public string ResearchTaskId { get; set; } = string.Empty;
    public VerificationPlanStatus Status { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int FailedSteps { get; set; }
    public int SkippedSteps { get; set; }
    public int PendingSteps { get; set; }
    public int RequiredSteps { get; set; }
    public int RequiredStepsPassed { get; set; }
    public double OverallPassRate { get; set; }
}
