namespace Libr4.IDE.Domain.IntelligenceRouter;

/// <summary>
/// Value object representing a routing decision for a specific phase
/// </summary>
public class RoutingDecision
{
    public string PhaseId { get; private set; }
    public string PhaseName { get; private set; }
    public PhaseComplexity Complexity { get; private set; }
    public ModelProvider SelectedProvider { get; private set; }
    public string SelectedModel { get; private set; }
    public List<ToolType> SelectedTools { get; private set; }
    public string Rationale { get; private set; }
    public double Confidence { get; private set; }
    public Dictionary<string, object> ContextQueries { get; private set; }
    
    private RoutingDecision() { }
    
    public RoutingDecision(
        string phaseId,
        string phaseName,
        PhaseComplexity complexity,
        ModelProvider selectedProvider,
        string selectedModel,
        List<ToolType> selectedTools,
        string rationale,
        double confidence,
        Dictionary<string, object>? contextQueries = null)
    {
        PhaseId = phaseId;
        PhaseName = phaseName;
        Complexity = complexity;
        SelectedProvider = selectedProvider;
        SelectedModel = selectedModel;
        SelectedTools = selectedTools ?? new List<ToolType>();
        Rationale = rationale;
        Confidence = Math.Max(0.0, Math.Min(1.0, confidence));
        ContextQueries = contextQueries ?? new Dictionary<string, object>();
    }
    
    public void AddTool(ToolType tool)
    {
        if (!SelectedTools.Contains(tool))
        {
            SelectedTools.Add(tool);
        }
    }
    
    public void AddContextQuery(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            ContextQueries[key] = value;
        }
    }
    
    public static RoutingDecision Create(
        string phaseId,
        string phaseName,
        PhaseComplexity complexity,
        ModelProvider selectedProvider,
        string selectedModel,
        List<ToolType> selectedTools,
        string rationale,
        double confidence,
        Dictionary<string, object>? contextQueries = null)
    {
        return new RoutingDecision(phaseId, phaseName, complexity, selectedProvider, selectedModel, selectedTools, rationale, confidence, contextQueries);
    }
}
