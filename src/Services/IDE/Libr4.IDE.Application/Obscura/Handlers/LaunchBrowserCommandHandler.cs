using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for LaunchBrowserCommand
/// </summary>
public class LaunchBrowserCommandHandler : IRequestHandler<LaunchBrowserCommand, string>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<LaunchBrowserCommandHandler> _logger;

    public LaunchBrowserCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<LaunchBrowserCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task<string> Handle(LaunchBrowserCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Launching Obscura browser");
        var sessionId = await _browserService.LaunchBrowserAsync(ct);
        return sessionId;
    }
}
