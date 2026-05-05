using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to get page content (HTML)
/// </summary>
public record GetPageContentCommand : IRequest<string>
{
    public string SessionId { get; init; } = string.Empty;
}
