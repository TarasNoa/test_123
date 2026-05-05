using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Obscura;

/// <summary>
/// Converts HTML DOM to clean Markdown
/// Optimized for AI consumption - cheaper to analyze than raw HTML
/// </summary>
public interface IDomToMarkdownConverter
{
    /// <summary>
    /// Convert HTML to Markdown
    /// </summary>
    string Convert(string html, ConversionOptions? options = null);

    /// <summary>
    /// Extract main content from HTML (article, main, body)
    /// </summary>
    string ExtractMainContent(string html);

    /// <summary>
    /// Clean and normalize Markdown
    /// </summary>
    string Normalize(string markdown);
}

/// <summary>
/// Conversion options
/// </summary>
public class ConversionOptions
{
    /// <summary>
    /// Include links as [text](url)
    /// </summary>
    public bool IncludeLinks { get; set; } = true;

    /// <summary>
    /// Include images as ![alt](url)
    /// </summary>
    public bool IncludeImages { get; set; } = false;  // Usually not needed for text analysis

    /// <summary>
    /// Extract tables as Markdown tables
    /// </summary>
    public bool IncludeTables { get; set; } = true;

    /// <summary>
    /// Maximum depth for nested lists
    /// </summary>
    public int MaxListDepth { get; set; } = 6;

    /// <summary>
    /// Remove navigation, ads, sidebars (heuristic)
    /// </summary>
    public bool RemoveNoise { get; set; } = true;

    /// <summary>
    /// Maximum output length (truncate if longer)
    /// </summary>
    public int MaxLength { get; set; } = 100000;

    /// <summary>
    /// Include code blocks
    /// </summary>
    public bool IncludeCodeBlocks { get; set; } = true;
}

