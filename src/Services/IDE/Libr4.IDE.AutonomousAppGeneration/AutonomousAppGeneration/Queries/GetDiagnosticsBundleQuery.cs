using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

/// <summary>
/// Query to request a diagnostics bundle for a given orchestrator run.
/// </summary>
public sealed record GetDiagnosticsBundleQuery(Guid OrchestratorId)
    : IRequest<DiagnosticsBundleDto?>;
