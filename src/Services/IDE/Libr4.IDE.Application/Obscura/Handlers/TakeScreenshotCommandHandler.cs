using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for TakeScreenshotCommand
/// </summary>
public class TakeScreenshotCommandHandler : IRequestHandler<TakeScreenshotCommand, byte[]>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<TakeScreenshotCommandHandler> _logger;

    public TakeScreenshotCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<TakeScreenshotCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task<byte[]> Handle(TakeScreenshotCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Taking screenshot in session {SessionId}", request.SessionId);
        return await _browserService.TakeScreenshotAsync(request.SessionId, ct);
    }
}
