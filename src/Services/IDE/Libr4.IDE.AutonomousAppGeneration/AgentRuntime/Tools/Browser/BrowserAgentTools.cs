using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;

public sealed class BrowserLaunchTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserLaunchTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Launch;
    public string Description => "Launch Obscura browser. Input: {} — returns session_id (reuses run lease when runId present).";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var purpose = ObscuraBrowserToolFacade.GetString(input, "purpose") ?? "agent-runtime";
        var (ok, sessionId, error) = await _facade.LaunchAsync(context, purpose, ct).ConfigureAwait(false);
        return ok
            ? ObscuraBrowserToolFacade.Ok(Name, $"session_id={sessionId}")
            : ObscuraBrowserToolFacade.Fail(Name, error ?? "launch failed");
    }
}

public sealed class BrowserNavigateTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserNavigateTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Navigate;
    public string Description => "Navigate browser. Input: { \"session_id\": \"...\", \"url\": \"https://...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var url = ObscuraBrowserToolFacade.GetString(input, "url");
        if (string.IsNullOrWhiteSpace(url))
            return ObscuraBrowserToolFacade.Fail(Name, "url is required");

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: true, "navigate", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session resolution failed");

        var (ok, output, error) = await _facade.NavigateAsync(sessionId, url, ct, context.Session.RunId).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "navigate failed");
    }
}

public sealed class BrowserSnapshotTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserSnapshotTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Snapshot;
    public string Description => "Accessibility-oriented DOM snapshot with refs. Input: { \"session_id\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "snapshot", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.SnapshotAsync(sessionId, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "snapshot failed");
    }
}

public sealed class BrowserClickTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserClickTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Click;
    public string Description => "Click element. Input: { \"session_id\": \"...\", \"selector\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var selector = ObscuraBrowserToolFacade.GetString(input, "selector");
        if (string.IsNullOrWhiteSpace(selector))
            return ObscuraBrowserToolFacade.Fail(Name, "selector is required");

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "click", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.ClickAsync(sessionId, selector, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "click failed");
    }
}

public sealed class BrowserTypeTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserTypeTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Type;
    public string Description => "Type into element. Input: { \"session_id\": \"...\", \"selector\": \"...\", \"text\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var selector = ObscuraBrowserToolFacade.GetString(input, "selector");
        var text = ObscuraBrowserToolFacade.GetString(input, "text") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(selector))
            return ObscuraBrowserToolFacade.Fail(Name, "selector is required");

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "type", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.TypeAsync(sessionId, selector, text, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "type failed");
    }
}

public sealed class BrowserScrollTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserScrollTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Scroll;
    public string Description => "Scroll page. Input: { \"session_id\": \"...\", \"delta_x\": 0, \"delta_y\": 400 }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var dx = ObscuraBrowserToolFacade.GetInt(input, "delta_x", 0);
        var dy = ObscuraBrowserToolFacade.GetInt(input, "delta_y", 400);

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "scroll", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.ScrollAsync(sessionId, dx, dy, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "scroll failed");
    }
}

public sealed class BrowserWaitTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserWaitTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Wait;
    public string Description => "Wait for selector. Input: { \"session_id\": \"...\", \"selector\": \"...\", \"timeout_ms\": 5000 }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var selector = ObscuraBrowserToolFacade.GetString(input, "selector");
        if (string.IsNullOrWhiteSpace(selector))
            return ObscuraBrowserToolFacade.Fail(Name, "selector is required");

        var timeout = ObscuraBrowserToolFacade.GetInt(input, "timeout_ms", 5000);
        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "wait", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.WaitAsync(sessionId, selector, timeout, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "wait failed");
    }
}

public sealed class BrowserScreenshotTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserScreenshotTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Screenshot;
    public string Description => "Capture PNG screenshot (base64). Input: { \"session_id\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "screenshot", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.ScreenshotAsync(sessionId, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "screenshot failed");
    }
}

