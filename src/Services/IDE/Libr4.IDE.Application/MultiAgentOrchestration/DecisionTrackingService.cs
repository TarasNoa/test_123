namespace Libr4.IDE.Application.MultiAgentOrchestration;

using Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Service for tracking and managing agent decisions with certainty levels
/// </summary>
public interface IDecisionTrackingService
{
    /// <summary>
    /// Create a new agent decision
    /// </summary>
    AgentDecision CreateDecision(
        string agentId,
        string decisionDescription,
        double confidenceScore,
        List<string>? reasoning = null,
        List<string>? alternatives = null,
        string? context = null,
        string? decisionType = null);
    
    /// <summary>
    /// Update decision confidence
    /// </summary>
    void UpdateDecisionConfidence(string decisionId, double newScore);
    
    /// <summary>
    /// Add reasoning to a decision
    /// </summary>
    void AddDecisionReasoning(string decisionId, string reason);
    
    /// <summary>
    /// Add alternative to a decision
    /// </summary>
    void AddDecisionAlternative(string decisionId, string alternative);
    
    /// <summary>
    /// Approve a decision
    /// </summary>
    void ApproveDecision(string decisionId, string approvedBy);
    
    /// <summary>
    /// Reject a decision
    /// </summary>
    void RejectDecision(string decisionId, string reason);
    
    /// <summary>
    /// Check if decision meets threshold
    /// </summary>
    bool MeetsThreshold(string decisionId, CertaintyThreshold threshold);
    
    /// <summary>
    /// Get action to take based on threshold
    /// </summary>
    ThresholdAction GetThresholdAction(string decisionId, CertaintyThreshold threshold);
    
    /// <summary>
    /// Get decision by ID
    /// </summary>
    AgentDecision? GetDecision(string decisionId);
    
    /// <summary>
    /// Get all decisions for an agent
    /// </summary>
    List<AgentDecision> GetAgentDecisions(string agentId);
    
    /// <summary>
    /// Get decisions requiring human review
    /// </summary>
    List<AgentDecision> GetDecisionsRequiringReview();
    
    /// <summary>
    /// Get decision statistics
    /// </summary>
    DecisionStatistics GetDecisionStatistics();
}

/// <summary>
/// Statistics about agent decisions
/// </summary>
public class DecisionStatistics
{
    public int TotalDecisions { get; set; }
    public int ApprovedDecisions { get; set; }
    public int RejectedDecisions { get; set; }
    public int PendingReview { get; set; }
    public Dictionary<CertaintyLevel, int> CertaintyDistribution { get; set; } = new();
    public double AverageConfidence { get; set; }
}

public class DecisionTrackingService : IDecisionTrackingService
{
    private readonly Dictionary<string, AgentDecision> _decisions = new();
    private readonly Dictionary<string, List<string>> _agentDecisions = new();
    
    public AgentDecision CreateDecision(
        string agentId,
        string decisionDescription,
        double confidenceScore,
        List<string>? reasoning = null,
        List<string>? alternatives = null,
        string? context = null,
        string? decisionType = null)
    {
        var decisionId = Guid.NewGuid().ToString();
        var decision = new AgentDecision(
            decisionId,
            agentId,
            decisionDescription,
            confidenceScore,
            reasoning,
            alternatives)
        {
            Context = context,
            DecisionType = decisionType
        };
        
        _decisions[decisionId] = decision;
        
        if (!_agentDecisions.ContainsKey(agentId))
        {
            _agentDecisions[agentId] = new List<string>();
        }
        _agentDecisions[agentId].Add(decisionId);
        
        return decision;
    }
    
    public void UpdateDecisionConfidence(string decisionId, double newScore)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            decision.UpdateConfidence(newScore);
        }
    }
    
    public void AddDecisionReasoning(string decisionId, string reason)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            decision.AddReasoning(reason);
        }
    }
    
    public void AddDecisionAlternative(string decisionId, string alternative)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            decision.AddAlternative(alternative);
        }
    }
    
    public void ApproveDecision(string decisionId, string approvedBy)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            decision.Approve(approvedBy);
        }
    }
    
    public void RejectDecision(string decisionId, string reason)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            decision.Reject(reason);
        }
    }
    
    public bool MeetsThreshold(string decisionId, CertaintyThreshold threshold)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            return threshold.MeetsThreshold(decision);
        }
        return false;
    }
    
    public ThresholdAction GetThresholdAction(string decisionId, CertaintyThreshold threshold)
    {
        if (_decisions.TryGetValue(decisionId, out var decision))
        {
            return threshold.GetAction(decision);
        }
        return ThresholdAction.Block;
    }
    
    public AgentDecision? GetDecision(string decisionId)
    {
        return _decisions.TryGetValue(decisionId, out var decision) ? decision : null;
    }
    
    public List<AgentDecision> GetAgentDecisions(string agentId)
    {
        if (_agentDecisions.TryGetValue(agentId, out var decisionIds))
        {
            return decisionIds.Select(id => _decisions[id]).ToList();
        }
        return new List<AgentDecision>();
    }
    
    public List<AgentDecision> GetDecisionsRequiringReview()
    {
        return _decisions.Values.Where(d => d.ShouldRequireHumanReview && !d.IsApproved).ToList();
    }
    
    public DecisionStatistics GetDecisionStatistics()
    {
        var stats = new DecisionStatistics
        {
            TotalDecisions = _decisions.Count,
            ApprovedDecisions = _decisions.Values.Count(d => d.IsApproved),
            RejectedDecisions = _decisions.Values.Count(d => !d.IsApproved && d.ApprovedAt.HasValue),
            PendingReview = _decisions.Values.Count(d => d.ShouldRequireHumanReview && !d.IsApproved)
        };
        
        // Certainty distribution
        foreach (var decision in _decisions.Values)
        {
            if (!stats.CertaintyDistribution.ContainsKey(decision.Certainty))
            {
                stats.CertaintyDistribution[decision.Certainty] = 0;
            }
            stats.CertaintyDistribution[decision.Certainty]++;
        }
        
        // Average confidence
        if (_decisions.Count > 0)
        {
            stats.AverageConfidence = _decisions.Values.Average(d => d.ConfidenceScore);
        }
        
        return stats;
    }
}
