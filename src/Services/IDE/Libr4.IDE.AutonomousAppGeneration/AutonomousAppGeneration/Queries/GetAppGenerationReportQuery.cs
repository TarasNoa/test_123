using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record GetAppGenerationReportQuery(Guid OrchestratorId)
    : IRequest<AppGenerationReportDto?>;
