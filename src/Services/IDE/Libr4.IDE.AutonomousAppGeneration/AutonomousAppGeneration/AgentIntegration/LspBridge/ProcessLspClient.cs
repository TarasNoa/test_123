using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

public sealed class ProcessLspClient
{
    private readonly LspBridgeOptions _options;
    private readonly ILogger<ProcessLspClient> _logger;

    public ProcessLspClient(IOptions<LspBridgeOptions> options, ILogger<ProcessLspClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LspDiagnostic>> GetDiagnosticsAsync(
        string profileKey,
        GeneratedFile file,
        CancellationToken ct)
    {
        if (!_options.Servers.TryGetValue(profileKey, out var profile))
            return Array.Empty<LspDiagnostic>();

        await using var session = await LspStdioSession.StartAsync(profile, _logger, ct).ConfigureAwait(false);
        var uri = new Uri(Path.GetFullPath(file.RelativePath)).AbsoluteUri;
        var languageId = InferLanguageId(file.RelativePath);

        await session.SendNotificationAsync("textDocument/didOpen", new
        {
            textDocument = new
            {
                uri,
                languageId,
                version = 1,
                text = file.Content ?? string.Empty
            }
        }, ct).ConfigureAwait(false);

        var response = await session.SendRequestAsync("textDocument/diagnostic", new
        {
            textDocument = new { uri }
        }, ct).ConfigureAwait(false);

        if (!response.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return Array.Empty<LspDiagnostic>();

        var list = new List<LspDiagnostic>();
        foreach (var item in items.EnumerateArray().Take(_options.MaxDiagnosticsPerFile))
        {
            var message = item.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "";
            var severity = item.TryGetProperty("severity", out var sev) ? MapSeverity(sev.GetInt32()) : "warning";
            var line = 0;
            var col = 0;
            if (item.TryGetProperty("range", out var range)
                && range.TryGetProperty("start", out var start))
            {
                if (start.TryGetProperty("line", out var ln))
                    line = ln.GetInt32() + 1;
                if (start.TryGetProperty("character", out var ch))
                    col = ch.GetInt32() + 1;
            }

            list.Add(new LspDiagnostic(
                file.RelativePath,
                line,
                col,
                severity,
                message,
                item.TryGetProperty("code", out var code) ? code.ToString() : null,
                profileKey));
        }

        return list;
    }

    private static string InferLanguageId(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".cs" => "csharp",
            ".py" => "python",
            _ => "plaintext"
        };

    private static string MapSeverity(int severity) => severity switch
    {
        1 => "error",
        2 => "warning",
        3 => "info",
        _ => "hint"
    };
}

internal sealed class LspStdioSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _stderr = new();
    private readonly ILogger _logger;
    private int _nextId = 1;
    private readonly SemaphoreSlim _ioGate = new(1, 1);

    private LspStdioSession(Process process, ILogger logger)
    {
        _process = process;
        _logger = logger;
    }

    public static async Task<LspStdioSession> StartAsync(
        LspServerLaunchProfile profile,
        ILogger logger,
        CancellationToken ct)
    {
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

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException($"Failed to start LSP server '{profile.FileName}'");

        var session = new LspStdioSession(proc, logger);
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                session._stderr.AppendLine(e.Data);
        };
        proc.BeginErrorReadLine();

        await session.SendRequestAsync("initialize", new
        {
            processId = Environment.ProcessId,
            capabilities = new
            {
                textDocument = new
                {
                    diagnostic = new { dynamicRegistration = false }
                }
            },
            rootUri = string.IsNullOrWhiteSpace(profile.WorkingDirectory)
                ? null
                : new Uri(Path.GetFullPath(profile.WorkingDirectory)).AbsoluteUri
        }, ct).ConfigureAwait(false);

        await session.SendNotificationAsync("initialized", new { }, ct).ConfigureAwait(false);
        return session;
    }

    public Task<JsonElement> SendRequestAsync(string method, object parameters, CancellationToken ct) =>
        SendCoreAsync(method, parameters, expectResponse: true, ct);

    public async Task SendNotificationAsync(string method, object parameters, CancellationToken ct)
    {
        _ = await SendCoreAsync(method, parameters, expectResponse: false, ct).ConfigureAwait(false);
    }

    private async Task<JsonElement> SendCoreAsync(
        string method,
        object parameters,
        bool expectResponse,
        CancellationToken ct)
    {
        await _ioGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (expectResponse)
            {
                var id = Interlocked.Increment(ref _nextId);
                await WriteAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, ct)
                    .ConfigureAwait(false);
                var root = await ReadRootAsync(id, ct).ConfigureAwait(false);
                return root.TryGetProperty("result", out var result) ? result.Clone() : default;
            }

            await WriteAsync(new { jsonrpc = "2.0", method, @params = parameters }, ct).ConfigureAwait(false);
            return default;
        }
        finally
        {
            _ioGate.Release();
        }
    }

    private async Task WriteAsync(object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        var header = $"Content-Length: {Encoding.UTF8.GetByteCount(json)}\r\n\r\n";
        await _process.StandardInput.WriteAsync(header.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.WriteAsync(json.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    private async Task<JsonElement> ReadRootAsync(int expectId, CancellationToken ct)
    {
        while (true)
        {
            var content = await ReadMessageAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
                throw new IOException($"LSP server closed stream. stderr: {_stderr}");

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                continue;
            if (idEl.GetInt32() != expectId)
                continue;

            if (root.TryGetProperty("error", out var err))
            {
                var msg = err.TryGetProperty("message", out var m) ? m.GetString() : err.ToString();
                throw new InvalidOperationException($"LSP error: {msg}");
            }

            return root.Clone();
        }
    }

    private async Task<string> ReadMessageAsync(CancellationToken ct)
    {
        var headerLines = new List<string>();
        while (true)
        {
            var line = await _process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
                return string.Empty;
            if (line.Length == 0)
                break;
            headerLines.Add(line);
        }

        var lengthLine = headerLines.FirstOrDefault(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        if (lengthLine is null)
            throw new InvalidOperationException("LSP response missing Content-Length header");

        var length = int.Parse(lengthLine["Content-Length:".Length..].Trim());
        var buffer = new char[length];
        var read = 0;
        while (read < length)
        {
            var chunk = await _process.StandardOutput.ReadAsync(buffer.AsMemory(read, length - read), ct)
                .ConfigureAwait(false);
            if (chunk == 0)
                break;
            read += chunk;
        }

        return new string(buffer, 0, read);
    }

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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LSP session dispose failed");
        }
        finally
        {
            _process.Dispose();
            _ioGate.Dispose();
        }
    }
}
