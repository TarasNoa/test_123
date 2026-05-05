namespace Libr4.IDE.Application.Cascade.DTOs;

/// <summary>
/// DTO for OrchestratorPhase
/// </summary>
public record OrchestratorPhaseDto
{
    public string PhaseId { get; init; } = string.Empty;
    public string PhaseName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Dependencies { get; init; } = new();
    public Dictionary<string, object> PhaseSpecificInstructions { get; init; } = new();
    public string ExpectedOutput { get; init; } = string.Empty;
}