public sealed class BrowserExecuteJsTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserExecuteJsTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.ExecuteJs;
    public string Description => "Execute JavaScript. Input: { \"session_id\": \"...\", \"script\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var script = ObscuraBrowserToolFacade.GetString(input, "script");
        if (string.IsNullOrWhiteSpace(script))
            return ObscuraBrowserToolFacade.Fail(Name, "script is required");

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "execute_js", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.ExecuteJsAsync(sessionId, script, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "execute_js failed");
    }
}

public sealed class BrowserConsoleTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserConsoleTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Console;
    public string Description => "Read captured console messages. Input: { \"session_id\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "console", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.ConsoleAsync(sessionId, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "console failed");
    }
}

public sealed class BrowserGetContentTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    private readonly IDomToMarkdownConverter _markdown;

    public BrowserGetContentTool(ObscuraBrowserToolFacade facade, IDomToMarkdownConverter markdown)
    {
        _facade = facade;
        _markdown = markdown;
    }

    public string Name => BrowserToolNames.GetContent;
    public string Description => "Get page content. Input: { \"session_id\": \"...\", \"format\": \"html|markdown\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var format = ObscuraBrowserToolFacade.GetString(input, "format") ?? "html";
        var asMarkdown = string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase);

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "get_content", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.GetContentAsync(sessionId, asMarkdown, _markdown, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "get_content failed");
    }
}

public sealed class BrowserExtractTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserExtractTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Extract;
    public string Description => "Extract text by CSS selectors. Input: { \"session_id\": \"...\", \"selectors\": [\"h1\", \".price\"] }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!input.TryGetProperty("selectors", out var selectorsEl) || selectorsEl.ValueKind != JsonValueKind.Array)
            return ObscuraBrowserToolFacade.Fail(Name, "selectors array is required");

        var selectors = selectorsEl.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (selectors.Count == 0)
            return ObscuraBrowserToolFacade.Fail(Name, "selectors must be non-empty");

        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: false, "extract", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.ExtractAsync(sessionId, selectors, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "extract failed");
    }
}

public sealed class BrowserRecordStartTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserRecordStartTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.RecordStart;
    public string Description => "Start WebM screen recording. Input: { \"session_id\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var (rsOk, sessionId, rsErr) = await _facade.ResolveSessionAsync(input, context, autoLaunch: true, "record", ct).ConfigureAwait(false);
        if (!rsOk)
            return ObscuraBrowserToolFacade.Fail(Name, rsErr ?? "session_id required");

        var (ok, output, error) = await _facade.RecordStartAsync(sessionId, context.Session.RunId, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "record start failed");
    }
}

public sealed class BrowserRecordStopTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserRecordStopTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.RecordStop;
    public string Description => "Stop WebM recording and persist smoke.webm evidence. Input: { \"session_id\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!ObscuraBrowserToolFacade.TryGetSessionId(input, out var sessionId))
            return ObscuraBrowserToolFacade.Fail(Name, "session_id is required");

        var (ok, output, error) = await _facade.RecordStopAsync(
            sessionId,
            context.Session.RunId,
            context.Session.CurrentStepNumber,
            ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "record stop failed");
    }
}

public sealed class BrowserCloseTool : IAgentTool
{
    private readonly ObscuraBrowserToolFacade _facade;
    public BrowserCloseTool(ObscuraBrowserToolFacade facade) => _facade = facade;
    public string Name => BrowserToolNames.Close;
    public string Description => "Close browser session. Input: { \"session_id\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!ObscuraBrowserToolFacade.TryGetSessionId(input, out var sessionId))
            return ObscuraBrowserToolFacade.Fail(Name, "session_id is required");

        var (ok, output, error) = await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);
        return ok ? ObscuraBrowserToolFacade.Ok(Name, output) : ObscuraBrowserToolFacade.Fail(Name, error ?? "close failed");
    }
}
