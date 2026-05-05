using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to click on an element
/// </summary>
public record ClickCommand : IRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string Selector { get; init; } = string.Empty;
}
