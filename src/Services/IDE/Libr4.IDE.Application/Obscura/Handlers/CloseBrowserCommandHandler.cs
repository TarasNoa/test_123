using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for CloseBrowserCommand
/// </summary>
public class CloseBrowserCommandHandler : IRequestHandler<CloseBrowserCommand>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<CloseBrowserCommandHandler> _logger;

    public CloseBrowserCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<CloseBrowserCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task Handle(CloseBrowserCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Closing browser in session {SessionId}", request.SessionId);
        await _browserService.CloseBrowserAsync(request.SessionId, ct);
    }
}
