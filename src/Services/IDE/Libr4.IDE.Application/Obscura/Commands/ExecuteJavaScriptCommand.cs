using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to execute JavaScript in the browser context
/// </summary>
public record ExecuteJavaScriptCommand : IRequest<string>
{
    public string SessionId { get; init; } = string.Empty;
    public string Script { get; init; } = string.Empty;
}
