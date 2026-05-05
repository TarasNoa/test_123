using Libr4.AI.Domain.Terminal;

namespace Libr4.IDE.Application.Terminal;

public interface IDockerService
{
    Task<string> ExecuteCommandAsync(
        string containerId,
        string command,
        CancellationToken ct = default);

    Task<string> ExecuteCommandAsync(
        string containerId,
        string[] command,
        string? workingDirectory = null,
        CancellationToken ct = default);

    Task CreateShellSessionAsync(
        string containerId,
        string sessionId,
        ShellType shell,
        CancellationToken ct = default);

    Task<string> GetShellOutputAsync(
        string containerId,
        string sessionId,
        CancellationToken ct = default);

    Task TerminateShellSessionAsync(
        string containerId,
        string sessionId,
        CancellationToken ct = default);
}
