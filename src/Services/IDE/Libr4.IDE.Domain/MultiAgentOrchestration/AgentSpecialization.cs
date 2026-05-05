namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Agent domain category (from AI-IDE-Agent)
/// </summary>
public enum AgentDomainCategory
{
    Programming,
    CloudDevOps,
    DataAI,
    BusinessProduct,
    SecurityQuality,
    MobileGame
}

/// <summary>
/// Agent specialization with domain knowledge (from AI-IDE-Agent)
/// </summary>
public class AgentSpecialization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public AgentDomainCategory Category { get; private set; }
    public List<string> Expertise { get; private set; }
    public string PromptTemplate { get; private set; }
    public List<string> BestPractices { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int UsageCount { get; private set; }
    
    public AgentSpecialization(
        string name,
        string description,
        AgentDomainCategory category,
        string promptTemplate)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Category = category;
        PromptTemplate = promptTemplate;
        Expertise = new List<string>();
        BestPractices = new List<string>();
        CreatedAt = DateTime.UtcNow;
        UsageCount = 0;
    }
    
    public void AddExpertise(string expertise)
    {
        Expertise.Add(expertise);
    }
    
    public void AddBestPractice(string practice)
    {
        BestPractices.Add(practice);
    }
    
    public void RecordUsage()
    {
        UsageCount++;
    }
    
    public string GetPromptWithContext(Dictionary<string, object> context)
    {
        var prompt = PromptTemplate;
        foreach (var kvp in context)
        {
            prompt = prompt.Replace($"{{{kvp.Key}}}", kvp.Value.ToString());
        }
        return prompt;
    }
}

/// <summary>
/// Agent specialization registry (from AI-IDE-Agent categorization)
/// </summary>
public class AgentSpecializationRegistry
{
    public List<AgentSpecialization> Specializations { get; private set; }
    
    public AgentSpecializationRegistry()
    {
        Specializations = new List<AgentSpecialization>();
    }
    
    public void RegisterSpecialization(AgentSpecialization specialization)
    {
        Specializations.Add(specialization);
    }
    
    public List<AgentSpecialization> GetSpecializationsByCategory(AgentDomainCategory category)
    {
        return Specializations.Where(s => s.Category == category).ToList();
    }
    
    public AgentSpecialization? GetSpecializationByName(string name)
    {
        return Specializations.FirstOrDefault(s => s.Name == name);
    }
    
    public List<AgentSpecialization> GetMostUsedSpecializations(int topN = 5)
    {
        return Specializations.OrderByDescending(s => s.UsageCount).Take(topN).ToList();
    }
}
