using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for ExecuteJavaScriptCommand
/// </summary>
public class ExecuteJavaScriptCommandHandler : IRequestHandler<ExecuteJavaScriptCommand, string>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<ExecuteJavaScriptCommandHandler> _logger;

    public ExecuteJavaScriptCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<ExecuteJavaScriptCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task<string> Handle(ExecuteJavaScriptCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Executing JavaScript in session {SessionId}", request.SessionId);
        return await _browserService.ExecuteJavaScriptAsync(request.SessionId, request.Script, ct);
    }
}
