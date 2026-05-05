using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetAppGenerationManifestQueryHandler
    : IRequestHandler<GetAppGenerationManifestQuery, ExecutionManifestDto?>
{
    private readonly IAppGenerationRepository _repository;
    private readonly IExecutionManifestBuilder _manifestBuilder;

    public GetAppGenerationManifestQueryHandler(
        IAppGenerationRepository repository,
        IExecutionManifestBuilder manifestBuilder)
    {
        _repository = repository;
        _manifestBuilder = manifestBuilder;
    }

    public async Task<ExecutionManifestDto?> Handle(
        GetAppGenerationManifestQuery request,
        CancellationToken ct)
    {
        var orchestrator = await _repository.GetAsync(request.OrchestratorId, ct);
        if (orchestrator is null)
            return null;

        return await _manifestBuilder.BuildAndPersistAsync(orchestrator, ct);
    }
}
