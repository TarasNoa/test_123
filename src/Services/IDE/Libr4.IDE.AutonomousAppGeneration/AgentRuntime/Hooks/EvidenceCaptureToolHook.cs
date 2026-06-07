using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

/// <summary>
/// PostToolUse capture of build/verify artifacts into the verify evidence store.
/// Browser screenshots are handled by <see cref="BrowserToolEventHook"/>.
/// </summary>
public sealed class EvidenceCaptureToolHook : IAgentToolHook
{
    private readonly IVerifyEvidenceStore? _verifyEvidence;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<EvidenceCaptureToolHook> _logger;

    public EvidenceCaptureToolHook(
        IOptions<AgentRuntimeOptions> options,
        ILogger<EvidenceCaptureToolHook> logger,
        IVerifyEvidenceStore? verifyEvidence = null)
    {
        _options = options.Value;
        _logger = logger;
        _verifyEvidence = verifyEvidence;
    }

    public int Order => 110;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct) =>
        ValueTask.CompletedTask;

    public async ValueTask OnAfterToolAsync(
        IAgentTool tool,
        ToolContext context,
        ToolExecutionResult result,
        CancellationToken ct)
    {
        if (!_options.EnableEvidenceCaptureHook
            || _verifyEvidence is null
            || context.Session.RunId is not Guid runId
            || BrowserToolEventHook.IsBrowserTool(tool.Name))
        {
            return;
        }

        try
        {
            if (string.Equals(tool.Name, "apply_patch", StringComparison.OrdinalIgnoreCase)
                && result.Success
                && result.FilePatches.Count > 0)
            {
                var manifest = JsonSerializer.Serialize(new
                {
                    tool = tool.Name,
                    step = context.Session.CurrentStepNumber,
                    files = result.FilePatches.Select(f => f.RelativePath).ToList(),
                    capturedAtUtc = DateTime.UtcNow
                });
                await PersistTextAsync(
                    runId,
                    VerifyEvidenceKind.Other,
                    $"patch-step-{context.Session.CurrentStepNumber}.json",
                    manifest,
                    ct).ConfigureAwait(false);
                return;
            }

            if ((string.Equals(tool.Name, "run_build", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(tool.Name, "run_tests", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(result.Output))
            {
                var kind = string.Equals(tool.Name, "run_tests", StringComparison.OrdinalIgnoreCase)
                    ? VerifyEvidenceKind.VerifyReport
                    : VerifyEvidenceKind.AppLog;
                var suffix = result.Success ? "pass" : "fail";
                var fileName = $"{tool.Name}-step-{context.Session.CurrentStepNumber}-{suffix}.log";
                await PersistTextAsync(runId, kind, fileName, Truncate(result.Output, 64_000), ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Evidence capture skipped for tool {Tool} run {RunId}", tool.Name, runId);
        }
    }

    private async Task PersistTextAsync(
        Guid runId,
        VerifyEvidenceKind kind,
        string fileName,
        string content,
        CancellationToken ct)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _verifyEvidence!.PersistAsync(runId, kind, stream, fileName, ct).ConfigureAwait(false);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "\n...[truncated]...";
}
