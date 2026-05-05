namespace Libr4.IDE.Application.SecurityTesting.DTOs;

/// <summary>
/// DTO for SecurityTestResult
/// </summary>
public record SecurityTestResultDto
{
    public int TotalVulnerabilities { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public double SecurityScore { get; init; }
}
