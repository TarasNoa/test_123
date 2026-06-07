using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record GetBuildDiagnosticsDashboardQuery(
    Guid OrchestratorId,
    string? StackFilter = null)
    : IRequest<BuildDiagnosticsDashboardDto?>;
