using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for TypeCommand
/// </summary>
public class TypeCommandHandler : IRequestHandler<TypeCommand>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<TypeCommandHandler> _logger;

    public TypeCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<TypeCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task Handle(TypeCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Typing text into element {Selector} in session {SessionId}", request.Selector, request.SessionId);
        await _browserService.TypeAsync(request.SessionId, request.Selector, request.Text, ct);
    }
}
