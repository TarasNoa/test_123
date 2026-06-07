using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

/// <summary>
/// Deterministic Obscura browser smoke flow for verify stage (no LLM).
/// record_start → navigate → wait → snapshot → click → screenshot → console → get_content → record_stop → close
/// </summary>
public sealed class ObscuraVerifySmokeRunner : IObscuraVerifySmokeRunner
{
    private readonly ObscuraBrowserToolFacade _facade;
    private readonly IObscuraNetworkRouter? _networkRouter;
    private readonly IObscuraEvidenceStore? _evidence;
    private readonly IDomToMarkdownConverter? _markdown;
    private readonly VerifySubagentOptions _options;
    private readonly ILogger<ObscuraVerifySmokeRunner> _logger;

    public ObscuraVerifySmokeRunner(
        ObscuraBrowserToolFacade facade,
        IOptions<VerifySubagentOptions> options,
        ILogger<ObscuraVerifySmokeRunner> logger,
        IObscuraNetworkRouter? networkRouter = null,
        IObscuraEvidenceStore? evidence = null,
        IDomToMarkdownConverter? markdown = null)
    {
        _facade = facade;
        _options = options.Value;
        _logger = logger;
        _networkRouter = networkRouter;
        _evidence = evidence;
        _markdown = markdown;
    }

    public async Task<ObscuraVerifySmokeResult> RunBrowserTargetsAsync(
        Guid runId,
        IReadOnlyList<VerifySmokeTarget> targets,
        CancellationToken ct = default)
    {
        var browserTargets = targets
            .Where(t => t.Kind == VerifySmokeKind.Browser && !string.IsNullOrWhiteSpace(t.Url))
            .ToList();

        if (browserTargets.Count == 0)
        {
            return new ObscuraVerifySmokeResult(
                true,
                "no browser smoke targets",
                Array.Empty<ObscuraVerifySmokeTargetResult>());
        }

        var results = new List<ObscuraVerifySmokeTargetResult>();
        foreach (var target in browserTargets)
        {
            var result = await RunSingleTargetAsync(runId, target, ct).ConfigureAwait(false);
            results.Add(result);
        }

        var passed = results.All(r => r.Passed);
        var summary = passed
            ? $"obscura smoke passed ({results.Count} targets)"
            : $"obscura smoke failed: {string.Join("; ", results.Where(r => !r.Passed).Select(r => r.TargetName))}";

        return new ObscuraVerifySmokeResult(passed, summary, results);
    }

