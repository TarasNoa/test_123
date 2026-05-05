using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetDiagnosticsBundleQueryHandler
    : IRequestHandler<GetDiagnosticsBundleQuery, DiagnosticsBundleDto?>
{
    private readonly IDiagnosticsBundleService _diagnosticsService;

    public GetDiagnosticsBundleQueryHandler(IDiagnosticsBundleService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    public async Task<DiagnosticsBundleDto?> Handle(
        GetDiagnosticsBundleQuery request,
        CancellationToken ct)
    {
        return await _diagnosticsService.GenerateBundleAsync(request.OrchestratorId, ct);
    }
}
