namespace Libr4.IDE.Domain.OrchestrationRun;

/// <summary>
/// Value object representing a skill
/// </summary>
public class Skill
{
    public SkillType SkillType { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<string> Capabilities { get; private set; }
    public Dictionary<string, object> Requirements { get; private set; }
    public bool IsDefault { get; private set; }
    
    private Skill() { }
    
    public Skill(
        SkillType skillType,
        string name,
        string description,
        List<string>? capabilities,
        Dictionary<string, object>? requirements,
        bool isDefault = false)
    {
        SkillType = skillType;
        Name = name;
        Description = description;
        Capabilities = capabilities ?? new List<string>();
        Requirements = requirements ?? new Dictionary<string, object>();
        IsDefault = isDefault;
    }
    
    public void AddCapability(string capability)
    {
        if (!string.IsNullOrWhiteSpace(capability) && !Capabilities.Contains(capability))
        {
            Capabilities.Add(capability);
        }
    }
    
    public void AddRequirement(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Requirements[key] = value;
        }
    }
    
    public static Skill Create(
        SkillType skillType,
        string name,
        string description,
        List<string>? capabilities = null,
        Dictionary<string, object>? requirements = null,
        bool isDefault = false)
    {
        return new Skill(skillType, name, description, capabilities, requirements, isDefault);
    }
}
