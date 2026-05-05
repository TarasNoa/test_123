using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to close a browser instance
/// </summary>
public record CloseBrowserCommand : IRequest
{
    public string SessionId { get; init; } = string.Empty;
}
