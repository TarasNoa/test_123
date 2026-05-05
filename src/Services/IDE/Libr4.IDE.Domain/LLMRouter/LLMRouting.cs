using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.LLMRouter.Events;

namespace Libr4.IDE.Domain.LLMRouter;

/// <summary>
/// AggregateRoot for LLM routing
/// </summary>
public class LLMRouting : AggregateRoot<Guid>
{
    public string RoutingId { get; private set; }
    public string TaskId { get; private set; }
    public string Prompt { get; private set; }
    public int EstimatedTokens { get; private set; }
    public RoutingDecision Decision { get; private set; }
    public double CostSavings { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private LLMRouting() { }
    
    public LLMRouting(
        string routingId,
        string taskId,
        string prompt,
        int estimatedTokens)
    {
        Id = Guid.NewGuid();
        RoutingId = routingId;
        TaskId = taskId;
        Prompt = prompt;
        EstimatedTokens = estimatedTokens;
        Decision = null!;
        CostSavings = 0.0;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetDecision(RoutingDecision decision)
    {
        Decision = decision;
    }
    
    public void SetCostSavings(double savings)
    {
        CostSavings = savings;
    }
    
    /// <summary>
    /// Marks the routing as completed and raises a domain event
    /// </summary>
    public void MarkAsCompleted()
    {
        AddDomainEvent(new RoutingCompletedEvent(Id, RoutingId));
    }
    
    /// <summary>
    /// Marks model selection and raises a domain event
    /// </summary>
    public void MarkModelSelected(LLMModel model)
    {
        AddDomainEvent(new ModelSelectedEvent(Id, RoutingId, model.ModelName));
    }
    
    /// <summary>
    /// Marks cost optimization and raises a domain event
    /// </summary>
    public void MarkCostOptimized(double savings)
    {
        AddDomainEvent(new CostOptimizedEvent(Id, RoutingId, savings));
    }
    
    public static LLMRouting Create(
        string routingId,
        string taskId,
        string prompt,
        int estimatedTokens)
    {
        return new LLMRouting(routingId, taskId, prompt, estimatedTokens);
    }
}
