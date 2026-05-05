namespace Libr4.IDE.Domain.SeniorRolePrompts;

/// <summary>
/// Value object representing a senior role
/// </summary>
public class SeniorRole
{
    public string RoleTitle { get; private set; }
    public string Description { get; private set; }
    public List<string> Responsibilities { get; private set; }
    public List<string> Capabilities { get; private set; }
    public string ReviewProfile { get; private set; }
    
    private SeniorRole() { }
    
    public SeniorRole(
        string roleTitle,
        string description,
        List<string>? responsibilities = null,
        List<string>? capabilities = null,
        string reviewProfile = "general")
    {
        RoleTitle = roleTitle;
        Description = description;
        Responsibilities = responsibilities ?? new List<string>();
        Capabilities = capabilities ?? new List<string>();
        ReviewProfile = reviewProfile;
    }
    
    public void AddResponsibility(string responsibility)
    {
        if (!string.IsNullOrWhiteSpace(responsibility) && !Responsibilities.Contains(responsibility))
        {
            Responsibilities.Add(responsibility);
        }
    }
    
    public void AddCapability(string capability)
    {
        if (!string.IsNullOrWhiteSpace(capability) && !Capabilities.Contains(capability))
        {
            Capabilities.Add(capability);
        }
    }
    
    public static SeniorRole Create(
        string roleTitle,
        string description,
        List<string>? responsibilities = null,
        List<string>? capabilities = null,
        string reviewProfile = "general")
    {
        return new SeniorRole(roleTitle, description, responsibilities, capabilities, reviewProfile);
    }
}
