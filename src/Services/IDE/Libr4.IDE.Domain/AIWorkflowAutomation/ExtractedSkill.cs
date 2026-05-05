namespace Libr4.IDE.Domain.AIWorkflowAutomation;

/// <summary>
/// Entity representing an extracted skill
/// </summary>
public class ExtractedSkill
{
    public Guid Id { get; private set; }
    public string SkillName { get; private set; }
    public string Description { get; private set; }
    public List<string> Capabilities { get; private set; }
    public double ConfidenceScore { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private ExtractedSkill() { }
    
    public ExtractedSkill(
        string skillName,
        string description,
        double confidenceScore = 1.0,
        List<string>? capabilities = null,
        Dictionary<string, object>? metadata = null)
    {
        Id = Guid.NewGuid();
        SkillName = skillName;
        Description = description;
        Capabilities = capabilities ?? new List<string>();
        ConfidenceScore = Math.Max(0.0, Math.Min(1.0, confidenceScore));
        Metadata = metadata ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetConfidenceScore(double score)
    {
        ConfidenceScore = Math.Max(0.0, Math.Min(1.0, score));
    }
    
    public void AddCapability(string capability)
    {
        if (!string.IsNullOrWhiteSpace(capability) && !Capabilities.Contains(capability))
        {
            Capabilities.Add(capability);
        }
    }
    
    public static ExtractedSkill Create(
        string skillName,
        string description,
        double confidenceScore = 1.0,
        List<string>? capabilities = null,
        Dictionary<string, object>? metadata = null)
    {
        return new ExtractedSkill(skillName, description, confidenceScore, capabilities, metadata);
    }
}
