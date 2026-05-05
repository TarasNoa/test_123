using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using BrowserAutomation;

namespace Libr4.IDE.Application.Obscura;

/// <summary>
/// Golden Stack: Thin C# gRPC client to Rust browser-automation service
/// All browser automation moved to Rust (obscura/crates/browser-automation)
/// Uses chromiumoxide (Chromium/Playwright protocol)
/// </summary>
public class BrowserAutomationGrpcClient : IBrowserAutomationService, IDisposable
{
    private readonly ILogger<BrowserAutomationGrpcClient> _logger;
    private readonly BrowserAutomation.BrowserAutomation.BrowserAutomationClient _client;
    private readonly GrpcChannel _channel;

    public BrowserAutomationGrpcClient(
        ILogger<BrowserAutomationGrpcClient> logger,
        string? address = null)
    {
        _logger = logger;
        var grpcAddress = address ?? Environment.GetEnvironmentVariable("BROWSER_AUTOMATION_ADDR") 
            ?? "http://localhost:50052";
        
        _logger.LogInformation("Connecting to Rust browser automation at {Address}", grpcAddress);
        
        _channel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 50 * 1024 * 1024,
            MaxSendMessageSize = 50 * 1024 * 1024,
        });
        
        _client = new BrowserAutomation.BrowserAutomation.BrowserAutomationClient(_channel);
    }

    public async Task<string> LaunchBrowserAsync(
        string browserId,
        bool headless = true,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var request = new LaunchBrowserRequest
        {
            BrowserId = browserId,
            Headless = headless,
            UserAgent = userAgent ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        };

        _logger.LogInformation("Launching browser {BrowserId} (headless: {Headless})", browserId, headless);
        
        var response = await _client.LaunchBrowserAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to launch browser: {response.Error}");
        }

        _logger.LogInformation("Browser {BrowserId} launched: {Endpoint}", browserId, response.WsEndpoint);
        return response.WsEndpoint;
    }

    public async Task CloseBrowserAsync(string browserId, CancellationToken ct = default)
    {
        var request = new CloseBrowserRequest { BrowserId = browserId };
        
        _logger.LogInformation("Closing browser {BrowserId}", browserId);
        
        var response = await _client.CloseBrowserAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to close browser {BrowserId}: {Error}", browserId, response.Error);
        }
    }

    public async Task<NavigationResult> NavigateAsync(
        string browserId,
        string url,
        bool waitUntilNetworkIdle = true,
        CancellationToken ct = default)
    {
        var request = new NavigateRequest
        {
            BrowserId = browserId,
            Url = url,
            WaitUntilNetworkIdle = waitUntilNetworkIdle,
            TimeoutMs = 30000
        };

        _logger.LogInformation("Navigating browser {BrowserId} to {Url}", browserId, url);
        
        var response = await _client.NavigateAsync(request, cancellationToken: ct);
        
        return new NavigationResult
        {
            Success = response.Success,
            StatusCode = response.StatusCode,
            FinalUrl = response.FinalUrl,
            Error = response.Error
        };
    }

    public async Task<string> GetPageSourceAsync(string browserId, CancellationToken ct = default)
    {
        var request = new GetPageSourceRequest { BrowserId = browserId };
        
        var response = await _client.GetPageSourceAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to get page source: {response.Error}");
        }

        return response.Html;
    }

    public async Task<string> GetPageTextAsync(string browserId, CancellationToken ct = default)
    {
        var request = new GetPageTextRequest { BrowserId = browserId };
        
        var response = await _client.GetPageTextAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to get page text: {response.Error}");
        }

        return response.Text;
    }

    public async Task<byte[]> TakeScreenshotAsync(
        string browserId,
        bool fullPage = false,
        string? selector = null,
        CancellationToken ct = default)
    {
        var request = new TakeScreenshotRequest
        {
            BrowserId = browserId,
            Selector = selector ?? "",
            Options = new BrowserAutomation.ScreenshotOptions
            {
                FullPage = fullPage,
                Format = "png"
            }
        };

        _logger.LogDebug("Taking screenshot for {BrowserId} (fullPage: {FullPage})", browserId, fullPage);
        
        var response = await _client.TakeScreenshotAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to take screenshot: {response.Error}");
        }

        return response.ImageData.ToByteArray();
    }

    public async Task<string> ExtractElementAsync(
        string browserId,
        string selector,
        ExtractionType extractionType = ExtractionType.Text,
        string? attributeName = null,
        CancellationToken ct = default)
    {
        var request = new ExtractElementRequest
        {
            BrowserId = browserId,
            Selector = selector,
            ExtractionType = (BrowserAutomation.ExtractionType)(int)extractionType,
            AttributeName = attributeName ?? ""
        };

        var response = await _client.ExtractElementAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to extract element: {response.Error}");
        }

        return response.Value;
    }

    public async Task<List<string>> ExtractMultipleAsync(
        string browserId,
        string selector,
        ExtractionType extractionType = ExtractionType.Text,
        string? attributeName = null,
        CancellationToken ct = default)
    {
        var request = new ExtractMultipleRequest
        {
            BrowserId = browserId,
            Selector = selector,
            ExtractionType = (BrowserAutomation.ExtractionType)(int)extractionType,
            AttributeName = attributeName ?? ""
        };

        var response = await _client.ExtractMultipleAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to extract elements: {response.Error}");
        }

        return response.Values.ToList();
    }

    public async Task<T> ExecuteJavaScriptAsync<T>(
        string browserId,
        string script,
        CancellationToken ct = default)
    {
        var request = new ExecuteJavaScriptRequest
        {
            BrowserId = browserId,
            Script = script
        };

        var response = await _client.ExecuteJavaScriptAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to execute JavaScript: {response.Error}");
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(response.ResultJson) 
            ?? throw new InvalidOperationException("Failed to deserialize JavaScript result");
    }

    public async Task ClickElementAsync(
        string browserId,
        string selector,
        CancellationToken ct = default)
    {
        var request = new ClickElementRequest
        {
            BrowserId = browserId,
            Selector = selector,
            TimeoutMs = 5000
        };

        var response = await _client.ClickElementAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to click element: {response.Error}");
        }
    }

    public async Task TypeTextAsync(
        string browserId,
        string selector,
        string text,
        bool clearFirst = true,
        CancellationToken ct = default)
    {
        var request = new TypeTextRequest
        {
            BrowserId = browserId,
            Selector = selector,
            Text = text,
            ClearFirst = clearFirst,
            TimeoutMs = 5000
        };

        var response = await _client.TypeTextAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to type text: {response.Error}");
        }
    }

    public async Task<MarkdownConversionResult> ConvertToMarkdownAsync(
        string browserId,
        bool includeImages = true,
        CancellationToken ct = default)
    {
        var request = new ConvertToMarkdownRequest
        {
            BrowserId = browserId,
            IncludeImages = includeImages
        };

        _logger.LogInformation("Converting page to markdown for {BrowserId}", browserId);
        
        var response = await _client.ConvertToMarkdownAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to convert to markdown: {response.Error}");
        }

        return new MarkdownConversionResult
        {
            Markdown = response.Markdown,
            Title = response.Title
        };
    }

    public async Task CrawlAsync(
        string browserId,
        string startUrl,
        int maxPages = 10,
        int maxDepth = 2,
        Action<CrawlEvent>? onEvent = null,
        CancellationToken ct = default)
    {
        var request = new ObscuraCrawlRequest
        {
            BrowserId = browserId,
            StartUrl = startUrl,
            MaxPages = maxPages,
            MaxDepth = maxDepth,
            SameDomainOnly = true
        };

        _logger.LogInformation("Starting crawl from {StartUrl} (maxPages: {MaxPages}, maxDepth: {MaxDepth})", 
            startUrl, maxPages, maxDepth);

        using var streamingCall = _client.ObscuraCrawl(request, cancellationToken: ct);
        
        await foreach (var evt in streamingCall.ResponseStream.ReadAllAsync(ct))
        {
            onEvent?.Invoke(new CrawlEvent
            {
                Type = evt.EventCase.ToString(),
                Url = evt.PageCrawled?.Url ?? evt.CrawlError?.Url,
                Title = evt.PageCrawled?.Title,
                LinksFound = (int)(evt.PageCrawled?.LinksFound ?? 0),
                Error = evt.CrawlError?.Error,
                PagesCrawled = (int)(evt.CrawlComplete?.PagesCrawled ?? 0),
                Errors = (int)(evt.CrawlComplete?.Errors ?? 0)
            });

            if (evt.EventCase == ObscuraCrawlEvent.EventOneofCase.CrawlComplete)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

// Result DTOs
public class NavigationResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string FinalUrl { get; set; } = "";
    public string Error { get; set; } = "";
}

public class MarkdownConversionResult
{
    public string Markdown { get; set; } = "";
    public string Title { get; set; } = "";
}

public class CrawlEvent
{
    public string Type { get; set; } = "";
    public string? Url { get; set; }
    public string? Title { get; set; }
    public int LinksFound { get; set; }
    public string? Error { get; set; }
    public int PagesCrawled { get; set; }
    public int Errors { get; set; }
}

public enum ExtractionType
{
    Text = 0,
    Html = 1,
    Attribute = 2,
    Value = 3,
    Href = 4,
    Src = 5
}

public interface IBrowserAutomationService
{
    Task<string> LaunchBrowserAsync(string browserId, bool headless = true, string? userAgent = null, CancellationToken ct = default);
    Task CloseBrowserAsync(string browserId, CancellationToken ct = default);
    Task<NavigationResult> NavigateAsync(string browserId, string url, bool waitUntilNetworkIdle = true, CancellationToken ct = default);
    Task<string> GetPageSourceAsync(string browserId, CancellationToken ct = default);
    Task<string> GetPageTextAsync(string browserId, CancellationToken ct = default);
    Task<byte[]> TakeScreenshotAsync(string browserId, bool fullPage = false, string? selector = null, CancellationToken ct = default);
    Task<string> ExtractElementAsync(string browserId, string selector, ExtractionType extractionType = ExtractionType.Text, string? attributeName = null, CancellationToken ct = default);
    Task<List<string>> ExtractMultipleAsync(string browserId, string selector, ExtractionType extractionType = ExtractionType.Text, string? attributeName = null, CancellationToken ct = default);
    Task<T> ExecuteJavaScriptAsync<T>(string browserId, string script, CancellationToken ct = default);
    Task ClickElementAsync(string browserId, string selector, CancellationToken ct = default);
    Task TypeTextAsync(string browserId, string selector, string text, bool clearFirst = true, CancellationToken ct = default);
    Task<MarkdownConversionResult> ConvertToMarkdownAsync(string browserId, bool includeImages = true, CancellationToken ct = default);
    Task CrawlAsync(string browserId, string startUrl, int maxPages = 10, int maxDepth = 2, Action<CrawlEvent>? onEvent = null, CancellationToken ct = default);
}
