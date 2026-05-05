using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Docker;

/// <summary>
/// Live session backed by a running docker container. Commands are executed
/// via <c>docker exec</c>; disposing the session stops and removes the
/// container.
/// </summary>
internal sealed class DockerRuntimeSession : IRuntimeSession
{
    private readonly ILogger _logger;
    private static readonly TimeSpan DefaultExecTimeout = TimeSpan.FromMinutes(5);
    private bool _disposed;

    public string ProviderName => "docker";
    public string SessionId { get; }
    public string HostMountPath { get; }
    public string GuestMountPath { get; }
    public string Image { get; }

    public DockerRuntimeSession(
        string containerName, string image,
        string hostMountPath, string guestMountPath, ILogger logger)
    {
        SessionId = containerName;
        Image = image;
        HostMountPath = hostMountPath;
        GuestMountPath = guestMountPath;
        _logger = logger;
    }

    public async Task<ExecResult> ExecAsync(
        string command,
        string workingSubDirectory,
        IDictionary<string, string>? environmentVariables = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DockerRuntimeSession));
        if (string.IsNullOrWhiteSpace(command))
            return new ExecResult(0, TimeSpan.Zero, Array.Empty<Libr4.IDE.Domain.AutonomousAppGeneration.ConsoleLogEntry>());

        var sub = string.IsNullOrWhiteSpace(workingSubDirectory) ? "." : workingSubDirectory.Replace('\\', '/');
        var workdir = sub == "." ? GuestMountPath : $"{GuestMountPath.TrimEnd('/')}/{sub.TrimStart('/')}";

        // Build environment variable arguments if provided
        var envArgs = string.Empty;
        if (environmentVariables != null && environmentVariables.Count > 0)
        {
            envArgs = string.Join(" ", environmentVariables.Select(kv => $"-e {kv.Key}={kv.Value}"));
        }

        // Use sh -c so agents can use pipes, &&, etc. naturally.
        var args =
            $"exec {envArgs} -w {DockerIsolatedRuntime.Quote(workdir)} {SessionId} sh -c {Escape(command)}";

        var start = DateTime.UtcNow;
        var (exitCode, logs) = await DockerProcess.RunAsync(
            args, timeout ?? DefaultExecTimeout, ct);

        return new ExecResult(exitCode, DateTime.UtcNow - start, logs);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            await DockerProcess.RunAsync(
                $"rm -f {SessionId}", TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove docker container {Container}", SessionId);
        }
    }

    private static string Escape(string command)
    {
        // Wrap in double quotes, escaping any double quote inside.
        // This is more reliable than single quotes for shell commands.
        var escaped = command.Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }
}
