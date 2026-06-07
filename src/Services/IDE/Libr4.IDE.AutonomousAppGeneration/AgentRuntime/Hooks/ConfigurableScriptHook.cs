using System.Diagnostics;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

public sealed class ConfigurableScriptHook : IAgentToolHook
{
    private readonly ConfigurableScriptHookOptions _options;

    public ConfigurableScriptHook(IOptions<ConfigurableScriptHookOptions> options) =>
        _options = options.Value;

    public int Order => 50;

    public async ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        foreach (var hook in _options.Hooks.Where(h =>
                     string.Equals(h.Kind, "PreToolUse", StringComparison.OrdinalIgnoreCase)))
        {
            await RunHookAsync(hook, tool, context, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct)
    {
        foreach (var hook in _options.Hooks.Where(h =>
                     string.Equals(h.Kind, "PostToolUse", StringComparison.OrdinalIgnoreCase)))
        {
            await RunHookAsync(hook, tool, context, ct, result).ConfigureAwait(false);
        }
    }

    private static async Task RunHookAsync(
        ScriptHookDefinition hook,
        IAgentTool tool,
        ToolContext context,
        CancellationToken ct,
        ToolExecutionResult? result = null)
    {
        if (string.IsNullOrWhiteSpace(hook.FileName) || !File.Exists(hook.FileName))
            return;

        var payload = JsonSerializer.Serialize(new
        {
            kind = hook.Kind,
            tool = tool.Name,
            runId = context.Session.RunId,
            sessionId = context.Session.SessionId,
            workspaceRoot = context.Workspace.HostPath,
            success = result?.Success,
            output = result?.Output
        });

        var psi = new ProcessStartInfo
        {
            FileName = hook.FileName,
            Arguments = hook.Arguments ?? string.Empty,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"script_hook_start_failed: {hook.FileName}");

        await process.StandardInput.WriteAsync(payload).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(hook.TimeoutMs, 500)));
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            if (string.Equals(hook.OnFailure, "block", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"script_hook_timeout: {hook.FileName}");
            return;
        }

        if (process.ExitCode != 0
            && string.Equals(hook.OnFailure, "block", StringComparison.OrdinalIgnoreCase))
        {
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"script_hook_failed: {hook.FileName}: {stderr}");
        }
    }
}
