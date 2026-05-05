# Obscura Browser Integration for Libr4

## Overview

Obscura is now fully integrated into Libr4 IDE service:
- **30MB RAM** vs 200MB+ for headless Chrome
- **85ms page load** vs ~500ms for Chrome
- **Built-in anti-detection** (stealth mode)
- **Native CDP support** (Chrome DevTools Protocol)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     LIBR4 IDE SERVICE                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            IObscuraBrowserService                   │   │
│  │  (Process management + CDP WebSocket)               │   │
│  └─────────────────────┬───────────────────────────────┘   │
│                        │                                     │
│                        │ WebSocket (CDP)                   │
│                        ▼                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │             RUST OBSCURA PROCESS                  │   │
│  │         (Port 9222, headless browser)             │   │
│  └─────────────────────────────────────────────────────┘   │
│                        │                                     │
│                        │ HTTP/WebSocket                    │
│                        ▼                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                  TARGET WEBSITE                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│  AGENT LAYER                                                │
│  ┌─────────────────┐  ┌──────────────────────────────┐     │
│  │ IAgentObscuraTool│  │ ISubagentObscuraIntegration│    │
│  │ - Research      │  │ - Configured tasks          │     │
│  │ - Scrape        │  │ - Data selectors            │     │
│  │ - Actions       │  │ - Template actions          │     │
│  └─────────────────┘  └──────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Services

### 1. IObscuraBrowserService
Low-level browser management:
```csharp
// Launch browser
var sessionId = await _browserService.LaunchBrowserAsync(new ObscuraLaunchOptions {
    StealthMode = true,
    Port = 9222
});

// Navigate
await _browserService.NavigateAsync(sessionId, "https://example.com");

// Execute JS
var title = await _browserService.ExecuteJavaScriptAsync(sessionId, "document.title");

// Screenshot
var screenshot = await _browserService.TakeScreenshotAsync(sessionId);

// Close
await _browserService.CloseBrowserAsync(sessionId);
```

### 2. IAgentObscuraTool
High-level agent automation:
```csharp
// Web research
var result = await _agentTool.ResearchAsync(
    query: "AI frameworks 2024",
    sources: new[] { "https://news.ycombinator.com", "https://reddit.com/r/MachineLearning" },
    options: new WebResearchOptions { StealthMode = true, MaxSources = 5 }
);

// Scrape single URL
var scrape = await _agentTool.ScrapeAsync(
    url: "https://example.com",
    options: new ScrapeOptions { TakeScreenshot = true }
);

// Perform actions (login, forms, etc.)
var actionResult = await _agentTool.PerformActionsAsync(
    startUrl: "https://example.com/login",
    actions: new[] {
        new BrowserAction { Type = BrowserActionType.Type, Selector = "#username", Value = "user" },
        new BrowserAction { Type = BrowserActionType.Type, Selector = "#password", Value = "pass" },
        new BrowserAction { Type = BrowserActionType.Click, Selector = "#submit" },
        new BrowserAction { Type = BrowserActionType.WaitForElement, Selector = ".dashboard" }
    }
);

// Extract data
var extraction = await _agentTool.ExtractAsync(
    url: "https://example.com/products",
    extractionScripts: new[] {
        "document.querySelector('.price')?.textContent",
        "document.querySelector('.title')?.textContent",
        "Array.from(document.querySelectorAll('.feature')).map(f => f.textContent)"
    }
);
```

### 3. ISubagentObscuraIntegration
Subagent-specific browser automation:
```csharp
// Register subagent browser config
_subagentObscura.RegisterSubagentBrowserConfig("price-scraper", new SubagentBrowserConfig {
    SubagentId = "price-scraper",
    StealthMode = true,
    DataSelectors = new List<DataSelector> {
        new DataSelector {
            Name = "price",
            Selector = ".product-price",
            Type = SelectorType.Css
        },
        new DataSelector {
            Name = "title",
            Selector = "h1.product-title",
            Type = SelectorType.Css
        }
    }
});

// Scrape using subagent selectors
var result = await _subagentObscura.ScrapeWithSubagentConfigAsync(
    subagentId: "price-scraper",
    url: "https://shop.example.com/product/123"
);

// Execute predefined task
var taskResult = await _subagentObscura.ExecuteBrowserTaskAsync(
    subagentId: "checkout-bot",
    taskName: "complete-purchase",
    parameters: new Dictionary<string, string> {
        ["url"] = "https://shop.example.com/checkout",
        ["email"] = "customer@example.com",
        ["card_number"] = "4111111111111111"
    }
);
```

## Subagent Configuration Example