/// <summary>
/// DOM to Markdown converter implementation
/// </summary>
public class DomToMarkdownConverter : IDomToMarkdownConverter
{
    private static readonly Regex HtmlTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NavRegex = new Regex("<nav.*?</nav>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex HeaderRegex = new Regex("<header.*?</header>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex FooterRegex = new Regex("<footer.*?</footer>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex AsideRegex = new Regex("<aside.*?</aside>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ScriptStyleRegex = new Regex("<(script|style).*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public string Convert(string html, ConversionOptions? options = null)
    {
        options ??= new ConversionOptions();

        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Step 1: Remove noise if requested
        if (options.RemoveNoise)
        {
            html = RemoveNoiseElements(html);
        }

        // Step 2: Remove scripts and styles
        html = ScriptStyleRegex.Replace(html, "");

        // Step 3: Convert to Markdown
        var markdown = ConvertHtmlToMarkdown(html, options);

        // Step 4: Normalize
        markdown = Normalize(markdown);

        // Step 5: Truncate if too long
        if (markdown.Length > options.MaxLength)
        {
            markdown = markdown.Substring(0, options.MaxLength) + "\n\n... [truncated]";
        }

        return markdown;
    }

    public string ExtractMainContent(string html)
    {
        // Try to find main content areas
        var mainMatch = Regex.Match(html, "<main[^>]*>(.*?)</main>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (mainMatch.Success)
        {
            return mainMatch.Groups[1].Value;
        }

        var articleMatch = Regex.Match(html, "<article[^>]*>(.*?)</article>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (articleMatch.Success)
        {
            return articleMatch.Groups[1].Value;
        }

        var contentMatch = Regex.Match(html, "<div[^>]*class=['\"][^'\"]*content[^'\"]*['\"][^>]*>(.*?)</div>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (contentMatch.Success)
        {
            return contentMatch.Groups[1].Value;
        }

        // Fallback: return body
        var bodyMatch = Regex.Match(html, "<body[^>]*>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (bodyMatch.Success)
        {
            return bodyMatch.Groups[1].Value;
        }

        return html;
    }

    public string Normalize(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Fix multiple blank lines
        markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");

        // Fix spaces at line ends
        markdown = Regex.Replace(markdown, "[ ]+\n", "\n");

        // Fix spaces at line starts
        markdown = Regex.Replace(markdown, "\n[ ]+", "\n");

        // Ensure single space after list markers
        markdown = Regex.Replace(markdown, "^([*-])[ ]*", "$1 ", RegexOptions.Multiline);

        // Trim
        return markdown.Trim();
    }

    private string RemoveNoiseElements(string html)
    {
        // Remove common noise elements
        html = NavRegex.Replace(html, "");
        html = HeaderRegex.Replace(html, "");
        html = FooterRegex.Replace(html, "");
        html = AsideRegex.Replace(html, "");

        // Remove elements with common ad/noise classes
        var noiseClasses = new[] { "ad", "ads", "advertisement", "sidebar", "widget", "social", "share" };
        foreach (var cls in noiseClasses)
        {
            var pattern = $"<[^>]*class=['\"][^'\"]*{cls}[^'\"]*['\"][^>]*>.*?</[^>]+>";
            html = Regex.Replace(html, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        return html;
    }

    private string ConvertHtmlToMarkdown(string html, ConversionOptions options)
    {
        var sb = new StringBuilder();
        var lines = html.Split('\n');
        var codeLanguage = "";

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                sb.AppendLine();
                continue;
            }

            // Headers
            if (trimmed.StartsWith("<h1", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"# {text}");
            }
            else if (trimmed.StartsWith("<h2", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"## {text}");
            }
            else if (trimmed.StartsWith("<h3", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"### {text}");
            }
            else if (trimmed.StartsWith("<h4", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"#### {text}");
            }
            else if (trimmed.StartsWith("<h5", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"##### {text}");
            }
            else if (trimmed.StartsWith("<h6", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"###### {text}");
            }

            // Code blocks
            else if (options.IncludeCodeBlocks && trimmed.StartsWith("<pre", StringComparison.OrdinalIgnoreCase))
            {
                var codeContent = ExtractCodeContent(line);
                sb.AppendLine($"```{codeLanguage}");
                sb.AppendLine(codeContent);
                sb.AppendLine("```");
            }
            else if (options.IncludeCodeBlocks && trimmed.StartsWith("<code", StringComparison.OrdinalIgnoreCase))
            {
                var code = ExtractText(RemoveHtmlTags(line));
                sb.AppendLine($"`{code}`");
            }

            // Lists
            else if (trimmed.StartsWith("<ul", StringComparison.OrdinalIgnoreCase))
            {
                // Unordered list - items processed separately
            }
            else if (trimmed.StartsWith("<ol", StringComparison.OrdinalIgnoreCase))
            {
                // Ordered list - items processed separately
            }
            else if (trimmed.StartsWith("<li", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                // Detect if inside ol or ul
                var isOrdered = IsInsideOrderedList(html, i);
                var marker = isOrdered ? "1." : "-";
                sb.AppendLine($"{marker} {text}");
            }

            // Links
            else if (options.IncludeLinks && trimmed.Contains("<a ", StringComparison.OrdinalIgnoreCase))
            {
                var linkText = ExtractText(RemoveHtmlTags(line));
                var href = ExtractHref(line);
                if (!string.IsNullOrEmpty(href))
                {
                    sb.AppendLine($"[{linkText}]({href})");
                }
                else
                {
                    sb.AppendLine(linkText);
                }
            }

            // Images
            else if (options.IncludeImages && trimmed.StartsWith("<img", StringComparison.OrdinalIgnoreCase))
            {
                var alt = ExtractAlt(line);
                var src = ExtractSrc(line);
                sb.AppendLine($"![{alt}]({src})");
            }

            // Tables
            else if (options.IncludeTables && trimmed.StartsWith("<table", StringComparison.OrdinalIgnoreCase))
            {
                var tableMarkdown = ConvertTableToMarkdown(line);
                sb.AppendLine(tableMarkdown);
            }

            // Paragraphs and other text
            else if (trimmed.StartsWith("<p", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("<div", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("<span", StringComparison.OrdinalIgnoreCase))
            {
                var text = ExtractText(RemoveHtmlTags(line));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
            else
            {
                // Plain text or unknown tag - try to extract text
                var text = ExtractText(RemoveHtmlTags(line));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }
        }

        return sb.ToString();
    }

    private string RemoveHtmlTags(string html)
    {
        return HtmlTagRegex.Replace(html, "");
    }

    private string ExtractText(string html)
    {
        // Decode HTML entities
        var text = html
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&nbsp;", " ");

        // Normalize whitespace
        text = WhitespaceRegex.Replace(text, " ");
        return text.Trim();
    }

    private string ExtractCodeContent(string html)
    {
        // Extract content from <pre><code>...</code></pre>
        var codeMatch = Regex.Match(html, "<code[^>]*>(.*?)</code>", RegexOptions.Singleline);
        if (codeMatch.Success)
        {
            return codeMatch.Groups[1].Value;
        }

        var preMatch = Regex.Match(html, "<pre[^>]*>(.*?)</pre>", RegexOptions.Singleline);
        if (preMatch.Success)
        {
            return preMatch.Groups[1].Value;
        }

        return ExtractText(html);
    }

    private string ExtractHref(string html)
    {
        var match = Regex.Match(html, "href=['\"]([^'\"]+)['\"]");
        return match.Success ? match.Groups[1].Value : "";
    }

    private string ExtractAlt(string html)
    {
        var match = Regex.Match(html, "alt=['\"]([^'\"]*)['\"]");
        return match.Success ? match.Groups[1].Value : "image";
    }

    private string ExtractSrc(string html)
    {
        var match = Regex.Match(html, "src=['\"]([^'\"]+)['\"]");
        return match.Success ? match.Groups[1].Value : "";
    }

    private bool IsInsideOrderedList(string html, int currentIndex)
    {
        // Simple heuristic: look backwards for <ol>
        var searchBack = Math.Max(0, currentIndex - 50);
        var context = string.Join("\n", html.Split('\n').Skip(searchBack).Take(currentIndex - searchBack));
        return context.Contains("<ol", StringComparison.OrdinalIgnoreCase);
    }

    private string ConvertTableToMarkdown(string html)
    {
        var sb = new StringBuilder();

        // Extract headers
        var headers = new List<string>();
        var headerMatches = Regex.Matches(html, "<th[^>]*>(.*?)</th>", RegexOptions.Singleline);
        foreach (Match match in headerMatches)
        {
            headers.Add(ExtractText(match.Groups[1].Value));
        }

        // Extract rows
        var rows = new List<List<string>>();
        var rowMatches = Regex.Matches(html, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline);
        foreach (Match rowMatch in rowMatches)
        {
            var cells = new List<string>();
            var cellMatches = Regex.Matches(rowMatch.Groups[1].Value, "<td[^>]*>(.*?)</td>", RegexOptions.Singleline);
            foreach (Match cellMatch in cellMatches)
            {
                cells.Add(ExtractText(cellMatch.Groups[1].Value));
            }
            if (cells.Any())
            {
                rows.Add(cells);
            }
        }

        // If no headers but have rows, use first row as headers
        if (!headers.Any() && rows.Any())
        {
            headers = rows[0];
            rows = rows.Skip(1).ToList();
        }

        if (!headers.Any())
        {
            return ""; // No valid table
        }

        // Build Markdown table
        sb.AppendLine("| " + string.Join(" | ", headers) + " |");
        sb.AppendLine("|" + string.Join("|", headers.Select(h => " --- ")) + "|");

        foreach (var row in rows)
        {
            var paddedRow = new List<string>();
            for (int i = 0; i < headers.Count; i++)
            {
                paddedRow.Add(i < row.Count ? row[i] : "");
            }
            sb.AppendLine("| " + string.Join(" | ", paddedRow) + " |");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Obscura agent tool with Markdown conversion
/// </summary>
public class ObscuraMarkdownTool : IAgentObscuraTool
{
    private readonly IAgentObscuraTool _baseTool;
    private readonly IDomToMarkdownConverter _markdownConverter;
    private readonly ILogger<ObscuraMarkdownTool> _logger;

    public ObscuraMarkdownTool(
        IAgentObscuraTool baseTool,
        IDomToMarkdownConverter markdownConverter,
        ILogger<ObscuraMarkdownTool> logger)
    {
        _baseTool = baseTool;
        _markdownConverter = markdownConverter;
        _logger = logger;
    }

    public async Task<WebResearchResult> ResearchAsync(string query, string[] sources, WebResearchOptions? options = null, CancellationToken ct = default)
    {
        var result = await _baseTool.ResearchAsync(query, sources, options, ct);

        // Convert HTML content to Markdown
        foreach (var source in result.Sources)
        {
            if (!string.IsNullOrEmpty(source.HtmlContent))
            {
                var markdown = _markdownConverter.Convert(source.HtmlContent, new ConversionOptions
                {
                    RemoveNoise = true,
                    IncludeLinks = true,
                    MaxLength = 50000  // Limit for AI
                });

                source.Content = markdown;
                _logger.LogDebug(
                    "Converted {OriginalLength} chars HTML to {MarkdownLength} chars Markdown for {Url}",
                    source.HtmlContent.Length, markdown.Length, source.Url);
            }
        }

        return result;
    }

    public Task<ScrapeResult> ScrapeAsync(string url, ScrapeOptions? options = null, CancellationToken ct = default)
        => _baseTool.ScrapeAsync(url, options, ct);

    public Task<ActionResult> PerformActionsAsync(string startUrl, BrowserAction[] actions, ActionOptions? options = null, CancellationToken ct = default)
        => _baseTool.PerformActionsAsync(startUrl, actions, options, ct);

    public Task<ScreenshotResult> ScreenshotAsync(string url, ScreenshotOptions? options = null, CancellationToken ct = default)
        => _baseTool.ScreenshotAsync(url, options, ct);

    public async Task<ExtractionResult> ExtractAsync(string url, string[] extractionScripts, ExtractionOptions? options = null, CancellationToken ct = default)
    {
        var result = await _baseTool.ExtractAsync(url, extractionScripts, options, ct);

        // Convert extracted HTML to Markdown if present
        foreach (var key in result.ExtractedData.Keys.ToList())
        {
            var value = result.ExtractedData[key];
            if (value is string str && str.Contains('<') && str.Contains('>'))
            {
                var markdown = _markdownConverter.Convert(str, new ConversionOptions
                {
                    RemoveNoise = true,
                    MaxLength = 10000
                });
                result.ExtractedData[key] = markdown;
            }
        }

        return result;
    }

    public Task CloseAllSessionsAsync() => _baseTool.CloseAllSessionsAsync();
}
