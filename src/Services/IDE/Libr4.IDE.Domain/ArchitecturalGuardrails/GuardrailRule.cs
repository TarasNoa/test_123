namespace Libr4.IDE.Domain.ArchitecturalGuardrails;

/// <summary>
/// Entity representing a guardrail rule
/// </summary>
public class GuardrailRule
{
    public Guid Id { get; private set; }
    public string RuleName { get; private set; }
    public RuleType Type { get; private set; }
    public string Pattern { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private GuardrailRule() { }
    
    public GuardrailRule(
        string ruleName,
        RuleType type,
        string pattern,
        string description,
        bool isActive = true)
    {
        Id = Guid.NewGuid();
        RuleName = ruleName;
        Type = type;
        Pattern = pattern;
        Description = description;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
    
    public static GuardrailRule Create(
        string ruleName,
        RuleType type,
        string pattern,
        string description,
        bool isActive = true)
    {
        return new GuardrailRule(ruleName, type, pattern, description, isActive);
    }
}