    private async Task<ObscuraVerifySmokeTargetResult> RunSingleTargetAsync(
        Guid runId,
        VerifySmokeTarget target,
        CancellationToken ct)
    {
        var evidencePaths = new List<string>();
        var steps = new List<string>();
        string? sessionId = null;

        try
        {
            var context = BuildContext(runId);
            var resolvedUrl = _networkRouter?.ResolveForBrowser(runId, target.Url) ?? target.Url;

            var launch = await _facade.LaunchAsync(context, "verify-smoke", ct).ConfigureAwait(false);
            if (!launch.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"launch failed: {launch.Error}");

            sessionId = launch.SessionId;
            steps.Add("launch");

            var recordStart = await _facade.RecordStartAsync(sessionId, runId, ct).ConfigureAwait(false);
            if (!recordStart.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"record_start failed: {recordStart.Error}");
            steps.Add("record_start");

            var navigate = await _facade.NavigateAsync(sessionId, resolvedUrl, ct, runId).ConfigureAwait(false);
            if (!navigate.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"navigate failed: {navigate.Error}");
            steps.Add("navigate");

            var wait = await _facade.WaitAsync(
                sessionId,
                _options.ObscuraSmokeWaitSelector,
                _options.ObscuraSmokeWaitTimeoutMs,
                ct).ConfigureAwait(false);
            if (!wait.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"wait failed: {wait.Error}");
            steps.Add("wait");

            var snapshot = await _facade.SnapshotAsync(sessionId, ct).ConfigureAwait(false);
            if (!snapshot.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"snapshot failed: {snapshot.Error}");
            steps.Add("snapshot");

            var clickSelector = TryPickClickSelector(snapshot.Output) ?? _options.ObscuraSmokeClickSelector;
            if (!string.IsNullOrWhiteSpace(clickSelector))
            {
                var click = await _facade.ClickAsync(sessionId, clickSelector, ct).ConfigureAwait(false);
                if (click.Ok)
                    steps.Add($"click:{clickSelector}");
                else
                    _logger.LogDebug(
                        "[ObscuraSmoke {RunId}] Optional click skipped for {Target}: {Error}",
                        runId,
                        target.Name,
                        click.Error);
            }

            var screenshot = await _facade.ScreenshotAsync(sessionId, ct).ConfigureAwait(false);
            if (!screenshot.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"screenshot failed: {screenshot.Error}");
            steps.Add("screenshot");
            var screenshotPath = await PersistScreenshotAsync(runId, screenshot.Output, ct).ConfigureAwait(false);
            if (screenshotPath is not null)
                evidencePaths.Add(screenshotPath);

            var console = await _facade.ConsoleAsync(sessionId, ct).ConfigureAwait(false);
            if (!console.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"console failed: {console.Error}");
            steps.Add("console");
            var consolePath = await PersistConsoleAsync(runId, console.Output, ct).ConfigureAwait(false);
            if (consolePath is not null)
                evidencePaths.Add(consolePath);

            var content = await _facade.GetContentAsync(sessionId, asMarkdown: true, _markdown, ct)
                .ConfigureAwait(false);
            if (!content.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"get_content failed: {content.Error}");
            steps.Add("get_content");
            var domPath = await PersistDomSnapshotAsync(runId, content.Output, ct).ConfigureAwait(false);
            if (domPath is not null)
                evidencePaths.Add(domPath);

            var recordStop = await _facade.RecordStopAsync(sessionId, runId, stepNumber: 1, ct).ConfigureAwait(false);
            if (!recordStop.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"record_stop failed: {recordStop.Error}");
            steps.Add("record_stop");
            if (ExtractPathValue(recordStop.Output, "path") is { } videoPath)
                evidencePaths.Add(videoPath);

            var close = await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);
            if (!close.Ok)
                return Fail(target, resolvedUrl, evidencePaths, $"close failed: {close.Error}");
            steps.Add("close");

            var summary = $"PASS {target.Name} steps=[{string.Join(",", steps)}] evidence={evidencePaths.Count}";
            _logger.LogInformation("[ObscuraSmoke {RunId}] {Summary}", runId, summary);
            return new ObscuraVerifySmokeTargetResult(target.Name, resolvedUrl, true, summary, evidencePaths);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ObscuraSmoke {RunId}] Smoke failed for {Target}", runId, target.Name);
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);
                }
                catch
                {
                    // best-effort cleanup
                }
            }

            return Fail(target, target.Url, evidencePaths, ex.Message);
        }
    }

    private static ObscuraVerifySmokeTargetResult Fail(
        VerifySmokeTarget target,
        string url,
        IReadOnlyList<string> evidencePaths,
        string reason) =>
        new(target.Name, url, false, $"FAIL {target.Name}: {reason}", evidencePaths);

    private static ToolContext BuildContext(Guid runId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-obscura-smoke-" + runId.ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new SmokeRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId, CurrentStepNumber = 1 },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    private sealed class SmokeRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "smoke";
        public string SessionId => "smoke";
        public string HostMountPath => string.Empty;
        public string GuestMountPath => "/workspace";
        public string Image => "smoke";
        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private async Task<string?> PersistScreenshotAsync(Guid runId, string output, CancellationToken ct)
    {
        if (_evidence is null)
            return null;

        var b64 = ExtractBase64(output);
        if (string.IsNullOrWhiteSpace(b64))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(b64);
            var artifact = await _evidence.PersistAsync(
                runId,
                ObscuraEvidenceKind.Screenshot,
                bytes,
                new ObscuraEvidencePersistOptions(
                    LogicalName: "screenshot-smoke",
                    StepNumber: 1,
                    ToolName: BrowserToolNames.Screenshot,
                    MirrorToVerifyFileNames: ["screenshot-final.png"]),
                ct).ConfigureAwait(false);
            return artifact.AbsolutePath;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> PersistConsoleAsync(Guid runId, string output, CancellationToken ct)
    {
        if (_evidence is null || string.IsNullOrWhiteSpace(output))
            return null;

        var artifact = await _evidence.PersistTextAsync(
            runId,
            ObscuraEvidenceKind.ConsoleLog,
            output,
            new ObscuraEvidencePersistOptions(
                LogicalName: "console-smoke",
                StepNumber: 1,
                ToolName: BrowserToolNames.Console,
                MirrorToVerifyFileNames: ["console-errors.json"]),
            ct).ConfigureAwait(false);
        return artifact.AbsolutePath;
    }

    private async Task<string?> PersistDomSnapshotAsync(Guid runId, string output, CancellationToken ct)
    {
        if (_evidence is null || string.IsNullOrWhiteSpace(output))
            return null;

        var artifact = await _evidence.PersistTextAsync(
            runId,
            ObscuraEvidenceKind.DomSnapshot,
            output,
            new ObscuraEvidencePersistOptions(
                LogicalName: "dom-smoke",
                StepNumber: 1,
                ToolName: BrowserToolNames.GetContent,
                MirrorToVerifyFileNames: ["dom-snapshot.md"]),
            ct).ConfigureAwait(false);
        return artifact.AbsolutePath;
    }

    public static string? TryPickClickSelector(string snapshotJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(snapshotJson);
            if (!doc.RootElement.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var node in nodes.EnumerateArray())
            {
                if (!node.TryGetProperty("tag", out var tagEl) || tagEl.ValueKind != JsonValueKind.String)
                    continue;

                var tag = tagEl.GetString();
                if (tag is not ("button" or "a"))
                    continue;

                if (node.TryGetProperty("selector", out var selectorEl) && selectorEl.ValueKind == JsonValueKind.String)
                {
                    var selector = selectorEl.GetString();
                    if (!string.IsNullOrWhiteSpace(selector))
                        return selector;
                }
            }
        }
        catch
        {
            // snapshot parse is best-effort
        }

        return null;
    }

    private static string? ExtractBase64(string output)
    {
        const string prefix = "base64=";
        var idx = output.IndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0)
            return null;
        return output[(idx + prefix.Length)..].Trim();
    }

    private static string? ExtractPathValue(string output, string key)
    {
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith(key + '=', StringComparison.OrdinalIgnoreCase))
                return line[(key.Length + 1)..];
        }

        return null;
    }
}
