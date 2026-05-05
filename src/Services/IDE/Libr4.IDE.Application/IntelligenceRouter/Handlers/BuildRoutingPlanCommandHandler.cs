/*
using Libr4.IDE.Application.IntelligenceRouter.Commands;
using Libr4.IDE.Application.IntelligenceRouter.DTOs;
using Libr4.IDE.Domain.IntelligenceRouter;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.IntelligenceRouter.Handlers;

public class BuildRoutingPlanCommandHandler : IRequestHandler<BuildRoutingPlanCommand, RoutingPlanDto>
{
    private readonly IRoutingPlanRepository _routingPlanRepository;
    private readonly ILogger<BuildRoutingPlanCommandHandler> _logger;

    public BuildRoutingPlanCommandHandler(
        IRoutingPlanRepository routingPlanRepository,
        ILogger<BuildRoutingPlanCommandHandler> logger)
    {
        _routingPlanRepository = routingPlanRepository;
        _logger = logger;
    }

    public async Task<RoutingPlanDto> Handle(BuildRoutingPlanCommand request, CancellationToken ct)
    {
        var routingPlan = RoutingPlan.Create(
            request.Query,
            request.Capabilities,
            request.Priority);

        await _routingPlanRepository.SaveAsync(routingPlan, ct);

        _logger.LogInformation("Created routing plan {PlanId} for query {Query}", routingPlan.Id, request.Query);

        return new RoutingPlanDto
        {
            Id = routingPlan.Id,
            Query = routingPlan.Query,
            Capabilities = routingPlan.Capabilities,
            Priority = routingPlan.Priority,
            Routes = routingPlan.Routes.Select(r => new RouteDto
            {
                Provider = r.Provider,
                Model = r.Model,
                Confidence = r.Confidence
            }).ToList(),
            CreatedAt = routingPlan.CreatedAt
        };
    }
}
*/
