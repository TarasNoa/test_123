using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;

/// <summary>
/// Shared Obscura browser operations for native browser_* agent runtime tools.
/// Reuses run-scoped session leases via <see cref="IObscuraBrowserService"/>.
/// </summary>
public sealed class ObscuraBrowserToolFacade
{
    private readonly IObscuraBrowserService _browser;
    private readonly IObscuraNetworkRouter? _networkRouter;
    private readonly IObscuraBrowserRecordingService? _recording;
    private readonly HashSet<string> _consoleHooked = new(StringComparer.Ordinal);

    public ObscuraBrowserToolFacade(
        IObscuraBrowserService browser,
        IObscuraNetworkRouter? networkRouter = null,
        IObscuraBrowserRecordingService? recording = null)
    {
        _browser = browser;
        _networkRouter = networkRouter;
        _recording = recording;
    }

    public async Task<(bool Ok, string SessionId, string? Error)> LaunchAsync(
        ToolContext context,
        string purpose,
        CancellationToken ct)
    {
        try
        {
            var sessionId = await _browser.LaunchBrowserAsync(new ObscuraLaunchOptions
            {
                StealthMode = true,
                BlockTrackers = true,
                RunId = context.Session.RunId?.ToString("D"),
                UserId = context.Session.TenantUserId,
                Purpose = purpose
            }, ct).ConfigureAwait(false);

            return (true, sessionId, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string SessionId, string? Error)> ResolveSessionAsync(
        JsonElement input,
        ToolContext context,
        bool autoLaunch,
        string purpose,
        CancellationToken ct)
    {
        if (TryGetSessionId(input, out var existing))
            return (true, existing, null);

        if (!autoLaunch)
            return (false, string.Empty, "session_id is required (call browser_launch first)");

        return await LaunchAsync(context, purpose, ct).ConfigureAwait(false);
    }

    public async Task<(bool Ok, string Output, string? Error)> NavigateAsync(
        string sessionId,
        string url,
        CancellationToken ct,
        Guid? runId = null)
    {
        try
        {
            var resolved = _networkRouter?.ResolveForBrowser(runId, url) ?? url;
            await _browser.NavigateAsync(sessionId, resolved, ct).ConfigureAwait(false);
            await EnsureConsoleHookAsync(sessionId, ct).ConfigureAwait(false);
            return (true, $"navigated session={sessionId} url={resolved}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> SnapshotAsync(
        string sessionId,
        CancellationToken ct)
    {
        try
        {
            await EnsureConsoleHookAsync(sessionId, ct).ConfigureAwait(false);
            var json = await _browser.ExecuteJavaScriptAsync(sessionId, SnapshotScript, ct).ConfigureAwait(false);
            return (true, json, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> ScreenshotAsync(
        string sessionId,
        CancellationToken ct)
    {
        try
        {
            var bytes = await _browser.TakeScreenshotAsync(sessionId, ct).ConfigureAwait(false);
            var b64 = Convert.ToBase64String(bytes);
            return (true, $"session={sessionId}\ncontent_type=image/png\nbase64={b64}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> GetContentAsync(
        string sessionId,
        bool asMarkdown,
        IDomToMarkdownConverter? markdown,
        CancellationToken ct)
    {
        try
        {
            var html = await _browser.GetPageContentAsync(sessionId, ct).ConfigureAwait(false);
            if (asMarkdown && markdown is not null)
            {
                html = markdown.Convert(html, new ConversionOptions
                {
                    RemoveNoise = true,
                    IncludeLinks = true,
                    MaxLength = 80_000
                });
            }

            return (true, html, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> ExecuteJsAsync(
        string sessionId,
        string script,
        CancellationToken ct)
    {
        try
        {
            var result = await _browser.ExecuteJavaScriptAsync(sessionId, script, ct).ConfigureAwait(false);
            return (true, result ?? string.Empty, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> ConsoleAsync(
        string sessionId,
        CancellationToken ct)
    {
        try
        {
            await EnsureConsoleHookAsync(sessionId, ct).ConfigureAwait(false);
            var json = await _browser.ExecuteJavaScriptAsync(
                sessionId,
                "JSON.stringify(window.__libr4Console || [])",
                ct).ConfigureAwait(false);
            return (true, json, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> ExtractAsync(
        string sessionId,
        IReadOnlyList<string> selectors,
        CancellationToken ct)
    {
        try
        {
            var sb = new StringBuilder();
            for (var i = 0; i < selectors.Count; i++)
            {
                var selector = selectors[i].Replace("\\", "\\\\").Replace("'", "\\'");
                var script = $"(() => {{ const el = document.querySelector('{selector}'); return el ? (el.textContent || '').trim() : ''; }})()";
                var value = await _browser.ExecuteJavaScriptAsync(sessionId, script, ct).ConfigureAwait(false);
                sb.AppendLine($"{selectors[i]}={value}");
            }

            return (true, sb.ToString().TrimEnd(), null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> ClickAsync(
        string sessionId,
        string selector,
        CancellationToken ct)
    {
        try
        {
            await _browser.ClickAsync(sessionId, selector, ct).ConfigureAwait(false);
            return (true, $"clicked selector={selector}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> TypeAsync(
        string sessionId,
        string selector,
        string text,
        CancellationToken ct)
    {
        try
        {
            await _browser.TypeAsync(sessionId, selector, text, ct).ConfigureAwait(false);
            return (true, $"typed selector={selector} len={text.Length}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> ScrollAsync(
        string sessionId,
        int deltaX,
        int deltaY,
        CancellationToken ct)
    {
        try
        {
            var script = $"window.scrollBy({deltaX}, {deltaY}); JSON.stringify({{ x: window.scrollX, y: window.scrollY }})";
            var pos = await _browser.ExecuteJavaScriptAsync(sessionId, script, ct).ConfigureAwait(false);
            return (true, $"scrolled dx={deltaX} dy={deltaY} pos={pos}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> WaitAsync(
        string sessionId,
        string selector,
        int timeoutMs,
        CancellationToken ct)
    {
        try
        {
            await _browser.WaitForElementAsync(sessionId, selector, timeoutMs, ct).ConfigureAwait(false);
            return (true, $"found selector={selector}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> CloseAsync(
        string sessionId,
        CancellationToken ct)
    {
        try
        {
            await _browser.CloseBrowserAsync(sessionId, ct).ConfigureAwait(false);
            _consoleHooked.Remove(sessionId);
            return (true, $"closed session={sessionId}", null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    public async Task<(bool Ok, string Output, string? Error)> RecordStartAsync(
        string sessionId,
        Guid? runId,
        CancellationToken ct)
    {
        if (_recording is null)
            return (false, string.Empty, "browser recording service is not configured");

        var (ok, error) = await _recording.StartAsync(sessionId, runId, ct).ConfigureAwait(false);
        return ok
            ? (true, $"recording_started session={sessionId}", null)
            : (false, string.Empty, error ?? "record start failed");
    }

    public async Task<(bool Ok, string Output, string? Error)> RecordStopAsync(
        string sessionId,
        Guid? runId,
        int stepNumber,
        CancellationToken ct)
    {
        if (_recording is null)
            return (false, string.Empty, "browser recording service is not configured");

        var result = await _recording.StopAsync(sessionId, runId, stepNumber, ct).ConfigureAwait(false);
        return result.Success
            ? (true, result.Output ?? string.Empty, null)
            : (false, string.Empty, result.Error ?? "record stop failed");
    }

    private async Task EnsureConsoleHookAsync(string sessionId, CancellationToken ct)
    {
        if (!_consoleHooked.Add(sessionId))
            return;

        await _browser.ExecuteJavaScriptAsync(sessionId, ConsoleHookScript, ct).ConfigureAwait(false);
    }

    public static bool TryGetSessionId(JsonElement input, out string sessionId)
    {
        sessionId = string.Empty;
        if (!input.TryGetProperty("session_id", out var el) || el.ValueKind != JsonValueKind.String)
            return false;
        sessionId = el.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(sessionId);
    }

    public static string? GetString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    public static int GetInt(JsonElement input, string name, int fallback) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : fallback;

    public static ToolExecutionResult Ok(string tool, string output) =>
        new(tool, true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    public static ToolExecutionResult Fail(string tool, string message) =>
        new(tool, false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    private const string ConsoleHookScript = """
        (() => {
          if (window.__libr4Console) return;
          window.__libr4Console = [];
          ['log','warn','error','info'].forEach(level => {
            const orig = console[level];
            console[level] = (...args) => {
              window.__libr4Console.push({ level, message: args.map(a => String(a)).join(' '), at: Date.now() });
              orig.apply(console, args);
            };
          });
        })()
        """;

    private const string SnapshotScript = """
        (() => {
          let i = 0;
          const nodes = [];
          const seen = new Set();
          document.querySelectorAll('a,button,input,textarea,select,[role],[onclick],h1,h2,h3,label').forEach(el => {
            if (seen.has(el)) return;
            seen.add(el);
            const ref = 'e' + (++i);
            el.setAttribute('data-libr4-ref', ref);
            nodes.push({
              ref,
              tag: el.tagName.toLowerCase(),
              role: el.getAttribute('role') || '',
              text: (el.innerText || el.getAttribute('aria-label') || '').trim().slice(0, 120),
              selector: '[data-libr4-ref="' + ref + '"]'
            });
          });
          return JSON.stringify({ url: location.href, title: document.title, nodes });
        })()
        """;
}
