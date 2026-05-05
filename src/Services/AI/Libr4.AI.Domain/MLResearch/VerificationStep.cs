namespace Libr4.AI.Domain.MLResearch;

/// <summary>
/// Method for verification
/// </summary>
public enum VerificationMethod
{
    CodeExecution,
    UnitTest,
    IntegrationTest,
    Benchmark,
    StatisticalValidation,
    CrossValidation,
    ManualReview
}

/// <summary>
/// Status of verification step
/// </summary>
public enum VerificationStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// A single verification step
/// </summary>
public class VerificationStep
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string StepDescription { get; set; } = string.Empty;
    public VerificationMethod Method { get; set; }
    public string VerificationCode { get; set; } = string.Empty;
    public VerificationResult? Result { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public VerificationStatus Status { get; set; }
    public int Order { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public bool IsRequired { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public VerificationStep()
    {
    }
    
    public VerificationStep(
        string stepId,
        string stepName,
        string stepDescription,
        VerificationMethod method,
        string verificationCode,
        int order = 0)
    {
        StepId = stepId;
        StepName = stepName;
        StepDescription = stepDescription;
        Method = method;
        VerificationCode = verificationCode;
        Order = order;
        Status = VerificationStatus.Pending;
    }
    
    /// <summary>
    /// Mark step as running
    /// </summary>
    public void MarkAsRunning()
    {
        Status = VerificationStatus.Running;
    }
    
    /// <summary>
    /// Mark step as passed
    /// </summary>
    public void MarkAsPassed(VerificationResult result)
    {
        Status = VerificationStatus.Passed;
        Result = result;
        ExecutedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark step as failed
    /// </summary>
    public void MarkAsFailed(string failureReason)
    {
        Status = VerificationStatus.Failed;
        Result = new VerificationResult
        {
            Passed = false,
            Errors = new List<string> { failureReason }
        };
        ExecutedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Mark step as skipped
    /// </summary>
    public void MarkAsSkipped(string reason)
    {
        Status = VerificationStatus.Skipped;
        Result = new VerificationResult
        {
            Passed = true,
            Output = $"Skipped: {reason}"
        };
        ExecutedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Check if step can execute based on dependencies
    /// </summary>
    public bool CanExecute(List<string> completedSteps)
    {
        return Dependencies.All(dep => completedSteps.Contains(dep));
    }
}

/// <summary>
/// Result of verification
/// </summary>
public class VerificationResult
{
    public bool Passed { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string Evidence { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public double PassRate => (PassedTests + FailedTests) > 0 
        ? (double)PassedTests / (PassedTests + FailedTests) 
        : 1.0;
}
