using Libr4.IDE.Application.AI.DTOs;
using MediatR;
using Libr4.Shared.Kernel.Results;

namespace Libr4.IDE.Application.AI.Commands;

public record ChatCommand(
    Guid ConversationId,
    string Message,
    string? Provider = null
) : IRequest<Result<AIMessageDTO>>;
