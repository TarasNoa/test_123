/*
using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for WaitForElementCommand
/// </summary>
public class WaitForElementCommandHandler : IRequestHandler<WaitForElementCommand>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<WaitForElementCommandHandler> _logger;

    public WaitForElementCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<WaitForElementCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task Handle(WaitForElementCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Waiting for element {Selector} in session {SessionId}", request.Selector, request.SessionId);
        await _browserService.WaitForElementAsync(request.SessionId, request.Selector, ct);
    }
}
*/
