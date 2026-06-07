using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

/// <summary>
/// Wires browser_* agent tools to IAgentEventEmitter, rollout.jsonl (with media[]), and NDJSON events.
/// </summary>
public sealed class BrowserToolEventHook : IAgentToolHook
{
    private readonly IAgentEventEmitter _events;
    private readonly IRolloutRecorder _rollout;
    private readonly INdjsonEventWriter _ndjson;
    private readonly IObscuraEvidenceStore? _evidence;
    private readonly AgentRuntimeOptions _options;

    public BrowserToolEventHook(
        IAgentEventEmitter events,
        IRolloutRecorder rollout,
        INdjsonEventWriter ndjson,
        IOptions<AgentRuntimeOptions> options,
        IObscuraEvidenceStore? evidence = null)
    {
        _events = events;
        _rollout = rollout;
        _ndjson = ndjson;
        _evidence = evidence;
        _options = options.Value;
    }

    public int Order => 100;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct) =>
        ValueTask.CompletedTask;

    public async ValueTask OnAfterToolAsync(
        IAgentTool tool,
        ToolContext context,
        ToolExecutionResult result,
        CancellationToken ct)
    {
        if (!IsBrowserTool(tool.Name) || context.Session.RunId is not Guid runId)
            return;

        ObscuraTelemetry.RecordAction(tool.Name, result.Success);

        var sessionId = ExtractSessionId(context, result.Output);
        var detail = ExtractDetail(tool.Name, context, result.Output);
        var media = await BuildMediaAsync(runId, tool.Name, context.Session.CurrentStepNumber, result, ct)
            .ConfigureAwait(false);

        await _events.EmitBrowserToolAsync(runId, tool.Name, sessionId, result.Success, detail).ConfigureAwait(false);

        await _rollout.RecordToolUseAsync(
            runId,
            context.Session.SessionId ?? sessionId,
            context.Session.CurrentStepNumber,
            tool.Name,
            context.Session.LastToolInputJson ?? context.ToolInput.GetRawText(),
            TruncateOutput(result.Output),
            result.Success,
            context.Session.LastToolDurationMs,
            media,
            ct).ConfigureAwait(false);

        if (_options.EnableNdjsonEvents)
        {
            await _ndjson.WriteAsync(runId, new
            {
                type = "tool_use",
                sessionId = context.Session.SessionId,
                stepNumber = context.Session.CurrentStepNumber,
                toolName = tool.Name,
                success = result.Success,
                timing = new { durationMs = context.Session.LastToolDurationMs },
                media = media?.Select(m => new { path = m.Path, url = m.Url, kind = m.Kind }).ToArray(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, ct).ConfigureAwait(false);
        }
    }

    public static bool IsBrowserTool(string toolName) =>
        toolName.StartsWith("browser_", StringComparison.OrdinalIgnoreCase)
        || BrowserToolNames.All.Contains(toolName, StringComparer.OrdinalIgnoreCase);

    private static string ExtractSessionId(ToolContext context, string output)
    {
        if (ObscuraBrowserToolFacade.TryGetSessionId(context.ToolInput, out var fromInput))
            return fromInput;

        const string prefix = "session_id=";
        var idx = output.IndexOf(prefix, StringComparison.Ordinal);
        if (idx >= 0)
        {
            var rest = output[(idx + prefix.Length)..];
            var end = rest.IndexOfAny(['\n', '\r', ' ']);
            return end > 0 ? rest[..end] : rest.Trim();
        }

        return context.Session.SessionId ?? "unknown";
    }

    private static string? ExtractDetail(string toolName, ToolContext context, string output) =>
        toolName switch
        {
            "browser_navigate" => ObscuraBrowserToolFacade.GetString(context.ToolInput, "url"),
            "browser_execute_js" => ObscuraBrowserToolFacade.GetString(context.ToolInput, "script"),
            "browser_click" or "browser_wait" or "browser_type" =>
                ObscuraBrowserToolFacade.GetString(context.ToolInput, "selector"),
            _ => output.Length > 200 ? output[..200] : output
        };

    private async Task<IReadOnlyList<RolloutMediaAttachment>?> BuildMediaAsync(
        Guid runId,
        string toolName,
        int stepNumber,
        ToolExecutionResult result,
        CancellationToken ct)
    {
        if (!result.Success)
            return null;

        if (toolName == BrowserToolNames.RecordStop)
            return await BuildVideoMediaAsync(runId, stepNumber, result, ct).ConfigureAwait(false);

        if (toolName == BrowserToolNames.GetContent)
            return await BuildDomSnapshotMediaAsync(runId, stepNumber, result, ct).ConfigureAwait(false);

        if (toolName == BrowserToolNames.Console)
            return await BuildConsoleMediaAsync(runId, stepNumber, result, ct).ConfigureAwait(false);

        if (toolName != BrowserToolNames.Screenshot)
            return null;

        var b64 = ExtractBase64(result.Output);
        if (string.IsNullOrWhiteSpace(b64))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(b64);
            return await PersistMediaAsync(
                runId,
                stepNumber,
                ObscuraEvidenceKind.Screenshot,
                bytes,
                $"screenshot-step{stepNumber}",
                BrowserToolNames.Screenshot,
                ["screenshot-final.png"],
                "screenshot",
                ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<RolloutMediaAttachment>?> BuildVideoMediaAsync(
        Guid runId,
        int stepNumber,
        ToolExecutionResult result,
        CancellationToken ct)
    {
        var path = ExtractPathValue(result.Output, "path");
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            var file = Path.GetFileName(path);
            return
            [
                new RolloutMediaAttachment(path, $"/api/ide/app-generation/{runId:D}/obscura/artifacts/{file}", "video")
            ];
        }

        var b64 = ExtractBase64(result.Output);
        if (string.IsNullOrWhiteSpace(b64))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(b64);
            return await PersistMediaAsync(
                runId,
                stepNumber,
                ObscuraEvidenceKind.Video,
                bytes,
                $"recording-step{stepNumber}",
                BrowserToolNames.RecordStop,
                ["smoke.webm"],
                "video",
                ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<RolloutMediaAttachment>?> BuildDomSnapshotMediaAsync(
        Guid runId,
        int stepNumber,
        ToolExecutionResult result,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(result.Output))
            return null;

        var text = result.Output;
        return await PersistMediaAsync(
            runId,
            stepNumber,
            ObscuraEvidenceKind.DomSnapshot,
            Encoding.UTF8.GetBytes(text),
            $"dom-snapshot-step{stepNumber}",
            BrowserToolNames.GetContent,
            ["dom-snapshot.md"],
            "dom",
            ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<RolloutMediaAttachment>?> BuildConsoleMediaAsync(
        Guid runId,
        int stepNumber,
        ToolExecutionResult result,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(result.Output))
            return null;

        return await PersistMediaAsync(
            runId,
            stepNumber,
            ObscuraEvidenceKind.ConsoleLog,
            Encoding.UTF8.GetBytes(result.Output),
            $"console-step{stepNumber}",
            BrowserToolNames.Console,
            ["console-errors.json"],
            "console",
            ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<RolloutMediaAttachment>?> PersistMediaAsync(
        Guid runId,
        int stepNumber,
        ObscuraEvidenceKind kind,
        byte[] bytes,
        string logicalName,
        string toolName,
        IReadOnlyList<string> mirrorToVerify,
        string mediaKind,
        CancellationToken ct)
    {
        if (_evidence is not null)
        {
            var artifact = await _evidence.PersistAsync(
                runId,
                kind,
                bytes,
                new ObscuraEvidencePersistOptions(
                    LogicalName: logicalName,
                    StepNumber: stepNumber,
                    ToolName: toolName,
                    MirrorToVerifyFileNames: mirrorToVerify),
                ct).ConfigureAwait(false);

            return
            [
                new RolloutMediaAttachment(artifact.AbsolutePath, artifact.DownloadUrl, mediaKind)
            ];
        }

        var dir = Path.Combine(_options.RunsRoot, runId.ToString("D"), "obscura");
        Directory.CreateDirectory(dir);
        var fallbackName = logicalName + (kind == ObscuraEvidenceKind.Screenshot ? ".png"
            : kind == ObscuraEvidenceKind.Video ? ".webm"
            : kind == ObscuraEvidenceKind.DomSnapshot ? ".md" : ".json");
        var path = Path.Combine(dir, fallbackName);
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        return
        [
            new RolloutMediaAttachment(path, $"/api/ide/app-generation/{runId:D}/obscura/artifacts/{fallbackName}", mediaKind)
        ];
    }

    private static string? ExtractPathValue(string output, string key)
    {
        var marker = key + "=";
        var idx = output.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return null;
        var rest = output[(idx + marker.Length)..];
        var end = rest.IndexOfAny(['\n', '\r']);
        return (end > 0 ? rest[..end] : rest).Trim();
    }

    private static string? ExtractBase64(string output)
    {
        const string marker = "base64=";
        var idx = output.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return null;
        return output[(idx + marker.Length)..].Trim();
    }

    private static string TruncateOutput(string output) =>
        output.Length > 16_000 ? output[..16_000] + "\n…(truncated)" : output;
}
