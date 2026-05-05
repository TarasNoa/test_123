namespace Libr4.IDE.Domain.HackerAgent;

/// <summary>
/// Entity representing a security script
/// </summary>
public class SecurityScript
{
    public Guid Id { get; private set; }
    public string ScriptName { get; private set; }
    public ScriptType Type { get; private set; }
    public string ScriptContent { get; private set; }
    public string Description { get; private set; }
    public bool IsCustom { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private SecurityScript() { }
    
    public SecurityScript(
        string scriptName,
        ScriptType type,
        string scriptContent,
        string description = "",
        bool isCustom = true)
    {
        Id = Guid.NewGuid();
        ScriptName = scriptName;
        Type = type;
        ScriptContent = scriptContent;
        Description = description;
        IsCustom = isCustom;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateContent(string scriptContent)
    {
        ScriptContent = scriptContent;
    }
    
    public static SecurityScript Create(
        string scriptName,
        ScriptType type,
        string scriptContent,
        string description = "",
        bool isCustom = true)
    {
        return new SecurityScript(scriptName, type, scriptContent, description, isCustom);
    }
}
