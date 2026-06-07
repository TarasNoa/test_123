using System.Text.Json;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Routes legacy MCP browser-lane tools (browser.smoke, browser.auth) to Obscura native services.
/// </summary>
public sealed class ObscuraMcpBridge : IObscuraMcpBridge
{
    private static readonly HashSet<string> SupportedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "browser.smoke",
        "browser.auth"
    };

    private readonly IAgentObscuraTool _agentTool;
    private readonly IOptions<McpExecutionOptions> _mcpOptions;
    private readonly ILogger<ObscuraMcpBridge> _logger;

    public ObscuraMcpBridge(
        IAgentObscuraTool agentTool,
        IOptions<McpExecutionOptions> mcpOptions,
        ILogger<ObscuraMcpBridge> logger)
    {
        _agentTool = agentTool;
        _mcpOptions = mcpOptions;
        _logger = logger;
    }

    public bool CanHandle(string toolName, McpExecutionOptions options) =>
        SupportedTools.Contains(toolName) && options.BrowserLane.UsesObscuraProvider();

    public async Task<McpInvocationOutcome> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        Guid? runId,
        CancellationToken ct = default)
    {
        try
        {
            return toolName.ToLowerInvariant() switch
            {
                "browser.smoke" => await InvokeSmokeAsync(arguments, runId, ct).ConfigureAwait(false),
                "browser.auth" => await InvokeAuthAsync(arguments, runId, ct).ConfigureAwait(false),
                _ => new McpInvocationOutcome(false, "registry_miss", $"Unsupported browser tool: {toolName}", null)
            };
        }
        catch (Exception ex)
        {
            var msg = ex.Message.Length > 512 ? ex.Message[..512] : ex.Message;
            _logger.LogWarning(ex, "Obscura MCP bridge failed for {Tool}", toolName);
            return new McpInvocationOutcome(false, "obscura_bridge_error", msg, null);
        }
    }

    private async Task<McpInvocationOutcome> InvokeSmokeAsync(
        IReadOnlyDictionary<string, object?> arguments,
        Guid? runId,
        CancellationToken ct)
    {
        var options = _mcpOptions.Value;
        var profile = options.BrowserProfiles.GetValueOrDefault("smoke");
        var url = GetStringArgument(arguments, "url")
            ?? GetStringArgument(arguments, "baseUrl")
            ?? profile?.BaseUrl;

        if (string.IsNullOrWhiteSpace(url))
            return new McpInvocationOutcome(false, "bad_request", "url is required for browser.smoke", null);

        var scrape = await _agentTool.ScrapeAsync(
            url,
            new ScrapeOptions
            {
                TakeScreenshot = true,
                WaitAfterLoadMs = profile?.TimeoutMs > 0 ? Math.Min(profile.TimeoutMs, 15_000) : 2_000,
                RunId = runId?.ToString("D")
            },
            ct).ConfigureAwait(false);

        var summary = JsonSerializer.Serialize(new
        {
            provider = "obscura",
            tool = "browser.smoke",
            mode = GetStringArgument(arguments, "mode"),
            query = GetStringArgument(arguments, "query"),
            url = scrape.Url,
            title = scrape.Title,
            text = Truncate(scrape.TextContent, 1_500),
            repository_url = ExtractRepositoryUrl(scrape),
            license = ExtractLicenseHint(scrape.TextContent),
            screenshot_b64 = scrape.Screenshot is { Length: > 0 }
                ? Convert.ToBase64String(scrape.Screenshot)
                : null,
            links = scrape.Links.Take(12).ToArray()
        });

        _logger.LogInformation("Obscura browser.smoke completed for {Url}", url);
        return new McpInvocationOutcome(true, "obscura_succeeded", null, summary);
    }

    private async Task<McpInvocationOutcome> InvokeAuthAsync(
        IReadOnlyDictionary<string, object?> arguments,
        Guid? runId,
        CancellationToken ct)
    {
        var options = _mcpOptions.Value;
        var profile = options.BrowserProfiles.GetValueOrDefault("auth");
        var url = GetStringArgument(arguments, "url")
            ?? GetStringArgument(arguments, "baseUrl")
            ?? profile?.BaseUrl;

        if (string.IsNullOrWhiteSpace(url))
            return new McpInvocationOutcome(false, "bad_request", "url is required for browser.auth", null);

        var actions = BuildAuthActions(arguments, profile);
        var actionResult = await _agentTool.PerformActionsAsync(
            url,
            actions,
            new ActionOptions
            {
                TakeScreenshots = true,
                RunId = runId?.ToString("D")
            },
            ct).ConfigureAwait(false);

        if (!actionResult.Success)
        {
            return new McpInvocationOutcome(
                false,
                "obscura_auth_failed",
                actionResult.Error ?? "browser.auth flow failed",
                JsonSerializer.Serialize(new
                {
                    provider = "obscura",
                    tool = "browser.auth",
                    success = false,
                    logs = actionResult.Logs
                }));
        }

        var summary = JsonSerializer.Serialize(new
        {
            provider = "obscura",
            tool = "browser.auth",
            success = true,
            startUrl = actionResult.StartUrl,
            finalUrl = actionResult.FinalUrl,
            logs = actionResult.Logs,
            screenshot_b64 = actionResult.Screenshots.LastOrDefault()?.Data is { Length: > 0 } shot
                ? Convert.ToBase64String(shot)
                : null
        });

        _logger.LogInformation("Obscura browser.auth completed for {Url}", url);
        return new McpInvocationOutcome(true, "obscura_succeeded", null, summary);
    }

    private static BrowserAction[] BuildAuthActions(
        IReadOnlyDictionary<string, object?> arguments,
        BrowserLaneProfile? profile)
    {
        if (TryParseActionsArgument(arguments, out var parsed) && parsed.Length > 0)
            return parsed;

        var username = GetStringArgument(arguments, "username")
            ?? GetProfileEnv(profile, "AUTH_TEST_USER");
        var password = GetStringArgument(arguments, "password")
            ?? GetProfileEnv(profile, "AUTH_TEST_PASS");
        var userSelector = GetStringArgument(arguments, "username_selector")
            ?? GetStringArgument(arguments, "userSelector")
            ?? "input[name=username], input[type=email], #username, #email";
        var passSelector = GetStringArgument(arguments, "password_selector")
            ?? GetStringArgument(arguments, "passwordSelector")
            ?? "input[name=password], input[type=password], #password";
        var submitSelector = GetStringArgument(arguments, "submit_selector")
            ?? GetStringArgument(arguments, "submitSelector")
            ?? "button[type=submit], input[type=submit], button.login, #login";

        var actions = new List<BrowserAction>();
        if (!string.IsNullOrWhiteSpace(userSelector) && !string.IsNullOrWhiteSpace(username))
        {
            actions.Add(new BrowserAction
            {
                Type = BrowserActionType.WaitForElement,
                Selector = userSelector,
                WaitMs = 8_000
            });
            actions.Add(new BrowserAction
            {
                Type = BrowserActionType.Type,
                Selector = userSelector,
                Value = username
            });
        }

        if (!string.IsNullOrWhiteSpace(passSelector) && !string.IsNullOrWhiteSpace(password))
        {
            actions.Add(new BrowserAction
            {
                Type = BrowserActionType.Type,
                Selector = passSelector,
                Value = password
            });
        }

        if (!string.IsNullOrWhiteSpace(submitSelector))
        {
            actions.Add(new BrowserAction
            {
                Type = BrowserActionType.Click,
                Selector = submitSelector
            });
            actions.Add(new BrowserAction
            {
                Type = BrowserActionType.Wait,
                WaitMs = 1_500
            });
        }

        actions.Add(new BrowserAction { Type = BrowserActionType.Screenshot });
        return actions.ToArray();
    }

    private static bool TryParseActionsArgument(
        IReadOnlyDictionary<string, object?> arguments,
        out BrowserAction[] actions)
    {
        actions = Array.Empty<BrowserAction>();
        if (!arguments.TryGetValue("actions", out var raw) || raw is null)
            return false;

        try
        {
            var json = raw switch
            {
                string s => s,
                JsonElement je => je.GetRawText(),
                _ => JsonSerializer.Serialize(raw)
            };

            var parsed = JsonSerializer.Deserialize<List<BrowserActionDto>>(json);
            if (parsed is null || parsed.Count == 0)
                return false;

            actions = parsed.Select(dto => new BrowserAction
            {
                Type = ParseActionType(dto.Type),
                Selector = dto.Selector,
                Value = dto.Value,
                WaitMs = dto.WaitMs
            }).ToArray();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static BrowserActionType ParseActionType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "click" => BrowserActionType.Click,
            "type" => BrowserActionType.Type,
            "wait" => BrowserActionType.Wait,
            "wait_for_element" or "waitforelement" => BrowserActionType.WaitForElement,
            "screenshot" => BrowserActionType.Screenshot,
            "navigate" => BrowserActionType.Navigate,
            "execute_script" or "executescript" => BrowserActionType.ExecuteScript,
            "get_content" or "getcontent" => BrowserActionType.GetContent,
            "scroll" => BrowserActionType.Scroll,
            _ => BrowserActionType.Wait
        };

    private static string? ExtractRepositoryUrl(ScrapeResult scrape)
    {
        var candidate = scrape.Links.FirstOrDefault(l =>
            l.Contains("github.com/", StringComparison.OrdinalIgnoreCase) &&
            !l.Contains("/search", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(candidate))
            return candidate;

        foreach (var token in scrape.TextContent.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Contains("github.com/", StringComparison.OrdinalIgnoreCase))
                return token.TrimEnd(',', ';', ')', ']', '"', '\'');
        }

        return null;
    }

    private static string? ExtractLicenseHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lower = text.ToLowerInvariant();
        foreach (var license in new[] { "mit", "apache-2.0", "bsd", "isc", "mpl-2.0", "mpl" })
        {
            if (lower.Contains(license, StringComparison.Ordinal))
                return license;
        }

        return lower.Contains("license", StringComparison.Ordinal) ? "license-mentioned" : null;
    }

    private static string? GetProfileEnv(BrowserLaneProfile? profile, string key)
    {
        if (profile is null)
            return null;
        return profile.Environment.TryGetValue(key, out var value) ? value : null;
    }

    private static string? GetStringArgument(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var raw) || raw is null)
            return null;
        if (raw is string s)
            return s;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.String)
            return je.GetString();
        return raw.ToString();
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    private sealed class BrowserActionDto
    {
        public string? Type { get; set; }
        public string? Selector { get; set; }
        public string? Value { get; set; }
        public int? WaitMs { get; set; }
    }
}
