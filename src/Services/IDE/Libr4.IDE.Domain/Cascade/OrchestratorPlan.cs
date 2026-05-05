using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.Cascade.Events;

namespace Libr4.IDE.Domain.Cascade;

/// <summary>
/// Task analysis result
/// </summary>
public class TaskAnalysis
{
    public string TaskDescription { get; private set; }
    public List<string> Subtasks { get; private set; }
    public string Complexity { get; private set; }
    
    public TaskAnalysis(string taskDescription, List<string> subtasks, string complexity)
    {
        TaskDescription = taskDescription;
        Subtasks = subtasks ?? new List<string>();
        Complexity = complexity ?? "Medium";
    }
}

/// <summary>
/// AggregateRoot representing a complete orchestrator plan for cascade execution
/// </summary>
public class OrchestratorPlan : AggregateRoot<Guid>
{
    public string PlanId { get; private set; }
    public string OriginalPrompt { get; private set; }
    public TaskAnalysis TaskAnalysis { get; private set; }
    public List<OrchestratorPhase> Phases { get; private set; }
    public PrefetchContext PrefetchContext { get; private set; }
    public string OrchestratorJson { get; private set; }
    public string Rationale { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private OrchestratorPlan() { }
    
    public OrchestratorPlan(
        string planId,
        string originalPrompt,
        TaskAnalysis taskAnalysis,
        List<OrchestratorPhase> phases,
        PrefetchContext prefetchContext,
        string orchestratorJson,
        string rationale)
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        OriginalPrompt = originalPrompt;
        TaskAnalysis = taskAnalysis;
        Phases = phases ?? new List<OrchestratorPhase>();
        PrefetchContext = prefetchContext ?? PrefetchContext.Empty;
        OrchestratorJson = orchestratorJson;
        Rationale = rationale;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddPhase(OrchestratorPhase phase)
    {
        if (phase != null)
        {
            Phases.Add(phase);
        }
    }
    
    public void SetOrchestratorJson(string json)
    {
        OrchestratorJson = json;
    }
    
    public void SetRationale(string rationale)
    {
        Rationale = rationale;
    }
    
    /// <summary>
    /// Marks the orchestrator plan as generated and raises a domain event
    /// </summary>
    public void MarkAsGenerated()
    {
        AddDomainEvent(new CascadePlanGeneratedEvent(Id, PlanId));
    }
    
    public static OrchestratorPlan Create(
        string planId,
        string originalPrompt,
        TaskAnalysis taskAnalysis,
        List<OrchestratorPhase> phases,
        PrefetchContext prefetchContext,
        string orchestratorJson,
        string rationale)
    {
        return new OrchestratorPlan(planId, originalPrompt, taskAnalysis, phases, prefetchContext, orchestratorJson, rationale);
    }
}
