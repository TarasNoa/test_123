using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Obscura;

/// <summary>
/// Integration between Obscura browser and subagent system
/// Allows subagents to use browser automation via configuration
/// </summary>
public interface ISubagentObscuraIntegration
{
    /// <summary>
    /// Execute browser task defined in subagent configuration
    /// </summary>
    Task<SubagentBrowserResult> ExecuteBrowserTaskAsync(
        string subagentId,
        string taskName,
        Dictionary<string, string> parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Check if subagent has browser capabilities
    /// </summary>
    bool HasBrowserCapabilities(string subagentId);

    /// <summary>
    /// Get available browser tasks for subagent
    /// </summary>
    IReadOnlyList<string> GetAvailableTasks(string subagentId);

    /// <summary>
    /// Register browser tool for subagent
    /// </summary>
    void RegisterSubagentBrowserConfig(string subagentId, SubagentBrowserConfig config);

    /// <summary>
    /// Scrape data using subagent's configured selectors
    /// </summary>
    Task<SubagentScrapeResult> ScrapeWithSubagentConfigAsync(
        string subagentId,
        string url,
        CancellationToken ct = default);
}

/// <summary>
/// Subagent browser configuration
/// </summary>
public class SubagentBrowserConfig
{
    public string SubagentId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool StealthMode { get; set; } = true;
    public string? DefaultUserAgent { get; set; }
    public (int width, int height)? DefaultViewport { get; set; }
    public List<SubagentBrowserTaskDefinition> Tasks { get; set; } = new();
    public List<DataSelector> DataSelectors { get; set; } = new();
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}

/// <summary>
/// Browser task definition for subagent
/// </summary>
public class SubagentBrowserTaskDefinition
{
    public string TaskName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<BrowserActionTemplate> Actions { get; set; } = new();
    public List<DataExtractionRule> ExtractionRules { get; set; } = new();
    public int? TimeoutSeconds { get; set; }
    public bool TakeScreenshots { get; set; } = true;
}

/// <summary>
/// Browser action template with parameter placeholders
/// </summary>
public class BrowserActionTemplate
{
    public BrowserActionType Type { get; set; }
    public string? Selector { get; set; }  // Can contain {{parameterName}}
    public string? Value { get; set; }     // Can contain {{parameterName}}
    public int? WaitMs { get; set; }
}

/// <summary>
/// Data extraction rule
/// </summary>
public class DataExtractionRule
{
    public string FieldName { get; set; } = string.Empty;
    public DataExtractionType Type { get; set; }
    public string Selector { get; set; } = string.Empty;  // CSS or XPath
    public string? Attribute { get; set; }  // e.g., "href", "src", null for text
    public string? DefaultValue { get; set; }
    public bool Required { get; set; } = false;
}

public enum DataExtractionType
{
    Text,
    Attribute,
    Html,
    Count,
    Exists,
    JavaScript
}

/// <summary>
/// CSS Selector for data extraction
/// </summary>
public class DataSelector
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public SelectorType Type { get; set; }
    public string? Attribute { get; set; }
}

public enum SelectorType
{
    Css,
    XPath,
    JavaScript
}

/// <summary>
/// Implementation
/// </summary>
public class SubagentObscuraIntegration : ISubagentObscuraIntegration
{
    private readonly IObscuraBrowserService _browserService;
    private readonly IAgentObscuraTool _agentTool;
    private readonly ILogger<SubagentObscuraIntegration> _logger;
    private readonly Dictionary<string, SubagentBrowserConfig> _configs = new();
    private readonly object _lock = new();

    public SubagentObscuraIntegration(
        IObscuraBrowserService browserService,
        IAgentObscuraTool agentTool,
        ILogger<SubagentObscuraIntegration> logger)
    {
        _browserService = browserService;
        _agentTool = agentTool;
        _logger = logger;
    }

