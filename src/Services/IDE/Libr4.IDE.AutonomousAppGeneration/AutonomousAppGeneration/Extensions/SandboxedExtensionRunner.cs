using System.Diagnostics;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public interface ISandboxedExtensionRunner
{
    Task<SandboxedExtensionResult> RunHookAsync(
        ExtensionHookBinding binding,
        HookContext context,
        CancellationToken ct = default);

    Task<SandboxedExtensionResult> RunToolAsync(
        ExtensionToolBinding binding,
        JsonElement input,
        ToolContext context,
        CancellationToken ct = default);
}

public sealed record SandboxedExtensionResult(
    bool Success,
    string Output,
    int ExitCode,
    bool TimedOut);

public sealed class SandboxedExtensionRunner : ISandboxedExtensionRunner
{
    private readonly ExtensionHostOptions _options;
    private readonly ILogger<SandboxedExtensionRunner> _logger;

    public SandboxedExtensionRunner(
        IOptions<ExtensionHostOptions> options,
        ILogger<SandboxedExtensionRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<SandboxedExtensionResult> RunHookAsync(
        ExtensionHookBinding binding,
        HookContext context,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            kind = binding.Definition.Kind,
            extension = binding.Extension.Id,
            runId = context.RunId,
            sessionId = context.SessionId,
            workspaceRoot = context.WorkspaceRoot,
            stage = context.Stage,
            tool = context.Tool?.Name,
            toolSuccess = context.ToolResult?.Success,
            toolOutput = context.ToolResult?.Output
        });

        var timeoutMs = binding.Definition.TimeoutMs ?? _options.DefaultHookTimeoutMs;
        return RunScriptAsync(binding.Extension, binding.ScriptPath, payload, timeoutMs, ct);
    }

    public Task<SandboxedExtensionResult> RunToolAsync(
        ExtensionToolBinding binding,
        JsonElement input,
        ToolContext context,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            extension = binding.Extension.Id,
            tool = binding.Definition.Name,
            runId = context.Session.RunId,
            sessionId = context.Session.SessionId,
            workspaceRoot = context.Workspace.HostPath,
            input
        });

        var timeoutMs = binding.Definition.TimeoutMs ?? _options.DefaultToolTimeoutMs;
        return RunScriptAsync(binding.Extension, binding.ScriptPath, payload, timeoutMs, ct);
    }

    private async Task<SandboxedExtensionResult> RunScriptAsync(
        LoadedExtension extension,
        string scriptPath,
        string payload,
        int timeoutMs,
        CancellationToken ct)
    {
        if (!File.Exists(scriptPath))
            return new SandboxedExtensionResult(false, $"script_missing: {scriptPath}", -1, false);

        var interpreter = ResolveInterpreter(scriptPath);
        var psi = new ProcessStartInfo
        {
            FileName = interpreter.FileName,
            Arguments = interpreter.Arguments,
            WorkingDirectory = extension.RootPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var key in new[] { "PATH", "SystemRoot", "TEMP", "TMP", "HOME", "USERPROFILE" })
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                psi.Environment[key] = value;
        }

        psi.Environment["LIBR4_EXTENSION_ROOT"] = extension.RootPath;
        psi.Environment["LIBR4_EXTENSION_ID"] = extension.Id;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"extension_start_failed: {scriptPath}");

        await process.StandardInput.WriteAsync(payload).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(timeoutMs, 500)));
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best effort
            }
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        var output = string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}\n{stderr}".Trim();
        var success = !timedOut && process.ExitCode == 0;

        if (!success)
        {
            _logger.LogWarning(
                "Extension script failed extension={ExtensionId} script={Script} exit={ExitCode} timedOut={TimedOut}",
                extension.Id,
                scriptPath,
                process.ExitCode,
                timedOut);
        }

        return new SandboxedExtensionResult(success, output, process.ExitCode, timedOut);
    }

    private static (string FileName, string Arguments) ResolveInterpreter(string scriptPath)
    {
        var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
        return ext switch
        {
            ".ps1" => ("powershell", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\""),
            ".py" => ("python", $"\"{scriptPath}\""),
            ".sh" => OperatingSystem.IsWindows()
                ? ("bash", $"\"{scriptPath}\"")
                : ("/bin/sh", $"\"{scriptPath}\""),
            ".cmd" or ".bat" => ("cmd.exe", $"/c \"{scriptPath}\""),
            _ => throw new InvalidOperationException($"unsupported_extension_script: {scriptPath}")
        };
    }
}
