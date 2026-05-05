namespace Libr4.IDE.Application.SecurityTesting.DTOs;

/// <summary>
/// DTO for SecurityTestingAgent
/// </summary>
public record SecurityTestingAgentDto
{
    public Guid Id { get; init; }
    public string TestId { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public List<SecurityVulnerabilityDto> Vulnerabilities { get; init; } = new();
    public SecurityTestResultDto? Result { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
