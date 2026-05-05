namespace Libr4.IDE.Domain.ShadowWorkspace;

/// <summary>
/// Value object for validation result
/// </summary>
public class ValidationResult
{
    public ValidationType Type { get; private set; }
    public bool Passed { get; private set; }
    public List<string> Errors { get; private set; }
    public List<string> Warnings { get; private set; }
    public TimeSpan Duration { get; private set; }
    
    private ValidationResult() { }
    
    public ValidationResult(
        ValidationType type,
        bool passed,
        List<string>? errors,
        List<string>? warnings,
        TimeSpan? duration = null)
    {
        Type = type;
        Passed = passed;
        Errors = errors ?? new List<string>();
        Warnings = warnings ?? new List<string>();
        Duration = duration ?? TimeSpan.Zero;
    }
    
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error) && !Errors.Contains(error))
        {
            Errors.Add(error);
            Passed = false;
        }
    }
    
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning) && !Warnings.Contains(warning))
        {
            Warnings.Add(warning);
        }
    }
    
    public static ValidationResult Create(
        ValidationType type,
        bool passed,
        List<string>? errors = null,
        List<string>? warnings = null,
        TimeSpan? duration = null)
    {
        return new ValidationResult(type, passed, errors, warnings, duration);
    }
}
