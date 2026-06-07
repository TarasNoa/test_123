using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Computer;

public interface IComputerFlowRunner
{
    bool CanRun(string? flow);

    Task<ComputerSubagentResult> RunAsync(
        ComputerFlowRequest request,
        ToolContext context,
        CancellationToken ct = default);
}

public sealed class ComputerFlowRunner : IComputerFlowRunner
{
    private readonly ObscuraBrowserToolFacade _facade;
    private readonly ISubagentObscuraIntegration? _obscura;
    private readonly IObscuraNetworkRouter? _networkRouter;
    private readonly IObscuraEvidenceStore? _evidence;
    private readonly IDomToMarkdownConverter? _markdown;
    private readonly ComputerSubagentOptions _options;
    private readonly ILogger<ComputerFlowRunner> _logger;

    public ComputerFlowRunner(
        ObscuraBrowserToolFacade facade,
        IOptions<ComputerSubagentOptions> options,
        ILogger<ComputerFlowRunner> logger,
        ISubagentObscuraIntegration? obscura = null,
        IObscuraNetworkRouter? networkRouter = null,
        IObscuraEvidenceStore? evidence = null,
        IDomToMarkdownConverter? markdown = null)
    {
        _facade = facade;
        _options = options.Value;
        _logger = logger;
        _obscura = obscura;
        _networkRouter = networkRouter;
        _evidence = evidence;
        _markdown = markdown;
    }

    public bool CanRun(string? flow) =>
        !string.IsNullOrWhiteSpace(flow) && ComputerFlowNames.All.Contains(flow);

    public async Task<ComputerSubagentResult> RunAsync(
        ComputerFlowRequest request,
        ToolContext context,
        CancellationToken ct = default)
    {
        if (!request.HasDeterministicFlow)
        {
            return new ComputerSubagentResult(
                false,
                "deterministic flow requires flow name and url",
                null,
                false,
                new Dictionary<string, object>());
        }

        var runId = context.Session.RunId ?? Guid.NewGuid();
        var evidenceDir = Path.Combine(_options.EvidenceRoot, runId.ToString("D"), "computer");
        Directory.CreateDirectory(evidenceDir);

        if (_obscura?.HasBrowserCapabilities("computer") == true
            && _obscura.GetAvailableTasks("computer").Contains(request.Flow!, StringComparer.OrdinalIgnoreCase))
        {
            return await RunViaYamlTaskAsync(request, runId, evidenceDir, ct).ConfigureAwait(false);
        }

        return request.Flow switch
        {
            ComputerFlowNames.LoginFlow => await RunLoginFlowAsync(request, context, runId, evidenceDir, ct)
                .ConfigureAwait(false),
            ComputerFlowNames.FormFill => await RunFormFillAsync(request, context, runId, evidenceDir, ct)
                .ConfigureAwait(false),
            ComputerFlowNames.VisualDesignCheck => await RunVisualDesignCheckAsync(request, context, runId, evidenceDir, ct)
                .ConfigureAwait(false),
            _ => new ComputerSubagentResult(false, $"unsupported flow: {request.Flow}", evidenceDir, true, new Dictionary<string, object>())
        };
    }

    private async Task<ComputerSubagentResult> RunViaYamlTaskAsync(
        ComputerFlowRequest request,
        Guid runId,
        string evidenceDir,
        CancellationToken ct)
    {
        var parameters = new Dictionary<string, string>(request.Parameters, StringComparer.OrdinalIgnoreCase);
        parameters.TryAdd("url", request.Url!);
        ApplyDefaultSelectors(parameters);

        var browserResult = await _obscura!.ExecuteBrowserTaskAsync(
            "computer",
            request.Flow!,
            parameters,
            ct).ConfigureAwait(false);

        await WriteEvidenceAsync(evidenceDir, "browser-task-result.json", browserResult, ct).ConfigureAwait(false);

        return new ComputerSubagentResult(
            browserResult.Success,
            browserResult.Success
                ? $"computer flow {request.Flow} completed via YAML task"
                : browserResult.Error ?? "browser task failed",
            evidenceDir,
            true,
            browserResult.ExtractedData);
    }

