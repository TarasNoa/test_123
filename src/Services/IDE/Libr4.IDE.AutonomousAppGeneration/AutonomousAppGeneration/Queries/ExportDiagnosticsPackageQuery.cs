using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record ExportDiagnosticsPackageQuery(Guid OrchestratorId)
    : IRequest<DiagnosticsPackageExportDto?>;
