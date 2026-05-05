namespace Libr4.IDE.Application.AutonomousRuntimePolicy.DTOs;

/// <summary>
/// DTO for RuntimePolicy
/// </summary>
public record RuntimePolicyDto
{
    public Guid Id { get; init; }
    public string PolicyId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public string DomainSignal { get; init; } = string.Empty;
    public string RuntimeEvidenceSignal { get; init; } = string.Empty;
    public bool RuntimeProofRequired { get; init; }
    public bool RichAppBuildRequired { get; init; }
    public QualityContractDto QualityContract { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
