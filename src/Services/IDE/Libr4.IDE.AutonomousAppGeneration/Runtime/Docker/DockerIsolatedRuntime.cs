using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Docker;

/// <summary>
/// Default <see cref="IIsolatedRuntime"/> backed by the Docker daemon.
///
/// Each session is a <c>docker run -d</c> of the requested image with a
/// bind-mount of the host directory onto <c>/workspace</c>. Bind-mount gives
/// us live bidirectional sync between the IDE (host side) and the agents
/// executing inside the container.
/// </summary>
public sealed class DockerIsolatedRuntime : IIsolatedRuntime
{
    private readonly ILogger<DockerIsolatedRuntime> _logger;
    private static readonly TimeSpan DefaultCliTimeout = TimeSpan.FromMinutes(15);

    public string ProviderName => "docker";

    public DockerIsolatedRuntime(ILogger<DockerIsolatedRuntime> logger)
    {
        _logger = logger;
    }

    public async Task<IRuntimeSession> StartSessionAsync(
        string image, string hostMountPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(image)) throw new ArgumentException(nameof(image));
        if (!Directory.Exists(hostMountPath))
            throw new DirectoryNotFoundException($"Host mount path does not exist: {hostMountPath}");

        var containerName = $"libr4-shadow-{Guid.NewGuid():N}";
        const string guestMount = "/workspace";

        // 1. Make sure the image is available locally (docker will pull only if needed).
        var pullArgs = $"pull {Quote(image)}";
        _logger.LogInformation("Pulling image {Image}", image);
        var (pullCode, pullLogs) = await DockerProcess.RunAsync(pullArgs, DefaultCliTimeout, ct);
        if (pullCode != 0)
        {
            var tail = string.Join('\n', pullLogs.TakeLast(20).Select(l => l.Message));
            throw new InvalidOperationException(
                $"docker pull {image} failed (exit {pullCode}). Last output:\n{tail}");
        }

        // 2. Start the container as a long-living shell; agents exec individual commands against it.
        var runArgs =
            $"run -d " +
            $"--name {containerName} " +
            $"--label libr4=shadow " +
            $"-v {Quote(hostMountPath)}:{guestMount} " +
            $"-w {guestMount} " +
            $"--entrypoint sh " +
            $"{Quote(image)} -c \"while true; do sleep 3600; done\"";

        var (runCode, runLogs) = await DockerProcess.RunAsync(runArgs, DefaultCliTimeout, ct);
        if (runCode != 0)
        {
            var tail = string.Join('\n', runLogs.TakeLast(20).Select(l => l.Message));
            throw new InvalidOperationException(
                $"docker run failed (exit {runCode}). Last output:\n{tail}");
        }

        _logger.LogInformation(
            "Started docker session {Container} (image {Image}, mount {Host} -> {Guest})",
            containerName, image, hostMountPath, guestMount);

        return new DockerRuntimeSession(containerName, image, hostMountPath, guestMount, _logger);
    }

    internal static string Quote(string value) =>
        value.Contains(' ') ? $"\"{value}\"" : value;
}
