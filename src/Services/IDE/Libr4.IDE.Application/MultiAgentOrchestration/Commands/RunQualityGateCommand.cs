using MediatR;

namespace Libr4.IDE.Application.MultiAgentOrchestration.Commands;

/// <summary>
/// Command to run a quality gate evaluation
/// </summary>
public record RunQualityGateCommand(
    string GateId,
    string PhaseId,
    Dictionary<string, object> EvaluationContext
) : IRequest<QualityGateResult>;

/// <summary>
/// Result of quality gate evaluation
/// </summary>
public record QualityGateResult
{
    public string GateId { get; set; } = string.Empty;
    public string PhaseId { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public List<CriterionResult> CriterionResults { get; set; } = new();
    public string? FailureReason { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public TimeSpan EvaluationDuration { get; set; }
}

/// <summary>
/// Result of criterion evaluation
/// </summary>
public record CriterionResult
{
    public string CriterionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Evidence { get; set; }
    public string? FailureReason { get; set; }
}
