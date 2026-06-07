using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public sealed record IsolatedExternalBackendOutcome(
    int ExitCode,
    IReadOnlyList<string> StdoutLines,
    string? Stderr,
    TimeSpan Duration);

public sealed class IsolatedExternalBackendRunner
{
    private readonly IIsolatedRuntime _runtime;
    private readonly ExternalAgentBackendOptions _options;
    private readonly ILogger<IsolatedExternalBackendRunner> _logger;

    public IsolatedExternalBackendRunner(
        IIsolatedRuntime runtime,
        ExternalAgentBackendOptions options,
        ILogger<IsolatedExternalBackendRunner> logger)
    {
        _runtime = runtime;
        _options = options;
        _logger = logger;
    }

    public async Task<(IsolatedExternalBackendOutcome Outcome, IRuntimeSession Session)> RunAsync(
        string workspace,
        string shellCommand,
        IDictionary<string, string>? environmentVariables,
        TimeSpan timeout,
        CancellationToken ct)
    {
        if (!Directory.Exists(workspace))
            throw new DirectoryNotFoundException($"workspace_not_found:{workspace}");

        var session = await _runtime.StartSessionAsync(
            _options.ExternalBackendRuntimeImage,
            Path.GetFullPath(workspace),
            ct).ConfigureAwait(false);

        try
        {
            _logger.LogInformation(
                "Running isolated external backend command in {Provider} session {SessionId} image={Image}",
                session.ProviderName,
                session.SessionId,
                session.Image);

            var result = await session.ExecAsync(
                shellCommand,
                workingSubDirectory: ".",
                environmentVariables,
                timeout,
                ct).ConfigureAwait(false);

            var stdoutLines = result.Logs
                .Where(l => string.Equals(l.Stream, "stdout", StringComparison.OrdinalIgnoreCase))
                .SelectMany(l => l.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToList();

            var stderr = string.Join(
                Environment.NewLine,
                result.Logs
                    .Where(l => string.Equals(l.Stream, "stderr", StringComparison.OrdinalIgnoreCase))
                    .Select(l => l.Message));

            var outcome = new IsolatedExternalBackendOutcome(
                result.ExitCode,
                stdoutLines,
                string.IsNullOrWhiteSpace(stderr) ? null : stderr,
                result.Duration);

            return (outcome, session);
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public static string BuildShellCommand(string executable, IReadOnlyList<string> arguments)
    {
        static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            return value.Contains(' ') || value.Contains('"') || value.Contains('\t')
                ? $"\"{value.Replace("\"", "\\\"")}\""
                : value;
        }

        return string.Join(" ", new[] { Quote(executable) }.Concat(arguments.Select(Quote)));
    }
}
