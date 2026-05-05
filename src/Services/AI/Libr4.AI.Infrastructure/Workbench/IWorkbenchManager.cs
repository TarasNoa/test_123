namespace Libr4.AI.Infrastructure.Workbench;

/// <summary>
/// Workbench Manager - manages cross-project knowledge base
/// Based on "Слепое пятно LLM-разработки: контекст за пределами кода" article
/// </summary>
public interface IWorkbenchManager
{
    /// <summary>
    /// Get workbench path for current workspace
    /// </summary>
    string GetWorkbenchPath();
    
    /// <summary>
    /// Initialize workbench structure
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Get project documentation
    /// </summary>
    Task<string> GetProjectDocAsync(string projectName, string docName);
    
    /// <summary>
    /// Get domain documentation (cross-project)
    /// </summary>
    Task<string> GetDomainDocAsync(string domainName, string docName);
    
    /// <summary>
    /// Get ADR (Architecture Decision Record)
    /// </summary>
    Task<string> GetADRAsync(string adrId);
    
    /// <summary>
    /// Get formatted context for LLM
    /// </summary>
    Task<string> GetContextAsync(string? currentProject = null);
    
    /// <summary>
    /// Add or update project document
    /// </summary>
    Task SetProjectDocAsync(string projectName, string docName, string content);
    
    /// <summary>
    /// Add or update domain document
    /// </summary>
    Task SetDomainDocAsync(string domainName, string docName, string content);
    
    /// <summary>
    /// Create new ADR
    /// </summary>
    Task CreateADRAsync(string title, string status, string context, string decision, string consequences);
    
    /// <summary>
    /// Get SKILL.md content
    /// </summary>
    Task<string?> GetSkillAsync(string skillName);
    
    /// <summary>
    /// List all available skills
    /// </summary>
    Task<List<string>> ListSkillsAsync();
    
    /// <summary>
    /// Add or update skill
    /// </summary>
    Task SetSkillAsync(string skillName, string content);
}
