using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to take a screenshot of the current page
/// </summary>
public record TakeScreenshotCommand : IRequest<byte[]>
{
    public string SessionId { get; init; } = string.Empty;
}
