namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// A decision made by an agent with certainty tracking
/// </summary>
public class AgentDecision
{
    public string DecisionId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string DecisionDescription { get; set; } = string.Empty;
    public CertaintyLevel Certainty { get; set; }
    public double ConfidenceScore { get; set; } // 0.0 - 1.0
    public List<string> Reasoning { get; set; } = new();
    public List<string> Alternatives { get; set; } = new();
    public DateTime MadeAt { get; set; }
    public bool RequiresHumanReview { get; set; }
    public string? Context { get; set; }
    public string? DecisionType { get; set; } // "code_generation", "refactoring", "debugging", etc.
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; } // "agent", "human", "system"
    public DateTime? ApprovedAt { get; set; }
    
    public AgentDecision()
    {
    }
    
    public AgentDecision(
        string decisionId,
        string agentId,
        string decisionDescription,
        double confidenceScore,
        List<string>? reasoning = null,
        List<string>? alternatives = null)
    {
        DecisionId = decisionId;
        AgentId = agentId;
        DecisionDescription = decisionDescription;
        ConfidenceScore = Math.Clamp(confidenceScore, 0.0, 1.0);
        Certainty = CertaintyLevelExtensions.FromConfidence(ConfidenceScore);
        Reasoning = reasoning ?? new List<string>();
        Alternatives = alternatives ?? new List<string>();
        MadeAt = DateTime.UtcNow;
        RequiresHumanReview = Certainty < CertaintyLevel.High;
        IsApproved = !RequiresHumanReview;
    }
    
    /// <summary>
    /// Check if decision requires human review based on certainty
    /// </summary>
    public bool ShouldRequireHumanReview => Certainty < CertaintyLevel.High;
    
    /// <summary>
    /// Approve the decision
    /// </summary>
    public void Approve(string approvedBy)
    {
        IsApproved = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Reject the decision
    /// </summary>
    public void Reject(string reason)
    {
        IsApproved = false;
        ApprovedBy = "human";
        ApprovedAt = DateTime.UtcNow;
        Reasoning.Add($"Rejected: {reason}");
    }
    
    /// <summary>
    /// Update confidence score
    /// </summary>
    public void UpdateConfidence(double newScore)
    {
        ConfidenceScore = Math.Clamp(newScore, 0.0, 1.0);
        Certainty = CertaintyLevelExtensions.FromConfidence(ConfidenceScore);
        RequiresHumanReview = ShouldRequireHumanReview;
    }
    
    /// <summary>
    /// Add reasoning
    /// </summary>
    public void AddReasoning(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reasoning.Add(reason);
        }
    }
    
    /// <summary>
    /// Add alternative
    /// </summary>
    public void AddAlternative(string alternative)
    {
        if (!string.IsNullOrWhiteSpace(alternative))
        {
            Alternatives.Add(alternative);
        }
    }
}

/// <summary>
/// Threshold for certainty-based decision making
/// </summary>
public class CertaintyThreshold
{
    public CertaintyLevel MinimumRequired { get; set; }
    public bool AutoProceedIfMet { get; set; } = true;
    public bool RequestHumanReviewIfBelow { get; set; } = true;
    public bool BlockIfBelow { get; set; } = false;
    
    public CertaintyThreshold()
    {
        MinimumRequired = CertaintyLevel.Medium;
    }
    
    public CertaintyThreshold(CertaintyLevel minimumRequired)
    {
        MinimumRequired = minimumRequired;
    }
    
    /// <summary>
    /// Check if a decision meets the threshold
    /// </summary>
    public bool MeetsThreshold(AgentDecision decision)
    {
        return decision.Certainty >= MinimumRequired;
    }
    
    /// <summary>
    /// Get the action to take based on decision certainty
    /// </summary>
    public ThresholdAction GetAction(AgentDecision decision)
    {
        if (MeetsThreshold(decision))
        {
            return AutoProceedIfMet ? ThresholdAction.AutoProceed : ThresholdAction.RequestApproval;
        }
        
        if (RequestHumanReviewIfBelow)
        {
            return ThresholdAction.RequestHumanReview;
        }
        
        if (BlockIfBelow)
        {
            return ThresholdAction.Block;
        }
        
        return ThresholdAction.ProceedWithWarning;
    }
}

/// <summary>
/// Action to take based on certainty threshold
/// </summary>
public enum ThresholdAction
{
    AutoProceed,
    RequestApproval,
    RequestHumanReview,
    ProceedWithWarning,
    Block
}
