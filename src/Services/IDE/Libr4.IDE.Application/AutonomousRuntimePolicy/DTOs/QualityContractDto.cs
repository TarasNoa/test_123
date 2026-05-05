namespace Libr4.IDE.Application.AutonomousRuntimePolicy.DTOs;

/// <summary>
/// DTO for QualityContract
/// </summary>
public record QualityContractDto
{
    public bool ApprovalRequired { get; init; }
    public bool AuditTrailRequired { get; init; }
    public List<string> QualityChecks { get; init; } = new();
    public Dictionary<string, object> QualityThresholds { get; init; } = new();
    public string ApprovalWorkflow { get; init; } = string.Empty;
}
