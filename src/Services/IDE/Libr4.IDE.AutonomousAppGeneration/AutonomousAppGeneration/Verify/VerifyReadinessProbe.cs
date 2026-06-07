using System.Diagnostics;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifyReadinessProbe : IVerifyReadinessProbe
{
    private readonly IShadowWorkspaceAccessor? _shadow;
    private readonly IObscuraNetworkRouter? _networkRouter;
    private readonly VerifySubagentOptions _options;
    private readonly ILogger<VerifyReadinessProbe> _logger;

    public VerifyReadinessProbe(
        IOptions<VerifySubagentOptions> options,
        ILogger<VerifyReadinessProbe> logger,
        IShadowWorkspaceAccessor? shadow = null,
        IObscuraNetworkRouter? networkRouter = null)
    {
        _shadow = shadow;
        _networkRouter = networkRouter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VerifyReadinessResult> ProbeAsync(
        VerifySmokeTarget target,
        Guid? shadowWorkspaceId,
        string evidenceDir,
        Guid? runId = null,
        CancellationToken ct = default)
    {
        if (target.Kind == VerifySmokeKind.None || string.IsNullOrWhiteSpace(target.Url))
        {
            return new VerifyReadinessResult(
                target.Name,
                target.Url,
                Ready: true,
                Attempts: Array.Empty<VerifyReadinessAttempt>(),
                TotalElapsed: TimeSpan.Zero);
        }

        var probeUrl = ResolveProbeUrl(target, runId);
        var attempts = new List<VerifyReadinessAttempt>();
        var stopwatch = Stopwatch.StartNew();
        var ready = false;

        for (var attempt = 1; attempt <= _options.ReadinessMaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var attemptStarted = stopwatch.Elapsed;

            try
            {
                var (statusCode, probeError) = await ProbeOnceAsync(probeUrl, target, shadowWorkspaceId, ct)
                    .ConfigureAwait(false);
                var attemptReady = statusCode is >= 200 and < 400;
                attempts.Add(new VerifyReadinessAttempt(
                    target.Name,
                    target.Url,
                    attempt,
                    statusCode,
                    attemptReady,
                    probeError,
                    stopwatch.Elapsed - attemptStarted));

                if (attemptReady)
                {
                    ready = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                attempts.Add(new VerifyReadinessAttempt(
                    target.Name,
                    target.Url,
                    attempt,
                    0,
                    false,
                    ex.Message,
                    stopwatch.Elapsed - attemptStarted));
            }

            if (attempt < _options.ReadinessMaxAttempts)
                await Task.Delay(_options.ReadinessPollIntervalMs, ct).ConfigureAwait(false);
        }

        stopwatch.Stop();
        var result = new VerifyReadinessResult(
            target.Name,
            probeUrl,
            ready,
            attempts,
            stopwatch.Elapsed);

        await PersistReadinessAsync(evidenceDir, result, ct).ConfigureAwait(false);
        return result;
    }

    private string ResolveProbeUrl(VerifySmokeTarget target, Guid? runId)
    {
        if (target.Kind == VerifySmokeKind.Browser && runId is Guid id && _networkRouter is not null)
            return _networkRouter.ResolveForBrowser(id, target.Url);

        if (runId is Guid run && _networkRouter is not null && _networkRouter.TryResolve(run, target.Name, out var serviceUrl))
            return serviceUrl;

        return target.Url;
    }

    private async Task<(int StatusCode, string? Error)> ProbeOnceAsync(
        string probeUrl,
        VerifySmokeTarget target,
        Guid? shadowWorkspaceId,
        CancellationToken ct)
    {
        if (shadowWorkspaceId is Guid workspaceId && _shadow is not null)
        {
            var curl = $"curl -sf -o /dev/null -w '%{{http_code}}' {ShellQuote(target.Url)}";
            var exec = await _shadow.ExecAsync(workspaceId, curl, ct).ConfigureAwait(false);
            var output = string.Join('\n', exec.Logs.Select(l => l.Message)).Trim();
            if (int.TryParse(output, out var code) && code > 0)
                return (code, exec.Succeeded ? null : "curl exited non-zero");

            return (0, string.IsNullOrWhiteSpace(output) ? "empty curl response" : output);
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(_options.ReadinessRequestTimeoutSeconds) };
        using var response = await client.GetAsync(probeUrl, ct).ConfigureAwait(false);
        return ((int)response.StatusCode, response.IsSuccessStatusCode ? null : response.ReasonPhrase);
    }

    private static async Task PersistReadinessAsync(
        string evidenceDir,
        VerifyReadinessResult result,
        CancellationToken ct)
    {
        Directory.CreateDirectory(evidenceDir);
        var path = Path.Combine(evidenceDir, $"readiness-{Sanitize(result.TargetName)}.json");
        var payload = new
        {
            target = result.TargetName,
            url = result.Url,
            ready = result.Ready,
            totalElapsedMs = (int)result.TotalElapsed.TotalMilliseconds,
            attempts = result.Attempts.Select(a => new
            {
                a.Attempt,
                a.StatusCode,
                a.Ready,
                a.Error,
                elapsedMs = (int)a.Elapsed.TotalMilliseconds
            })
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);
    }

    private static string ShellQuote(string value) => $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";

    private static string Sanitize(string name) =>
        string.Concat(name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')).Trim('-');
}
