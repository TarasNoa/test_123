using MediatR;
using Libr4.IDE.Application.IntelligenceRouter.DTOs;

namespace Libr4.IDE.Application.IntelligenceRouter.Commands;

/// <summary>
/// Command to build a routing plan for multi-phase execution
/// </summary>
public record BuildRoutingPlanCommand : IRequest<RoutingPlanDto>
{
    public string Prompt { get; init; } = string.Empty;
    public List<object> Phases { get; init; } = new();
    public string DomainClass { get; init; } = "Standard";
    public string RiskLevel { get; init; } = "medium";
    public List<string> ContextFiles { get; init; } = new();
}
