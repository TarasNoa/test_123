using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for NavigateCommand
/// </summary>
public class NavigateCommandHandler : IRequestHandler<NavigateCommand>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<NavigateCommandHandler> _logger;

    public NavigateCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<NavigateCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task Handle(NavigateCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Navigating to {Url} in session {SessionId}", request.Url, request.SessionId);
        await _browserService.NavigateAsync(request.SessionId, request.Url, ct);
    }
}
