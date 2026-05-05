using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for ClickCommand
/// </summary>
public class ClickCommandHandler : IRequestHandler<ClickCommand>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<ClickCommandHandler> _logger;

    public ClickCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<ClickCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task Handle(ClickCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Clicking element {Selector} in session {SessionId}", request.Selector, request.SessionId);
        await _browserService.ClickAsync(request.SessionId, request.Selector, ct);
    }
}