    public async Task<SubagentBrowserResult> ExecuteBrowserTaskAsync(
        string subagentId,
        string taskName,
        Dictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        var config = GetConfig(subagentId);
        if (config == null)
        {
            throw new InvalidOperationException($"Subagent {subagentId} has no browser configuration");
        }

        var taskDef = config.Tasks.FirstOrDefault(t => t.TaskName == taskName);
        if (taskDef == null)
        {
            throw new InvalidOperationException($"Task {taskName} not found for subagent {subagentId}");
        }

        _logger.LogInformation(
            "Executing browser task {TaskName} for subagent {SubagentId} with {ParamCount} parameters",
            taskName, subagentId, parameters.Count);

        // Build actions from template
        var actions = BuildActionsFromTemplate(taskDef.Actions, parameters);

        // Launch browser
        var sessionId = await _browserService.LaunchBrowserAsync(new ObscuraLaunchOptions
        {
            StealthMode = config.StealthMode,
            UserAgent = config.DefaultUserAgent,
            Viewport = config.DefaultViewport
        }, ct);

        try
        {
            var startUrl = parameters.GetValueOrDefault("url", "about:blank");
            
            // Execute actions
            var actionResult = await ExecuteActionsAsync(sessionId, actions, taskDef, ct);

            // Extract data
            var extractedData = new Dictionary<string, object>();
            if (taskDef.ExtractionRules.Any())
            {
                extractedData = await ExtractDataAsync(sessionId, taskDef.ExtractionRules, ct);
            }

            // Get final screenshot
            byte[]? finalScreenshot = null;
            if (taskDef.TakeScreenshots)
            {
                finalScreenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);
            }

            return new SubagentBrowserResult
            {
                SubagentId = subagentId,
                TaskName = taskName,
                Success = actionResult.Success,
                ActionsExecuted = actionResult.ExecutedCount,
                ExtractedData = extractedData,
                Screenshots = actionResult.Screenshots,
                FinalScreenshot = finalScreenshot,
                Logs = actionResult.Logs,
                DurationMs = actionResult.DurationMs,
                Error = actionResult.Error
            };
        }
        finally
        {
            await _browserService.CloseBrowserAsync(sessionId, ct);
        }
    }

    public bool HasBrowserCapabilities(string subagentId)
    {
        var config = GetConfig(subagentId);
        return config?.Enabled == true && config.Tasks.Any();
    }

    public IReadOnlyList<string> GetAvailableTasks(string subagentId)
    {
        var config = GetConfig(subagentId);
        if (config == null) return Array.Empty<string>();
        return config.Tasks.Select(t => t.TaskName).ToList();
    }

    public void RegisterSubagentBrowserConfig(string subagentId, SubagentBrowserConfig config)
    {
        lock (_lock)
        {
            _configs[subagentId] = config;
        }
        _logger.LogInformation("Registered browser config for subagent {SubagentId} with {TaskCount} tasks", 
            subagentId, config.Tasks.Count);
    }

    public async Task<SubagentScrapeResult> ScrapeWithSubagentConfigAsync(
        string subagentId,
        string url,
        CancellationToken ct = default)
    {
        var config = GetConfig(subagentId);
        if (config == null || !config.DataSelectors.Any())
        {
            throw new InvalidOperationException($"Subagent {subagentId} has no data selectors configured");
        }

        _logger.LogInformation("Scraping {Url} using subagent {SubagentId} selectors", url, subagentId);

        // Launch browser
        var sessionId = await _browserService.LaunchBrowserAsync(new ObscuraLaunchOptions
        {
            StealthMode = config.StealthMode,
            UserAgent = config.DefaultUserAgent,
            Viewport = config.DefaultViewport
        }, ct);

        try
        {
            // Navigate
            await _browserService.NavigateAsync(sessionId, url, ct);
            await Task.Delay(2000, ct);

            // Extract using selectors
            var extractedData = new Dictionary<string, object>();
            var extractionLogs = new List<string>();

            foreach (var selector in config.DataSelectors)
            {
                try
                {
                    object? value = null;
                    
                    switch (selector.Type)
                    {
                        case SelectorType.Css:
                            value = await ExtractWithCssSelectorAsync(sessionId, selector, ct);
                            break;
                            
                        case SelectorType.XPath:
                            value = await ExtractWithXPathAsync(sessionId, selector, ct);
                            break;
                            
                        case SelectorType.JavaScript:
                            value = await ExtractWithJavaScriptAsync(sessionId, selector, ct);
                            break;
                    }

                    extractedData[selector.Name] = value ?? "null";
                    extractionLogs.Add($"{selector.Name}: {value ?? "null"}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract {SelectorName} for subagent {SubagentId}", 
                        selector.Name, subagentId);
                    extractedData[selector.Name] = $"ERROR: {ex.Message}";
                }
            }

            // Screenshot
            var screenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);

            return new SubagentScrapeResult
            {
                SubagentId = subagentId,
                Url = url,
                Data = extractedData,
                ExtractionLogs = extractionLogs,
                Screenshot = screenshot,
                ScrapedAt = DateTime.UtcNow
            };
        }
        finally
        {
            await _browserService.CloseBrowserAsync(sessionId, ct);
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private SubagentBrowserConfig? GetConfig(string subagentId)
    {
        lock (_lock)
        {
            _configs.TryGetValue(subagentId, out var config);
            return config;
        }
    }

    private List<BrowserAction> BuildActionsFromTemplate(List<BrowserActionTemplate> templates, Dictionary<string, string> parameters)
    {
        var actions = new List<BrowserAction>();

        foreach (var template in templates)
        {
            var selector = ReplaceParameters(template.Selector, parameters);
            var value = ReplaceParameters(template.Value, parameters);

            actions.Add(new BrowserAction
            {
                Type = template.Type,
                Selector = selector,
                Value = value,
                WaitMs = template.WaitMs
            });
        }

        return actions;
    }

    private string? ReplaceParameters(string? template, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(template)) return template;

        var result = template;
        foreach (var param in parameters)
        {
            result = result.Replace($"{{{{{param.Key}}}}}", param.Value);
        }
        return result;
    }

    private async Task<ActionExecutionResult> ExecuteActionsAsync(
        string sessionId, 
        List<BrowserAction> actions, 
        SubagentBrowserTaskDefinition taskDef,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var logs = new List<string>();
        var screenshots = new List<SubagentScreenshot>();
        int executedCount = 0;

        foreach (var action in actions)
        {
            try
            {
                switch (action.Type)
                {
                    case BrowserActionType.Navigate:
                        if (!string.IsNullOrEmpty(action.Value))
                        {
                            await _browserService.NavigateAsync(sessionId, action.Value, ct);
                            logs.Add($"Navigated to: {action.Value}");
                        }
                        break;

                    case BrowserActionType.Click:
                        if (!string.IsNullOrEmpty(action.Selector))
                        {
                            await _browserService.ClickAsync(sessionId, action.Selector, ct);
                            logs.Add($"Clicked: {action.Selector}");
                        }
                        break;

                    case BrowserActionType.Type:
                        if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                        {
                            await _browserService.TypeAsync(sessionId, action.Selector, action.Value, ct);
                            logs.Add($"Typed into: {action.Selector}");
                        }
                        break;

                    case BrowserActionType.WaitForElement:
                        if (!string.IsNullOrEmpty(action.Selector))
                        {
                            await _browserService.WaitForElementAsync(sessionId, action.Selector, action.WaitMs ?? 5000, ct);
                            logs.Add($"Waited for element: {action.Selector}");
                        }
                        break;

                    case BrowserActionType.Wait:
                        await Task.Delay(action.WaitMs ?? 1000, ct);
                        logs.Add($"Waited {action.WaitMs ?? 1000}ms");
                        break;

                    case BrowserActionType.Screenshot:
                        if (taskDef.TakeScreenshots)
                        {
                            var screenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);
                            screenshots.Add(new SubagentScreenshot
                            {
                                ActionIndex = executedCount,
                                Data = screenshot,
                                Timestamp = DateTime.UtcNow
                            });
                            logs.Add("Screenshot taken");
                        }
                        break;

                    case BrowserActionType.ExecuteScript:
                        if (!string.IsNullOrEmpty(action.Value))
                        {
                            var result = await _browserService.ExecuteJavaScriptAsync(sessionId, action.Value, ct);
                            logs.Add($"Script result: {result}");
                        }
                        break;
                }

                executedCount++;
            }
            catch (Exception ex)
            {
                logs.Add($"ERROR on action {executedCount + 1}: {ex.Message}");
                return new ActionExecutionResult
                {
                    Success = false,
                    ExecutedCount = executedCount,
                    Screenshots = screenshots,
                    Logs = logs,
                    Error = ex.Message,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            await Task.Delay(500, ct);
        }

        stopwatch.Stop();

        return new ActionExecutionResult
        {
            Success = true,
            ExecutedCount = executedCount,
            Screenshots = screenshots,
            Logs = logs,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<Dictionary<string, object>> ExtractDataAsync(
        string sessionId, 
        List<DataExtractionRule> rules, 
        CancellationToken ct)
    {
        var data = new Dictionary<string, object>();

        foreach (var rule in rules)
        {
            try
            {
                string script;
                
                if (rule.Type == DataExtractionType.JavaScript)
                {
                    script = rule.Selector;
                }
                else
                {
                    var attrPart = rule.Attribute != null 
                        ? $".getAttribute('{rule.Attribute}')" 
                        : ".textContent";
                    
                    script = $@"
                        (function() {{
                            var el = document.querySelector('{rule.Selector.Replace("'", "\\'")}');
                            if (!el) return null;
                            return el{attrPart};
                        }})()
                    ";
                }

                var result = await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
                data[rule.FieldName] = result ?? rule.DefaultValue ?? "null";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract {FieldName}", rule.FieldName);
                if (rule.Required)
                {
                    throw;
                }
                data[rule.FieldName] = $"ERROR: {ex.Message}";
            }
        }

        return data;
    }

    private async Task<object?> ExtractWithCssSelectorAsync(string sessionId, DataSelector selector, CancellationToken ct)
    {
        var script = $@"
            (function() {{
                var el = document.querySelector('{selector.Selector.Replace("'", "\\'")}');
                if (!el) return null;
                return {GetReturnExpression(selector)};
            }})()
        ";
        
        var result = await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
        return result;
    }

    private async Task<object?> ExtractWithXPathAsync(string sessionId, DataSelector selector, CancellationToken ct)
    {
        var script = $@"
            (function() {{
                var result = document.evaluate('{selector.Selector.Replace("'", "\\'")}', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
                var el = result.singleNodeValue;
                if (!el) return null;
                return {GetReturnExpression(selector)};
            }})()
        ";
        
        var result = await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
        return result;
    }

    private async Task<object?> ExtractWithJavaScriptAsync(string sessionId, DataSelector selector, CancellationToken ct)
    {
        var result = await _browserService.ExecuteJavaScriptAsync(sessionId, selector.Selector, ct);
        return result;
    }

    private string GetReturnExpression(DataSelector selector)
    {
        if (selector.Attribute != null)
        {
            return $"el.getAttribute('{selector.Attribute}')";
        }
        return "el.textContent?.trim()";
    }

    private class ActionExecutionResult
    {
        public bool Success { get; set; }
        public int ExecutedCount { get; set; }
        public List<SubagentScreenshot> Screenshots { get; set; } = new();
        public List<string> Logs { get; set; } = new();
        public string? Error { get; set; }
        public int DurationMs { get; set; }
    }
}

// ============================================================================
// RESULT CLASSES
// ============================================================================

public class SubagentBrowserResult
{
    public string SubagentId { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ActionsExecuted { get; set; }
    public Dictionary<string, object> ExtractedData { get; set; } = new();
    public List<SubagentScreenshot> Screenshots { get; set; } = new();
    public byte[]? FinalScreenshot { get; set; }
    public List<string> Logs { get; set; } = new();
    public int DurationMs { get; set; }
    public string? Error { get; set; }
}

public class SubagentScreenshot
{
    public int ActionIndex { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
}

public class SubagentScrapeResult
{
    public string SubagentId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> ExtractionLogs { get; set; } = new();
    public byte[] Screenshot { get; set; } = Array.Empty<byte>();
    public DateTime ScrapedAt { get; set; }
}
