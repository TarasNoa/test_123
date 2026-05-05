namespace Libr4.AI.Infrastructure.Harness;

/// <summary>
/// Harness Environment - provides tools and feedback loops for autonomous agent execution
/// Based on "Harness Engineering" pattern from 8 levels of agent engineering
/// </summary>
public interface IHarnessEnvironment
{
    /// <summary>
    /// Execute code in sandbox with automatic validation
    /// </summary>
    Task<HarnessExecutionResult> ExecuteWithValidationAsync(
        string code,
        HarnessValidationRules rules,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Run tests automatically
    /// </summary>
    Task<TestResult> RunTestsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Run linter automatically
    /// </summary>
    Task<LintResult> RunLinterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check code quality
    /// </summary>
    Task<HarnessQualityResult> CheckQualityAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Apply backpressure - automatic error detection and correction
    /// </summary>
    Task<HarnessBackpressureResult> ApplyBackpressureAsync(
        HarnessExecutionResult execution,
        CancellationToken cancellationToken = default);
}

public class HarnessValidationRules
{
    public bool RequireTests { get; set; } = true;
    public bool RequireTypeChecking { get; set; } = true;
    public bool RequireSecurityScan { get; set; } = false;
    public int MaxExecutionTimeMs { get; set; } = 30000;
    public List<string> ForbiddenPatterns { get; set; } = new();
    public List<string> RequiredPatterns { get; set; } = new();
}

public class HarnessExecutionResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<HarnessValidationIssue> ValidationIssues { get; set; } = new();
    public bool NeedsCorrection { get; set; }
}

public class HarnessValidationIssue
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
    public Severity Severity { get; set; }
}

public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}

public class HarnessTestResult
{
    public bool AllPassed { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<string> FailureMessages { get; set; } = new();
}

public class HarnessQualityResult
{
    public bool PassesQualityGate { get; set; }
    public List<QualityIssue> Issues { get; set; } = new();
}

public class QualityIssue
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Severity Severity { get; set; }
}

public class HarnessBackpressureResult
{
    public bool ShouldRetry { get; set; }
    public string? CorrectionSuggestion { get; set; }
    public List<string> ActionsTaken { get; set; } = new();
}

public class TestResult
{
    public bool Passed { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<string> Failures { get; set; } = new();
    public string? Output { get; set; }
}

public class LintResult
{
    public bool Passed { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public List<LintIssue> Issues { get; set; } = new();
    public string? Output { get; set; }
}

public class LintIssue
{
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
