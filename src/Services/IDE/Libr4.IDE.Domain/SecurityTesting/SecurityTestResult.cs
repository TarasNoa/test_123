namespace Libr4.IDE.Domain.SecurityTesting;

/// <summary>
/// Value object for security test result
/// </summary>
public class SecurityTestResult
{
    public int TotalVulnerabilities { get; private set; }
    public int CriticalCount { get; private set; }
    public int HighCount { get; private set; }
    public int MediumCount { get; private set; }
    public int LowCount { get; private set; }
    public double SecurityScore { get; private set; }
    
    private SecurityTestResult() { }
    
    public SecurityTestResult(
        int totalVulnerabilities,
        int criticalCount,
        int highCount,
        int mediumCount,
        int lowCount,
        double securityScore = 0.0)
    {
        TotalVulnerabilities = totalVulnerabilities;
        CriticalCount = criticalCount;
        HighCount = highCount;
        MediumCount = mediumCount;
        LowCount = lowCount;
        SecurityScore = Math.Max(0.0, Math.Min(100.0, securityScore));
    }
    
    public static SecurityTestResult Create(
        int totalVulnerabilities,
        int criticalCount,
        int highCount,
        int mediumCount,
        int lowCount,
        double securityScore = 0.0)
    {
        return new SecurityTestResult(totalVulnerabilities, criticalCount, highCount, mediumCount, lowCount, securityScore);
    }
}
