using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Obscura;

/// <summary>
/// Tool for AI agents to interact with web browsers via Obscura
/// This is the bridge between agents and browser automation
/// </summary>
public interface IAgentObscuraTool
{
    /// <summary>
    /// Execute web research task
    /// </summary>
    Task<WebResearchResult> ResearchAsync(string query, string[] sources, WebResearchOptions? options = null, CancellationToken ct = default);
    
    /// <summary>
    /// Scrape specific URL
    /// </summary>
    Task<ScrapeResult> ScrapeAsync(string url, ScrapeOptions? options = null, CancellationToken ct = default);
    
    /// <summary>
    /// Perform action sequence (login, form fill, etc.)
    /// </summary>
    Task<ActionResult> PerformActionsAsync(string startUrl, BrowserAction[] actions, ActionOptions? options = null, CancellationToken ct = default);
    
    /// <summary>
    /// Take screenshot of URL
    /// </summary>
    Task<ScreenshotResult> ScreenshotAsync(string url, ScreenshotOptions? options = null, CancellationToken ct = default);
    
    /// <summary>
    /// Extract data using JavaScript
    /// </summary>
    Task<ExtractionResult> ExtractAsync(string url, string[] extractionScripts, ExtractionOptions? options = null, CancellationToken ct = default);
    
    /// <summary>
    /// Close all browser sessions
    /// </summary>
    Task CloseAllSessionsAsync();
}

/// <summary>
/// Implementation of agent browser tool using Obscura
/// </summary>
public class AgentObscuraTool : IAgentObscuraTool
{
    private readonly IObscuraBrowserService _browserService;
    private readonly ILogger<AgentObscuraTool> _logger;
    private readonly List<string> _managedSessions = new();
    private readonly object _lock = new();

    public AgentObscuraTool(
        IObscuraBrowserService browserService,
        ILogger<AgentObscuraTool> logger)
    {
        _browserService = browserService;
        _logger = logger;
    }

    public async Task<WebResearchResult> ResearchAsync(
        string query, 
        string[] sources, 
        WebResearchOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new WebResearchOptions();
        var results = new List<WebSourceResult>();
        
        _logger.LogInformation(
            "Starting web research for query: {Query} across {SourceCount} sources",
            query, sources.Length);

        // Launch browser
        var sessionId = await LaunchBrowserForAgentAsync(options.StealthMode, ct, options.RunId, "research");
        
        try
        {
            foreach (var source in sources.Take(options.MaxSources))
            {
                try
                {
                    var result = await ResearchSingleSourceAsync(sessionId, source, query, options, ct);
                    if (result.HasContent)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to research source: {Source}", source);
                }
            }

            return new WebResearchResult
            {
                Query = query,
                Sources = results,
                TotalSourcesChecked = sources.Length,
                SuccessfulSources = results.Count,
                ResearchCompletedAt = DateTime.UtcNow
            };
        }
        finally
        {
            if (!options.KeepSessionOpen)
            {
                await _browserService.CloseBrowserAsync(sessionId, ct);
                RemoveManagedSession(sessionId);
            }
        }
    }

    public async Task<ScrapeResult> ScrapeAsync(
        string url, 
        ScrapeOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new ScrapeOptions();
        
        _logger.LogInformation("Scraping URL: {Url}", url);

        var sessionId = await LaunchBrowserForAgentAsync(options.StealthMode, ct, options.RunId, "scrape");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Navigate
            await _browserService.NavigateAsync(sessionId, url, ct);
            
            // Wait for load
            await Task.Delay(options.WaitAfterLoadMs, ct);
            
            // Get content
            var content = await _browserService.GetPageContentAsync(sessionId, ct);
            var text = await ExtractTextAsync(sessionId, ct);
            
            // Screenshot if requested
            byte[]? screenshot = null;
            if (options.TakeScreenshot)
            {
                screenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);
            }

            // Extract links
            var links = await ExtractLinksAsync(sessionId, ct);
            
            // Extract metadata
            var title = await ExecuteScriptAsync(sessionId, "document.title", ct);
            var description = await ExecuteScriptAsync(sessionId, 
                "document.querySelector('meta[name=description]')?.content || ''", ct);

