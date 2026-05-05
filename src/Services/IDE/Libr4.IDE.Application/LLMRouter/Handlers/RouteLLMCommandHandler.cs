/*
using Libr4.IDE.Application.LLMRouter.Commands;
using Libr4.IDE.Application.LLMRouter.DTOs;
using Libr4.IDE.Domain.LLMRouter;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.LLMRouter.Handlers;

public class RouteLLMCommandHandler : IRequestHandler<RouteLLMCommand, LLMDestinationDto>
{
    private readonly ILLMDestinationRepository _destinationRepository;
    private readonly ILogger<RouteLLMCommandHandler> _logger;

    public RouteLLMCommandHandler(
        ILLMDestinationRepository destinationRepository,
        ILogger<RouteLLMCommandHandler> logger)
    {
        _destinationRepository = destinationRepository;
        _logger = logger;
    }

    public async Task<LLMDestinationDto> Handle(RouteLLMCommand request, CancellationToken ct)
    {
        var destination = LLMDestination.Select(
            request.Query,
            request.Capabilities,
            request.Priority);

        await _destinationRepository.SaveAsync(destination, ct);

        _logger.LogInformation("Routed LLM request to {Provider} using {Model}", destination.Provider, destination.Model);

        return new LLMDestinationDto
        {
            Id = destination.Id,
            Provider = destination.Provider,
            Model = destination.Model,
            Confidence = destination.Confidence,
            Reason = destination.Reason,
            CreatedAt = destination.CreatedAt
        };
    }
}
*/
