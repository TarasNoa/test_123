using MediatR;

namespace Libr4.IDE.Application.Obscura.Commands;

/// <summary>
/// Command to launch a new Obscura browser instance
/// </summary>
public record LaunchBrowserCommand : IRequest<string>
{
    // Additional parameters can be added here (e.g., headless mode, viewport size, etc.)
}
