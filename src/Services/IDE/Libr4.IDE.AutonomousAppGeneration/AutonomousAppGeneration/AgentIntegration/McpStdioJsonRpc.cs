using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Minimal line-delimited JSON-RPC client for MCP servers that read one JSON object per line
/// (matches libr4-agent-bridge Python server).
/// </summary>
public static class McpStdioJsonRpc
{
    public static async Task<JsonElement> CallToolAsync(
        McpServerLaunchProfile profile,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        TimeSpan timeout,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(profile.FileName))
            throw new InvalidOperationException("MCP server FileName is not configured");

        var psi = new ProcessStartInfo
        {
            FileName = profile.FileName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in profile.Arguments)
            psi.ArgumentList.Add(a);

        if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory))
            psi.WorkingDirectory = profile.WorkingDirectory;

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start MCP server process");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var stderr = new StringBuilder();
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) stderr.AppendLine(e.Data);
        };
        proc.BeginErrorReadLine();

        try
        {
            await SendLineAsync(proc, BuildInitialize(1), cts.Token).ConfigureAwait(false);
            await ConsumeResponseAsync(proc, expectId: 1, cts.Token, stderr).ConfigureAwait(false);

            await SendLineAsync(proc, BuildToolsCall(2, toolName, arguments), cts.Token).ConfigureAwait(false);
            return await ReadResultAsync(proc, expectId: 2, cts.Token, stderr).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                    await proc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private static async Task SendLineAsync(Process proc, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        await proc.StandardInput.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
        await proc.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    private static async Task ConsumeResponseAsync(
        Process proc,
        int expectId,
        CancellationToken ct,
        StringBuilder stderr)
    {
        _ = await ReadRootAsync(proc, expectId, ct, stderr).ConfigureAwait(false);
    }

    private static async Task<JsonElement> ReadResultAsync(
        Process proc,
        int expectId,
        CancellationToken ct,
        StringBuilder stderr)
    {
        var root = await ReadRootAsync(proc, expectId, ct, stderr).ConfigureAwait(false);
        return root.GetProperty("result");
    }

    private static async Task<JsonElement> ReadRootAsync(
        Process proc,
        int expectId,
        CancellationToken ct,
        StringBuilder stderr)
    {
        var line = await proc.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(line))
            throw new IOException($"MCP server closed stdout before responding. stderr: {stderr}");

        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        if (!root.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number ||
            idEl.GetInt32() != expectId)
        {
            throw new IOException($"Unexpected JSON-RPC id in response: {line}");
        }

        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var m) ? m.GetString() : err.ToString();
            throw new InvalidOperationException($"MCP error: {msg}. stderr: {stderr}");
        }

        return root.Clone();
    }

    private static object BuildInitialize(int id) => new
    {
        jsonrpc = "2.0",
        id,
        method = "initialize",
        @params = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new { name = "libr4", version = "1.0.0" },
        },
    };

    private static object BuildToolsCall(int id, string name, IReadOnlyDictionary<string, object?> arguments) => new
    {
        jsonrpc = "2.0",
        id,
        method = "tools/call",
        @params = new { name, arguments },
    };
}
