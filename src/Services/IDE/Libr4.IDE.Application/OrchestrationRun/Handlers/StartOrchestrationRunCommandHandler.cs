/*
using Libr4.IDE.Application.OrchestrationRun.Commands;
using Libr4.IDE.Application.OrchestrationRun.DTOs;
using Libr4.IDE.Domain.OrchestrationRun;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.OrchestrationRun.Handlers;

public class StartOrchestrationRunCommandHandler : IRequestHandler<StartOrchestrationRunCommand, OrchestrationRunDto>
{
    private readonly IOrchestrationRunRepository _runRepository;
    private readonly ILogger<StartOrchestrationRunCommandHandler> _logger;

    public StartOrchestrationRunCommandHandler(
        IOrchestrationRunRepository runRepository,
        ILogger<StartOrchestrationRunCommandHandler> logger)
    {
        _runRepository = runRepository;
        _logger = logger;
    }

    public async Task<OrchestrationRunDto> Handle(StartOrchestrationRunCommand request, CancellationToken ct)
    {
        var run = OrchestrationRun.Create(
            request.OrchestrationId,
            request.Parameters,
            request.Timeout);

        await _runRepository.SaveAsync(run, ct);

        _logger.LogInformation("Started orchestration run {RunId} for orchestration {OrchestrationId}", run.Id, request.OrchestrationId);

        return new OrchestrationRunDto
        {
            Id = run.Id,
            OrchestrationId = run.OrchestrationId,
            Parameters = run.Parameters,
            Status = run.Status,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Result = run.Result
        };
    }
}
*/
