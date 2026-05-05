namespace Libr4.IDE.Application.SecurityTesting;

/// <summary>
/// Interface for security testing service
/// </summary>
public interface ISecurityTestingService
{
    Task<SecurityTestResult> RunTestsAsync(string targetPath, SecurityTestOptions? options = null, CancellationToken ct = default);
    Task<VulnerabilityReport> ScanDependenciesAsync(string projectPath, CancellationToken ct = default);
}

public class SecurityTestOptions
{
    public bool RunStaticAnalysis { get; set; } = true;
    public bool RunDependencyScan { get; set; } = true;
    public bool RunSecretsScan { get; set; } = true;
    public string[]? ExcludePatterns { get; set; }
}

public class SecurityTestResult
{
    public bool Success { get; set; }
    public Vulnerability[] Vulnerabilities { get; set; } = Array.Empty<Vulnerability>();
    public string[] Logs { get; set; } = Array.Empty<string>();
    public DateTime CompletedAt { get; set; }
}

public class Vulnerability
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class VulnerabilityReport
{
    public int TotalDependencies { get; set; }
    public int VulnerableDependencies { get; set; }
    public Vulnerability[] Vulnerabilities { get; set; } = Array.Empty<Vulnerability>();
}
