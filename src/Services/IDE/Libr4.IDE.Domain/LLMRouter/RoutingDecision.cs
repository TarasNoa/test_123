namespace Libr4.IDE.Domain.LLMRouter;

/// <summary>
/// Value object for routing decision
/// </summary>
public class RoutingDecision
{
    public LLMModel SelectedModel { get; private set; }
    public double EstimatedCost { get; private set; }
    public double EstimatedLatency { get; private set; }
    public string Rationale { get; private set; }
    
    private RoutingDecision() { }
    
    public RoutingDecision(
        LLMModel selectedModel,
        double estimatedCost,
        double estimatedLatency,
        string rationale)
    {
        SelectedModel = selectedModel;
        EstimatedCost = estimatedCost;
        EstimatedLatency = estimatedLatency;
        Rationale = rationale;
    }
    
    public static RoutingDecision Create(
        LLMModel selectedModel,
        double estimatedCost,
        double estimatedLatency,
        string rationale)
    {
        return new RoutingDecision(selectedModel, estimatedCost, estimatedLatency, rationale);
    }
}
