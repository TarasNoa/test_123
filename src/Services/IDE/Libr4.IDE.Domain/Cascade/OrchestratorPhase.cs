namespace Libr4.IDE.Domain.Cascade;

/// <summary>
/// Value object representing a phase in orchestrator output
/// </summary>
public class OrchestratorPhase
{
    public string PhaseId { get; private set; }
    public string PhaseName { get; private set; }
    public string Description { get; private set; }
    public List<string> Dependencies { get; private set; }
    public Dictionary<string, object> PhaseSpecificInstructions { get; private set; }
    public string ExpectedOutput { get; private set; }
    
    private OrchestratorPhase() { }
    
    public OrchestratorPhase(
        string phaseId,
        string phaseName,
        string description,
        List<string>? dependencies,
        Dictionary<string, object>? phaseSpecificInstructions,
        string expectedOutput)
    {
        PhaseId = phaseId;
        PhaseName = phaseName;
        Description = description;
        Dependencies = dependencies ?? new List<string>();
        PhaseSpecificInstructions = phaseSpecificInstructions ?? new Dictionary<string, object>();
        ExpectedOutput = expectedOutput;
    }
    
    public void AddDependency(string phaseId)
    {
        if (!string.IsNullOrWhiteSpace(phaseId) && !Dependencies.Contains(phaseId))
        {
            Dependencies.Add(phaseId);
        }
    }
    
    public void AddInstruction(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            PhaseSpecificInstructions[key] = value;
        }
    }
    
    public static OrchestratorPhase Create(
        string phaseId,
        string phaseName,
        string description,
        List<string>? dependencies = null,
        Dictionary<string, object>? phaseSpecificInstructions = null,
        string expectedOutput = "")
    {
        return new OrchestratorPhase(phaseId, phaseName, description, dependencies, phaseSpecificInstructions, expectedOutput);
    }
}
