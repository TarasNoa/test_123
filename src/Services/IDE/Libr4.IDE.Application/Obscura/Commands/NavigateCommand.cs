using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to navigate to a URL in an Obscura browser instance
/// </summary>
public record NavigateCommand : IRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}
