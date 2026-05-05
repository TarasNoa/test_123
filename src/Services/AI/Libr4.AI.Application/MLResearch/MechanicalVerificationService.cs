namespace Libr4.AI.Application.MLResearch;

using Libr4.AI.Domain.MLResearch;

/// <summary>
/// Service for mechanical verification of research results
/// </summary>
public interface IMechanicalVerificationService
{
    /// <summary>
    /// Create a new verification plan
    /// </summary>
    MechanicalVerificationPlan CreatePlan(string researchTaskId);
    
    /// <summary>
    /// Add a verification step to a plan
    /// </summary>
    void AddVerificationStep(MechanicalVerificationPlan plan, VerificationStep step);
    
    /// <summary>
    /// Execute a verification step
    /// </summary>
    Task<VerificationResult> ExecuteStepAsync(MechanicalVerificationPlan plan, string stepId, CancellationToken ct = default);
    
    /// <summary>
    /// Execute all steps in a plan
    /// </summary>
    Task<VerificationSummary> ExecutePlanAsync(MechanicalVerificationPlan plan, CancellationToken ct = default);
    
    /// <summary>
    /// Get verification summary
    /// </summary>
    VerificationSummary GetSummary(MechanicalVerificationPlan plan);
    
    /// <summary>
    /// Skip a verification step
    /// </summary>
    void SkipStep(MechanicalVerificationPlan plan, string stepId, string reason);
    
    /// <summary>
    /// Retry a failed verification step
    /// </summary>
    Task<VerificationResult> RetryStepAsync(MechanicalVerificationPlan plan, string stepId, CancellationToken ct = default);
}

public class MechanicalVerificationService : IMechanicalVerificationService
{
    public MechanicalVerificationPlan CreatePlan(string researchTaskId)
    {
        var planId = Guid.NewGuid().ToString();
        return new MechanicalVerificationPlan(planId, researchTaskId);
    }
    
    public void AddVerificationStep(MechanicalVerificationPlan plan, VerificationStep step)
    {
        plan.AddStep(step);
    }
    
    public async Task<VerificationResult> ExecuteStepAsync(MechanicalVerificationPlan plan, string stepId, CancellationToken ct = default)
    {
        var step = plan.Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step == null)
        {
            throw new ArgumentException($"Step {stepId} not found in plan");
        }
        
        step.MarkAsRunning();
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Execute verification based on method
            var result = await ExecuteVerificationByMethod(step, ct);
            
            step.ExecutionDuration = DateTime.UtcNow - startTime;
            step.ExecutedAt = DateTime.UtcNow;
            
            if (result.Passed)
            {
                step.MarkAsPassed(result);
                plan.MarkStepCompleted(stepId);
            }
            else
            {
                step.MarkAsFailed(string.Join("; ", result.Errors));
                plan.MarkStepFailed(stepId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            step.ExecutionDuration = DateTime.UtcNow - startTime;
            step.MarkAsFailed(ex.Message);
            plan.MarkStepFailed(stepId);
            
            return new VerificationResult
            {
                Passed = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }
    
    private async Task<VerificationResult> ExecuteVerificationByMethod(VerificationStep step, CancellationToken ct)
    {
        // In a real implementation, this would execute the verification code
        // based on the method (CodeExecution, UnitTest, etc.)
        
        // For now, return a mock result
        await Task.Delay(100, ct);
        
        return new VerificationResult
        {
            Passed = true,
            Output = "Verification passed (mock)",
            Metrics = new Dictionary<string, object>
            {
                { "executionTime", 100 }
            }
        };
    }
    
    public async Task<VerificationSummary> ExecutePlanAsync(MechanicalVerificationPlan plan, CancellationToken ct)
    {
        plan.Status = VerificationPlanStatus.InProgress;
        
        var steps = plan.GetOrderedSteps();
        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();
            
            if (step.Status == VerificationStatus.Pending && step.CanExecute(plan.CompletedSteps))
            {
                await ExecuteStepAsync(plan, step.StepId, ct);
                
                // Stop if a required step failed
                if (step.Status == VerificationStatus.Failed && step.IsRequired)
                {
                    break;
                }
            }
        }
        
        return plan.GetSummary();
    }
    
    public VerificationSummary GetSummary(MechanicalVerificationPlan plan)
    {
        return plan.GetSummary();
    }
    
    public void SkipStep(MechanicalVerificationPlan plan, string stepId, string reason)
    {
        var step = plan.Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step != null)
        {
            step.MarkAsSkipped(reason);
            plan.MarkStepSkipped(stepId);
        }
    }
    
    public async Task<VerificationResult> RetryStepAsync(MechanicalVerificationPlan plan, string stepId, CancellationToken ct)
    {
        var step = plan.Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step == null)
        {
            throw new ArgumentException($"Step {stepId} not found in plan");
        }
        
        // Reset step status
        step.Status = VerificationStatus.Pending;
        step.Result = null;
        step.ExecutedAt = null;
        
        // Remove from failed/completed lists
        plan.FailedSteps.Remove(stepId);
        plan.CompletedSteps.Remove(stepId);
        
        // Execute again
        return await ExecuteStepAsync(plan, stepId, ct);
    }
}
