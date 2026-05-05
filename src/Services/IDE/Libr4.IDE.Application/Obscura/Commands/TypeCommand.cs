using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to type text into an element
/// </summary>
public record TypeCommand : IRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string Selector { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}
