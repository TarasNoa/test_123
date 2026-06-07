using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

/// <summary>
/// Persistent stdio JSON-RPC session for a single MCP server process (per run).
/// </summary>
public sealed class McpStdioSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _stderr = new();
    private int _nextId = 1;
    private bool _initialized;

    private McpStdioSession(Process process, StringBuilder stderr)
    {
        _process = process;
        _stderr = stderr;
    }

    public static async Task<McpStdioSession> StartAsync(
        McpServerLaunchProfile profile,
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
        foreach (var arg in profile.Arguments)
            psi.ArgumentList.Add(arg);
        if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory))
            psi.WorkingDirectory = profile.WorkingDirectory;

        var stderr = new StringBuilder();
        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start MCP server process");

        var session = new McpStdioSession(proc, stderr);
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                session.AppendStderr(e.Data);
        };
        proc.BeginErrorReadLine();

        await session.InitializeAsync(ct).ConfigureAwait(false);
        return session;
    }

    internal void AppendStderr(string line) => _stderr.AppendLine(line);

    public async Task<JsonElement> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _nextId);
        await SendAsync(BuildToolsCall(id, toolName, arguments), ct).ConfigureAwait(false);
        var root = await ReadRootAsync(id, ct).ConfigureAwait(false);
        return root.GetProperty("result");
    }

    public async Task<IReadOnlyList<McpCatalogTool>> ListToolsAsync(CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _nextId);
        await SendAsync(new { jsonrpc = "2.0", id, method = "tools/list", @params = new { } }, ct)
            .ConfigureAwait(false);
        var root = await ReadRootAsync(id, ct).ConfigureAwait(false);
        if (!root.TryGetProperty("result", out var result)
            || !result.TryGetProperty("tools", out var tools)
            || tools.ValueKind != JsonValueKind.Array)
            return Array.Empty<McpCatalogTool>();

        var list = new List<McpCatalogTool>();
        foreach (var tool in tools.EnumerateArray())
        {
            var name = tool.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var desc = tool.TryGetProperty("description", out var d) ? d.GetString() : null;
            list.Add(new McpCatalogTool(name, "discovered", McpHostTransportKind.Stdio, desc, Array.Empty<string>()));
        }

        return list;
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        if (_initialized)
            return;

        var id = Interlocked.Increment(ref _nextId);
        await SendAsync(new
        {
            jsonrpc = "2.0",
            id,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "libr4-mcp-host", version = "1.0.0" },
            },
        }, ct).ConfigureAwait(false);
        _ = await ReadRootAsync(id, ct).ConfigureAwait(false);
        _initialized = true;
    }

    private async Task SendAsync(object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        await _process.StandardInput.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    private async Task<JsonElement> ReadRootAsync(int expectId, CancellationToken ct)
    {
        while (true)
        {
            var line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
                throw new IOException($"MCP server closed stdout. stderr: {_stderr}");

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                continue;
            if (idEl.GetInt32() != expectId)
                continue;

            if (root.TryGetProperty("error", out var err))
            {
                var msg = err.TryGetProperty("message", out var m) ? m.GetString() : err.ToString();
                throw new InvalidOperationException($"MCP error: {msg}. stderr: {_stderr}");
            }

            return root.Clone();
        }
    }

    private static object BuildToolsCall(int id, string name, IReadOnlyDictionary<string, object?> arguments) => new
    {
        jsonrpc = "2.0",
        id,
        method = "tools/call",
        @params = new { name, arguments },
    };

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch
        {
            // best-effort
        }
        finally
        {
            _process.Dispose();
        }
    }
}
