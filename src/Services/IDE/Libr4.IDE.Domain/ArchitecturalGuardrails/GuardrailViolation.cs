namespace Libr4.IDE.Domain.ArchitecturalGuardrails;

/// <summary>
/// Value object for guardrail violation
/// </summary>
public class GuardrailViolation
{
    public Guid Id { get; private set; }
    public GuardrailRule Rule { get; private set; }
    public string FilePath { get; private set; }
    public int LineNumber { get; private set; }
    public string Message { get; private set; }
    public string Severity { get; private set; }
    
    private GuardrailViolation() { }
    
    public GuardrailViolation(
        GuardrailRule rule,
        string filePath,
        int lineNumber,
        string message,
        string severity = "warning")
    {
        Id = Guid.NewGuid();
        Rule = rule;
        FilePath = filePath;
        LineNumber = lineNumber;
        Message = message;
        Severity = severity;
    }
    
    public static GuardrailViolation Create(
        GuardrailRule rule,
        string filePath,
        int lineNumber,
        string message,
        string severity = "warning")
    {
        return new GuardrailViolation(rule, filePath, lineNumber, message, severity);
    }
}
