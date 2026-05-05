using MediatR;
using Libr4.IDE.Domain.LLMRouter;
using Libr4.IDE.Application.LLMRouter.DTOs;

namespace Libr4.IDE.Application.LLMRouter.Commands;

/// <summary>
/// Command to route LLM request
/// </summary>
public record RouteLLMCommand : IRequest<LLMRoutingDto>
{
    public string TaskId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public List<LLMModel> AvailableModels { get; init; } = new();
}
