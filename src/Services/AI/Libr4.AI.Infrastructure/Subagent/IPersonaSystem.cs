namespace Libr4.AI.Infrastructure.Subagent;

/// <summary>
/// Persona System - 32 specialized personas from Claude Octopus
/// Auto-activated based on context
/// </summary>
public interface IPersonaSystem
{
    /// <summary>
    /// Get persona for a given task/context
    /// </summary>
    Task<SubagentPersona?> GetPersonaAsync(string taskDescription, Dictionary<string, object>? context = null);
    
    /// <summary>
    /// Get all available personas
    /// </summary>
    Task<List<SubagentPersona>> GetAllPersonasAsync();
    
    /// <summary>
    /// Add custom persona
    /// </summary>
    Task AddPersonaAsync(SubagentPersona persona);
    
    /// <summary>
    /// Activate persona for session
    /// </summary>
    Task ActivatePersonaAsync(string sessionId, string personaId);
}

public class SubagentPersona
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PersonaCategory Category { get; set; }
    public List<string> Keywords { get; set; } = new();
    public string SystemPrompt { get; set; } = string.Empty;
    public string? PreferredModel { get; set; }
    public List<string> Capabilities { get; set; } = new();
}

public enum PersonaCategory
{
    SoftwareEngineering,
    SpecializedDevelopment,
    DocumentationAndCommunication,
    ResearchAndStrategy,
    BusinessAndCompliance,
    CreativeAndDesign
}
