using MediatR;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura.Commands;

namespace Libr4.IDE.Application.Obscura.Handlers;

/// <summary>
/// Handler for GetPageContentCommand
/// </summary>
public class GetPageContentCommandHandler : IRequestHandler<GetPageContentCommand, string>
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<GetPageContentCommandHandler> _logger;

    public GetPageContentCommandHandler(
        IObscuraBrowserService browserService,
        ILogger<GetPageContentCommandHandler> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task<string> Handle(GetPageContentCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Getting page content in session {SessionId}", request.SessionId);
        return await _browserService.GetPageContentAsync(request.SessionId, ct);
    }
}
