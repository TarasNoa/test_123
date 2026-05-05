using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to wait for an element to appear
/// </summary>
public record WaitForElementCommand : IRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string Selector { get; init; } = string.Empty;
}
