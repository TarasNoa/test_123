using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.IntelligenceRouter.Events;

namespace Libr4.IDE.Domain.IntelligenceRouter;

/// <summary>
/// AggregateRoot representing a complete routing plan for multi-phase execution
/// </summary>
public class RoutingPlan : AggregateRoot<Guid>
{
    public string PlanId { get; private set; }
    public string Prompt { get; private set; }
    public List<RoutingDecision> PhaseDecisions { get; private set; }
    public string PrimaryProvider { get; private set; }
    public string PrimaryModel { get; private set; }
    public List<ToolType> GlobalTools { get; private set; }
    public string Rationale { get; private set; }
    public double Confidence { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private RoutingPlan() { }
    
    public RoutingPlan(
        string planId,
        string prompt,
        List<RoutingDecision> phaseDecisions,
        string primaryProvider,
        string primaryModel,
        List<ToolType> globalTools,
        string rationale,
        double confidence)
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        Prompt = prompt;
        PhaseDecisions = phaseDecisions ?? new List<RoutingDecision>();
        PrimaryProvider = primaryProvider;
        PrimaryModel = primaryModel;
        GlobalTools = globalTools ?? new List<ToolType>();
        Rationale = rationale;
        Confidence = Math.Max(0.0, Math.Min(1.0, confidence));
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddPhaseDecision(RoutingDecision decision)
    {
        if (decision != null)
        {
            PhaseDecisions.Add(decision);
        }
    }
    
    public void AddGlobalTool(ToolType tool)
    {
        if (!GlobalTools.Contains(tool))
        {
            GlobalTools.Add(tool);
        }
    }
    
    /// <summary>
    /// Marks the routing plan as generated and raises a domain event
    /// </summary>
    public void MarkAsGenerated()
    {
        AddDomainEvent(new RoutingPlanGeneratedEvent(Id, PlanId, PrimaryProvider));
    }
    
    public static RoutingPlan Create(
        string planId,
        string prompt,
        List<RoutingDecision> phaseDecisions,
        string primaryProvider,
        string primaryModel,
        List<ToolType> globalTools,
        string rationale,
        double confidence)
    {
        return new RoutingPlan(planId, prompt, phaseDecisions, primaryProvider, primaryModel, globalTools, rationale, confidence);
    }
}