    private async Task<ComputerSubagentResult> RunLoginFlowAsync(
        ComputerFlowRequest request,
        ToolContext context,
        Guid runId,
        string evidenceDir,
        CancellationToken ct)
    {
        var steps = new List<ComputerFlowStepResult>();
        var extracted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string? sessionId = null;

        try
        {
            var toolContext = BuildToolContext(runId, context);
            var url = ResolveUrl(runId, request.Url!);
            var launch = await _facade.LaunchAsync(toolContext, "computer-login", ct).ConfigureAwait(false);
            if (!launch.Ok)
                return Fail(request.Flow!, evidenceDir, steps, launch.Error ?? "launch failed");

            sessionId = launch.SessionId;
            steps.Add(new ComputerFlowStepResult("launch", true, sessionId));

            var navigate = await _facade.NavigateAsync(sessionId, url, ct, runId).ConfigureAwait(false);
            if (!navigate.Ok)
                return Fail(request.Flow!, evidenceDir, steps, navigate.Error ?? "navigate failed");
            steps.Add(new ComputerFlowStepResult("navigate", true, url));

            var passwordSelector = Param(request, "password_selector", _options.DefaultPasswordSelector);
            var wait = await _facade.WaitAsync(sessionId, passwordSelector, 8000, ct).ConfigureAwait(false);
            if (!wait.Ok)
                return Fail(request.Flow!, evidenceDir, steps, wait.Error ?? "wait failed");
            steps.Add(new ComputerFlowStepResult("wait_password_field", true, passwordSelector));

            var usernameSelector = Param(request, "username_selector", _options.DefaultUsernameSelector);
            var username = Param(request, "username", "test@example.com");
            var typeUser = await _facade.TypeAsync(sessionId, usernameSelector, username, ct).ConfigureAwait(false);
            if (!typeUser.Ok)
                return Fail(request.Flow!, evidenceDir, steps, typeUser.Error ?? "type username failed");
            steps.Add(new ComputerFlowStepResult("type_username", true, usernameSelector));

            var password = Param(request, "password", "password123");
            var typePass = await _facade.TypeAsync(sessionId, passwordSelector, password, ct).ConfigureAwait(false);
            if (!typePass.Ok)
                return Fail(request.Flow!, evidenceDir, steps, typePass.Error ?? "type password failed");
            steps.Add(new ComputerFlowStepResult("type_password", true, passwordSelector));

            var submitSelector = Param(request, "submit_selector", _options.DefaultSubmitSelector);
            var click = await _facade.ClickAsync(sessionId, submitSelector, ct).ConfigureAwait(false);
            if (!click.Ok)
                return Fail(request.Flow!, evidenceDir, steps, click.Error ?? "click submit failed");
            steps.Add(new ComputerFlowStepResult("click_submit", true, submitSelector));

            await Task.Delay(1000, ct).ConfigureAwait(false);

            var screenshot = await _facade.ScreenshotAsync(sessionId, ct).ConfigureAwait(false);
            if (screenshot.Ok)
            {
                await PersistScreenshotAsync(runId, evidenceDir, screenshot.Output, ct).ConfigureAwait(false);
                steps.Add(new ComputerFlowStepResult("screenshot", true, "login-screenshot.png"));
            }

            var content = await _facade.GetContentAsync(sessionId, asMarkdown: true, _markdown, ct).ConfigureAwait(false);
            if (content.Ok)
            {
                await File.WriteAllTextAsync(Path.Combine(evidenceDir, "dom-after-login.md"), content.Output, ct)
                    .ConfigureAwait(false);
                extracted["content_length"] = content.Output.Length;
            }

            var successSelector = Param(request, "success_selector", _options.DefaultSuccessSelector);
            var check = await _facade.ExtractAsync(sessionId, [successSelector], ct).ConfigureAwait(false);
            var loggedIn = check.Ok && !string.IsNullOrWhiteSpace(check.Output);
            extracted["logged_in"] = loggedIn;
            extracted["success_selector"] = successSelector;

            await WriteEvidenceAsync(evidenceDir, "steps.json", steps, ct).ConfigureAwait(false);
            await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);

            return new ComputerSubagentResult(
                loggedIn,
                loggedIn ? "login-flow PASS" : "login-flow FAIL: success selector not found",
                evidenceDir,
                true,
                extracted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "login-flow failed for run {RunId}", runId);
            if (sessionId is not null)
                await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);
            return Fail(request.Flow!, evidenceDir, steps, ex.Message);
        }
    }

    private async Task<ComputerSubagentResult> RunFormFillAsync(
        ComputerFlowRequest request,
        ToolContext context,
        Guid runId,
        string evidenceDir,
        CancellationToken ct)
    {
        var steps = new List<ComputerFlowStepResult>();
        var extracted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string? sessionId = null;

        try
        {
            var toolContext = BuildToolContext(runId, context);
            var url = ResolveUrl(runId, request.Url!);
            var launch = await _facade.LaunchAsync(toolContext, "computer-form", ct).ConfigureAwait(false);
            if (!launch.Ok)
                return Fail(request.Flow!, evidenceDir, steps, launch.Error ?? "launch failed");

            sessionId = launch.SessionId;
            var navigate = await _facade.NavigateAsync(sessionId, url, ct, runId).ConfigureAwait(false);
            if (!navigate.Ok)
                return Fail(request.Flow!, evidenceDir, steps, navigate.Error ?? "navigate failed");

            var formSelector = Param(request, "form_selector", _options.DefaultFormSelector);
            var wait = await _facade.WaitAsync(sessionId, formSelector, 8000, ct).ConfigureAwait(false);
            if (!wait.Ok)
                return Fail(request.Flow!, evidenceDir, steps, wait.Error ?? "wait form failed");

            var fieldSelector = Param(request, "field_selector", "input[name='email'], input[type='text']");
            var fieldValue = Param(request, "field_value", "demo@libr4.local");
            var type = await _facade.TypeAsync(sessionId, fieldSelector, fieldValue, ct).ConfigureAwait(false);
            if (!type.Ok)
                return Fail(request.Flow!, evidenceDir, steps, type.Error ?? "type field failed");

            var submitSelector = Param(request, "submit_selector", _options.DefaultSubmitSelector);
            var click = await _facade.ClickAsync(sessionId, submitSelector, ct).ConfigureAwait(false);
            if (!click.Ok)
                return Fail(request.Flow!, evidenceDir, steps, click.Error ?? "submit failed");

            var screenshot = await _facade.ScreenshotAsync(sessionId, ct).ConfigureAwait(false);
            if (screenshot.Ok)
                await PersistScreenshotAsync(runId, evidenceDir, screenshot.Output, ct).ConfigureAwait(false);

            extracted["field_selector"] = fieldSelector;
            extracted["field_value"] = fieldValue;
            await WriteEvidenceAsync(evidenceDir, "steps.json", steps, ct).ConfigureAwait(false);
            await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);

            return new ComputerSubagentResult(true, "form-fill PASS", evidenceDir, true, extracted);
        }
        catch (Exception ex)
        {
            if (sessionId is not null)
                await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);
            return Fail(request.Flow!, evidenceDir, steps, ex.Message);
        }
    }

    private async Task<ComputerSubagentResult> RunVisualDesignCheckAsync(
        ComputerFlowRequest request,
        ToolContext context,
        Guid runId,
        string evidenceDir,
        CancellationToken ct)
    {
        var steps = new List<ComputerFlowStepResult>();
        var extracted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string? sessionId = null;

        try
        {
            var toolContext = BuildToolContext(runId, context);
            var url = ResolveUrl(runId, request.Url!);
            var launch = await _facade.LaunchAsync(toolContext, "computer-visual", ct).ConfigureAwait(false);
            if (!launch.Ok)
                return Fail(request.Flow!, evidenceDir, steps, launch.Error ?? "launch failed");

            sessionId = launch.SessionId;
            var navigate = await _facade.NavigateAsync(sessionId, url, ct, runId).ConfigureAwait(false);
            if (!navigate.Ok)
                return Fail(request.Flow!, evidenceDir, steps, navigate.Error ?? "navigate failed");

            var layoutSelector = "#root, main, [data-testid='app'], body";
            var wait = await _facade.WaitAsync(sessionId, layoutSelector, 8000, ct).ConfigureAwait(false);
            if (!wait.Ok)
                return Fail(request.Flow!, evidenceDir, steps, wait.Error ?? "layout wait failed");

            var screenshot = await _facade.ScreenshotAsync(sessionId, ct).ConfigureAwait(false);
            if (!screenshot.Ok)
                return Fail(request.Flow!, evidenceDir, steps, screenshot.Error ?? "screenshot failed");
            await PersistScreenshotAsync(runId, evidenceDir, screenshot.Output, ct).ConfigureAwait(false);

            var content = await _facade.GetContentAsync(sessionId, asMarkdown: true, _markdown, ct).ConfigureAwait(false);
            if (content.Ok)
            {
                await File.WriteAllTextAsync(Path.Combine(evidenceDir, "visual-dom.md"), content.Output, ct)
                    .ConfigureAwait(false);
                extracted["content_length"] = content.Output.Length;
            }

            var title = await _facade.ExtractAsync(sessionId, ["title"], ct).ConfigureAwait(false);
            extracted["page_title"] = title.Output;
            extracted["has_layout"] = true;

            await WriteEvidenceAsync(evidenceDir, "steps.json", steps, ct).ConfigureAwait(false);
            await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);

            return new ComputerSubagentResult(true, "visual-design-check PASS", evidenceDir, true, extracted);
        }
        catch (Exception ex)
        {
            if (sessionId is not null)
                await _facade.CloseAsync(sessionId, ct).ConfigureAwait(false);
            return Fail(request.Flow!, evidenceDir, steps, ex.Message);
        }
    }

    private string ResolveUrl(Guid runId, string url) =>
        _networkRouter?.ResolveForBrowser(runId, url) ?? url;

    private static void ApplyDefaultSelectors(Dictionary<string, string> parameters)
    {
        parameters.TryAdd("username_selector", "input[name='username'], input[type='email'], #username");
        parameters.TryAdd("password_selector", "input[type='password'], input[name='password'], #password");
        parameters.TryAdd("submit_selector", "button[type='submit'], input[type='submit'], #login-submit");
        parameters.TryAdd("success_selector", "#dashboard, [data-testid='dashboard'], .dashboard, main h1");
        parameters.TryAdd("form_selector", "form, [data-testid='form']");
        parameters.TryAdd("field_selector", "input[name='email'], input[type='text']");
    }

    private static string Param(ComputerFlowRequest request, string key, string fallback) =>
        request.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private async Task PersistScreenshotAsync(Guid runId, string evidenceDir, string output, CancellationToken ct)
    {
        var b64 = ExtractBase64(output);
        if (string.IsNullOrWhiteSpace(b64))
            return;

        var bytes = Convert.FromBase64String(b64);
        var path = Path.Combine(evidenceDir, "screenshot.png");
        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);

        if (_evidence is not null)
        {
            await _evidence.PersistAsync(
                runId,
                ObscuraEvidenceKind.Screenshot,
                bytes,
                new ObscuraEvidencePersistOptions(LogicalName: "computer-flow", ToolName: "computer"),
                ct).ConfigureAwait(false);
        }
    }

    private static async Task WriteEvidenceAsync(string dir, string fileName, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(dir, fileName), json, ct).ConfigureAwait(false);
    }

    private static ComputerSubagentResult Fail(
        string flow,
        string evidenceDir,
        List<ComputerFlowStepResult> steps,
        string error) =>
        new(false, $"{flow} FAIL: {error}", evidenceDir, true, new Dictionary<string, object> { ["error"] = error });

    private static string? ExtractBase64(string output)
    {
        const string marker = "base64=";
        var idx = output.IndexOf(marker, StringComparison.Ordinal);
        return idx < 0 ? null : output[(idx + marker.Length)..].Trim();
    }

    private static ToolContext BuildToolContext(Guid runId, ToolContext parent)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-computer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = parent.Workspace.HostPath.Length > 0
                ? parent.Workspace
                : new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = parent.Accessor,
            WorkingFiles = parent.WorkingFiles,
            FileState = parent.FileState,
            Plan = parent.Plan,
            Mode = AgentSessionMode.Repair,
            BuildLog = parent.BuildLog,
            Session = new AgentSessionState { RunId = runId },
            ToolInput = default
        };
    }

    private sealed class StubRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "stub";
        public string SessionId => "stub";
        public string HostMountPath => string.Empty;
        public string GuestMountPath => "/workspace";
        public string Image => "stub";
        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