```yaml
# subagent-price-monitor.yaml
subagent_id: price-monitor
name: Price Monitoring Agent
description: Monitors product prices on e-commerce sites

browser:
  enabled: true
  stealth_mode: true
  default_viewport: [1920, 1080]
  
  data_selectors:
    - name: product_name
      selector: "h1.product-title"
      type: css
      
    - name: current_price
      selector: ".price-current .amount"
      type: css
      
    - name: old_price
      selector: ".price-old .amount"
      type: css
      
    - name: availability
      selector: ".stock-status"
      type: css
      
  tasks:
    - name: check-price
      description: Check product price on specific URL
      actions:
        - type: navigate
          value: "{{url}}"
          
        - type: wait_for_element
          selector: ".product-details"
          wait_ms: 5000
          
        - type: screenshot
          
        - type: get_content
          
      extraction_rules:
        - field_name: price
          type: text
          selector: ".price-current .amount"
          required: true
          
        - field_name: currency
          type: attribute
          selector: ".price-current"
          attribute: "data-currency"
          default_value: "USD"
```

## Usage in Agents

### Direct Usage (in Agent Handler)
```csharp
public class ResearchAgentHandler : IRequestHandler<ResearchCommand, ResearchResult>
{
    private readonly IAgentObscuraTool _browserTool;
    
    public async Task<ResearchResult> Handle(ResearchCommand request, CancellationToken ct)
    {
        // Research across multiple sources
        var research = await _browserTool.ResearchAsync(
            request.Query,
            request.SourceUrls,
            new WebResearchOptions { MaxSources = 10 },
            ct
        );
        
        return new ResearchResult {
            Sources = research.Sources,
            Summary = GenerateSummary(research)
        };
    }
}
```

### Subagent Usage
```csharp
public class PriceMonitoringSubagent : ISubagent
{
    private readonly ISubagentObscuraIntegration _browserIntegration;
    
    public async Task<SubagentResult> ExecuteAsync(SubagentContext context)
    {
        var url = context.Parameters["product_url"];
        
        // Use pre-configured scraping
        var scrapeResult = await _browserIntegration.ScrapeWithSubagentConfigAsync(
            context.SubagentId,
            url
        );
        
        return new SubagentResult {
            Data = scrapeResult.Data,
            Success = true
        };
    }
}
```

## API Endpoints

### Browser Control
```
POST /api/ide/obscura/launch           - Launch browser instance
POST /api/ide/obscura/navigate          - Navigate to URL
POST /api/ide/obscura/screenshot        - Take screenshot
POST /api/ide/obscura/execute-js        - Execute JavaScript
POST /api/ide/obscura/content           - Get page content
POST /api/ide/obscura/close             - Close browser
```

### Agent Automation
```
POST /api/ide/agents/browser/research   - Web research
POST /api/ide/agents/browser/scrape     - Scrape URL
POST /api/ide/agents/browser/actions    - Perform actions
POST /api/ide/agents/browser/extract    - Extract data
```

## Configuration

### appsettings.json
```json
{
  "Obscura": {
    "BinaryPath": "/usr/local/bin/obscura",
    "DefaultPort": 9222,
    "StealthMode": true,
    "MaxConcurrentSessions": 10,
    "SessionTimeoutMinutes": 30
  }
}
```

### Docker Compose
```yaml
services:
  obscura:
    build:
      context: ./obscura
      dockerfile: Dockerfile
    ports:
      - "9222-9250:9222-9250"  # CDP ports
    environment:
      - OBSCURA_STEALTH=true
      - OBSCURA_BLOCK_TRACKERS=true
    networks:
      - libr4-network
```

## Security

- **Stealth mode**: Anti-detection built-in
- **Session isolation**: Each agent gets separate browser
- **Proxy support**: Route through proxies
- **User agent rotation**: Randomize fingerprints
- **Request limits**: Rate limiting per session

## Performance

| Metric | Obscura | Headless Chrome | Savings |
|--------|---------|-----------------|---------|
| RAM | 30 MB | 200 MB | 85% |
| Binary | 70 MB | 300 MB | 77% |
| Startup | Instant | ~2s | 100% |
| Page Load | 85ms | 500ms | 83% |

## Troubleshooting

### Browser not launching
- Check `Obscura:BinaryPath` in configuration
- Ensure binary has execute permissions
- Check logs: `_logger.LogError`

### CDP connection failed
- Verify port is not in use
- Check firewall rules
- Ensure Obscura process started

### Anti-bot detection
- Enable `StealthMode = true`
- Use proxy rotation
- Add delays between actions
- Randomize viewport sizes

## Integration Status

- ✅ IObscuraBrowserService - Process management + CDP
- ✅ IAgentObscuraTool - High-level agent automation
- ✅ ISubagentObscuraIntegration - Subagent configuration
- ✅ DI Registration - Singleton services
- ✅ API Endpoints - Full CRUD
- ✅ Config-based selectors - YAML/JSON support

**Ready for production use!**