            stopwatch.Stop();

            return new ScrapeResult
            {
                Url = url,
                Title = title,
                Description = description,
                HtmlContent = content,
                TextContent = text,
                Screenshot = screenshot,
                Links = links,
                ScrapedAt = DateTime.UtcNow,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape URL: {Url}", url);
            throw;
        }
        finally
        {
            if (!options.KeepSessionOpen)
            {
                await _browserService.CloseBrowserAsync(sessionId, ct);
                RemoveManagedSession(sessionId);
            }
        }
    }

    public async Task<ActionResult> PerformActionsAsync(
        string startUrl, 
        BrowserAction[] actions, 
        ActionOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new ActionOptions();
        
        _logger.LogInformation(
            "Performing {ActionCount} actions starting from {Url}",
            actions.Length, startUrl);

        var sessionId = await LaunchBrowserForAgentAsync(options.StealthMode, ct, options.RunId, "actions");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var logs = new List<string>();
        var screenshots = new List<ActionScreenshot>();

        try
        {
            // Navigate to start
            await _browserService.NavigateAsync(sessionId, startUrl, ct);
            logs.Add($"Navigated to {startUrl}");

            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                var actionId = $"action-{i + 1}";
                
                _logger.LogDebug("Executing action {Index}/{Total}: {Type}", i + 1, actions.Length, action.Type);

                try
                {
                    switch (action.Type)
                    {
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
                            if (options.TakeScreenshots)
                            {
                                var screenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);
                                screenshots.Add(new ActionScreenshot
                                {
                                    ActionIndex = i,
                                    ActionType = action.Type.ToString(),
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
                                logs.Add($"Script executed. Result: {result}");
                            }
                            break;

                        case BrowserActionType.Navigate:
                            if (!string.IsNullOrEmpty(action.Value))
                            {
                                await _browserService.NavigateAsync(sessionId, action.Value, ct);
                                logs.Add($"Navigated to: {action.Value}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"ERROR on action {i + 1}: {ex.Message}");
                    if (!options.ContinueOnError)
                    {
                        throw;
                    }
                }

                // Delay between actions
                await Task.Delay(options.DelayBetweenActionsMs, ct);
            }

            // Final screenshot
            if (options.TakeScreenshots)
            {
                var finalScreenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);
                screenshots.Add(new ActionScreenshot
                {
                    ActionIndex = actions.Length,
                    ActionType = "Final",
                    Data = finalScreenshot,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Get final content
            var finalContent = await _browserService.GetPageContentAsync(sessionId, ct);
            var finalUrl = (await _browserService.GetSessionInfoAsync(sessionId))?.CurrentUrl;

            stopwatch.Stop();

            return new ActionResult
            {
                StartUrl = startUrl,
                FinalUrl = finalUrl,
                Success = true,
                Logs = logs,
                Screenshots = screenshots,
                FinalContent = finalContent,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Action sequence failed");
            
            return new ActionResult
            {
                StartUrl = startUrl,
                Success = false,
                Logs = logs,
                Error = ex.Message,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        finally
        {
            if (!options.KeepSessionOpen)
            {
                await _browserService.CloseBrowserAsync(sessionId, ct);
                RemoveManagedSession(sessionId);
            }
        }
    }

    public async Task<ScreenshotResult> ScreenshotAsync(
        string url, 
        ScreenshotOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new ScreenshotOptions();
        
        _logger.LogInformation("Taking screenshot of: {Url}", url);

        var sessionId = await LaunchBrowserForAgentAsync(options.StealthMode, ct, options.RunId, "screenshot");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await _browserService.NavigateAsync(sessionId, url, ct);
            await Task.Delay(options.WaitAfterLoadMs, ct);
            
            // Set viewport if specified
            if (options.ViewportSize.HasValue)
            {
                await _browserService.ExecuteJavaScriptAsync(sessionId, $@"
                    window.resizeTo({options.ViewportSize.Value.width}, {options.ViewportSize.Value.height});
                ", ct);
            }
            
            var screenshot = await _browserService.TakeScreenshotAsync(sessionId, ct);
            var title = await ExecuteScriptAsync(sessionId, "document.title", ct);
            
            stopwatch.Stop();

            return new ScreenshotResult
            {
                Url = url,
                Title = title,
                ScreenshotData = screenshot,
                Timestamp = DateTime.UtcNow,
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        finally
        {
            if (!options.KeepSessionOpen)
            {
                await _browserService.CloseBrowserAsync(sessionId, ct);
                RemoveManagedSession(sessionId);
            }
        }
    }

    public async Task<ExtractionResult> ExtractAsync(
        string url, 
        string[] extractionScripts, 
        ExtractionOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new ExtractionOptions();
        
        _logger.LogInformation(
            "Extracting {ScriptCount} data points from: {Url}",
            extractionScripts.Length, url);

        var sessionId = await LaunchBrowserForAgentAsync(options.StealthMode, ct, options.RunId, "extract");
        var extractedData = new Dictionary<string, string>();
        
        try
        {
            await _browserService.NavigateAsync(sessionId, url, ct);
            await Task.Delay(options.WaitAfterLoadMs, ct);

            for (int i = 0; i < extractionScripts.Length; i++)
            {
                var script = extractionScripts[i];
                var key = $"extraction_{i + 1}";
                
                try
                {
                    var result = await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
                    extractedData[key] = result ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Extraction script {Index} failed", i + 1);
                    extractedData[key] = $"ERROR: {ex.Message}";
                }
            }

            return new ExtractionResult
            {
                Url = url,
                ExtractedData = extractedData,
                ScriptsExecuted = extractionScripts.Length,
                ExtractedAt = DateTime.UtcNow
            };
        }
        finally
        {
            if (!options.KeepSessionOpen)
            {
                await _browserService.CloseBrowserAsync(sessionId, ct);
                RemoveManagedSession(sessionId);
            }
        }
    }

    public async Task CloseAllSessionsAsync()
    {
        _logger.LogInformation("Closing all {Count} managed Obscura sessions", _managedSessions.Count);
        
        List<string> sessionsToClose;
        lock (_lock)
        {
            sessionsToClose = _managedSessions.ToList();
            _managedSessions.Clear();
        }

        foreach (var sessionId in sessionsToClose)
        {
            try
            {
                await _browserService.CloseBrowserAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close session {SessionId}", sessionId);
            }
        }
    }

    private async Task<string> LaunchBrowserForAgentAsync(
        bool stealthMode,
        CancellationToken ct,
        string? runId = null,
        string purpose = "agent")
    {
        var sessionId = await _browserService.LaunchBrowserAsync(new ObscuraLaunchOptions
        {
            StealthMode = stealthMode,
            BlockTrackers = true,
            RunId = runId,
            Purpose = purpose
        }, ct);

        lock (_lock)
        {
            _managedSessions.Add(sessionId);
        }

        return sessionId;
    }

    private void RemoveManagedSession(string sessionId)
    {
        lock (_lock)
        {
            _managedSessions.Remove(sessionId);
        }
    }

    private async Task<WebSourceResult> ResearchSingleSourceAsync(
        string sessionId, 
        string source, 
        string query, 
        WebResearchOptions options, 
        CancellationToken ct)
    {
        await _browserService.NavigateAsync(sessionId, source, ct);
        await Task.Delay(options.WaitAfterLoadMs, ct);

        var content = await _browserService.GetPageContentAsync(sessionId, ct);
        var text = await ExtractTextAsync(sessionId, ct);
        var title = await ExecuteScriptAsync(sessionId, "document.title", ct);

        // Search for query in content
        var queryLower = query.ToLower();
        var hasQuery = text.ToLower().Contains(queryLower);
        var relevanceScore = hasQuery ? 1.0 : 0.0; // Simplified scoring

        return new WebSourceResult
        {
            Url = source,
            Title = title,
            Content = text,
            HtmlContent = content,
            HasContent = !string.IsNullOrWhiteSpace(text),
            RelevanceScore = relevanceScore,
            AccessedAt = DateTime.UtcNow
        };
    }

    private async Task<string> ExtractTextAsync(string sessionId, CancellationToken ct)
    {
        var script = @"
            (function() {
                // Remove script and style elements
                var scripts = document.querySelectorAll('script, style, nav, header, footer, aside');
                scripts.forEach(el => el.remove());
                
                // Get main content or body
                var main = document.querySelector('main, article, [role=main]');
                var text = main ? main.innerText : document.body.innerText;
                
                // Clean up
                return text
                    .replace(/\s+/g, ' ')
                    .replace(/\n+/g, '\n')
                    .trim()
                    .substring(0, 50000); // Limit to 50k chars
            })()
        ";
        
        return await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
    }

    private async Task<string[]> ExtractLinksAsync(string sessionId, CancellationToken ct)
    {
        var script = @"
            Array.from(document.querySelectorAll('a[href]'))
                .map(a => a.href)
                .filter(href => href.startsWith('http'))
                .slice(0, 100)
        ";
        
        var result = await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
        return result?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    }

    private async Task<string> ExecuteScriptAsync(string sessionId, string script, CancellationToken ct)
    {
        try
        {
            return await _browserService.ExecuteJavaScriptAsync(sessionId, script, ct);
        }
        catch
        {
            return string.Empty;
        }
    }
}

// ============================================================================
// DTO CLASSES
// ============================================================================

public class WebResearchOptions
{
    public bool StealthMode { get; set; } = true;
    public int MaxSources { get; set; } = 5;
    public int WaitAfterLoadMs { get; set; } = 2000;
    public bool KeepSessionOpen { get; set; } = false;
    public string? RunId { get; set; }
}

public class WebResearchResult
{
    public string Query { get; set; } = string.Empty;
    public List<WebSourceResult> Sources { get; set; } = new();
    public int TotalSourcesChecked { get; set; }
    public int SuccessfulSources { get; set; }
    public DateTime ResearchCompletedAt { get; set; }
}

public class WebSourceResult
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public bool HasContent { get; set; }
    public double RelevanceScore { get; set; }
    public DateTime AccessedAt { get; set; }
}

public class ScrapeOptions
{
    public bool StealthMode { get; set; } = true;
    public bool TakeScreenshot { get; set; } = false;
    public int WaitAfterLoadMs { get; set; } = 2000;
    public bool KeepSessionOpen { get; set; } = false;
    public string? RunId { get; set; }
}

public class ScrapeResult
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public byte[]? Screenshot { get; set; }
    public string[] Links { get; set; } = Array.Empty<string>();
    public DateTime ScrapedAt { get; set; }
    public int DurationMs { get; set; }
}

public class ActionOptions
{
    public bool StealthMode { get; set; } = true;
    public bool TakeScreenshots { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
    public int DelayBetweenActionsMs { get; set; } = 500;
    public bool KeepSessionOpen { get; set; } = false;
    public string? RunId { get; set; }
}

public class ActionResult
{
    public string StartUrl { get; set; } = string.Empty;
    public string? FinalUrl { get; set; }
    public bool Success { get; set; }
    public List<string> Logs { get; set; } = new();
    public List<ActionScreenshot> Screenshots { get; set; } = new();
    public string? FinalContent { get; set; }
    public string? Error { get; set; }
    public int DurationMs { get; set; }
}

public class ActionScreenshot
{
    public int ActionIndex { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
}

public class ScreenshotOptions
{
    public bool StealthMode { get; set; } = true;
    public int WaitAfterLoadMs { get; set; } = 2000;
    public (int width, int height)? ViewportSize { get; set; }
    public bool KeepSessionOpen { get; set; } = false;
    public string? RunId { get; set; }
}

public class ScreenshotResult
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public byte[] ScreenshotData { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
    public int DurationMs { get; set; }
}

public class ExtractionOptions
{
    public bool StealthMode { get; set; } = true;
    public int WaitAfterLoadMs { get; set; } = 2000;
    public bool KeepSessionOpen { get; set; } = false;
    public string? RunId { get; set; }
}

public class ExtractionResult
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> ExtractedData { get; set; } = new();
    public int ScriptsExecuted { get; set; }
    public DateTime ExtractedAt { get; set; }
}
