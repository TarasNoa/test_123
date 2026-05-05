/*
using MediatR;
using Libr4.IDE.Application.CodeIntelligence.Commands;
using Libr4.IDE.Application.CodeIntelligence.DTOs;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.CodeIntelligence.Handlers;

/// <summary>
/// Handler for GetCompletionsCommand - AI-powered code completions
/// </summary>
public class GetCompletionsCommandHandler : IRequestHandler<GetCompletionsCommand, CodeIntelligenceDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<GetCompletionsCommandHandler> _logger;

    public GetCompletionsCommandHandler(IAIService aiService, ILogger<GetCompletionsCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<CodeIntelligenceDto> Handle(GetCompletionsCommand request, CancellationToken ct)
    {
        _logger.LogDebug("Getting completions for {FilePath} at {Line}:{Column}",
            request.FilePath, request.Line, request.Column);

        var completions = new List<CompletionItemDto>();

        // Extract context before cursor
        var lines = request.Code.Split('\n');
        var currentLine = request.Line < lines.Length ? lines[request.Line] : "";
        var beforeCursor = currentLine.Substring(0, Math.Min(request.Column, currentLine.Length));

        // Pattern-based completions (fast)
        completions.AddRange(GetPatternBasedCompletions(beforeCursor, request.FilePath));

        // AI-powered completions
        var aiCompletions = await GetAICompletionsAsync(request, ct);
        completions.AddRange(aiCompletions);

        return new CodeIntelligenceDto
        {
            Completions = completions.Take(10).ToList(),
            RequestId = Guid.NewGuid().ToString(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private List<CompletionItemDto> GetPatternBasedCompletions(string context, string filePath)
    {
        var completions = new List<CompletionItemDto>();
        var language = GetLanguageFromExtension(filePath);

        // Common patterns by language
        if (language == "csharp")
        {
            if (context.EndsWith("pub"))
            {
                completions.Add(new CompletionItemDto { Label = "public", Kind = "Keyword", Detail = "Access modifier" });
                completions.Add(new CompletionItemDto { Label = "public static", Kind = "Keyword", Detail = "Static modifier" });
            }
            if (context.EndsWith("cla"))
            {
                completions.Add(new CompletionItemDto { Label = "class", Kind = "Keyword", Detail = "Class declaration" });
            }
            if (context.EndsWith("ret"))
            {
                completions.Add(new CompletionItemDto { Label = "return", Kind = "Keyword", Detail = "Return statement" });
            }
            if (context.EndsWith("asy"))
            {
                completions.Add(new CompletionItemDto { Label = "async", Kind = "Keyword", Detail = "Async modifier" });
                completions.Add(new CompletionItemDto { Label = "await", Kind = "Keyword", Detail = "Await operator" });
            }
        }
        else if (language == "typescript" || language == "javascript")
        {
            if (context.EndsWith("con"))
            {
                completions.Add(new CompletionItemDto { Label = "const", Kind = "Keyword", Detail = "Constant declaration" });
                completions.Add(new CompletionItemDto { Label = "console.log", Kind = "Function", Detail = "Log to console" });
            }
            if (context.EndsWith("fun"))
            {
                completions.Add(new CompletionItemDto { Label = "function", Kind = "Keyword", Detail = "Function declaration" });
            }
            if (context.EndsWith("imp"))
            {
                completions.Add(new CompletionItemDto { Label = "import", Kind = "Keyword", Detail = "Import statement" });
            }
        }

        return completions;
    }

    private async Task<List<CompletionItemDto>> GetAICompletionsAsync(GetCompletionsCommand request, CancellationToken ct)
    {
        var completions = new List<CompletionItemDto>();

        try
        {
            var lines = request.Code.Split('\n');
            var startLine = Math.Max(0, request.Line - 10);
            var contextLines = lines.Skip(startLine).Take(20);
            var context = string.Join("\n", contextLines);

            var prompt = $@"
File: {request.FilePath}
Line {request.Line}, Column {request.Column}

Context:
```
{context}
```

Suggest 3 code completions for this context.
Format: label|kind|description
Example: GetUserById|Method|Retrieves user by ID";

            var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);

            foreach (var line in response.Split('\n').Where(l => l.Contains('|')))
            {
                var parts = line.Split('|');
                if (parts.Length >= 2)
                {
                    completions.Add(new CompletionItemDto
                    {
                        Label = parts[0].Trim(),
                        Kind = parts[1].Trim(),
                        Detail = parts.Length > 2 ? parts[2].Trim() : "AI Suggestion",
                        Source = "AI"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI completions failed");
        }

        return completions;
    }

    private string GetLanguageFromExtension(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".cs" => "csharp",
            ".ts" => "typescript",
            ".tsx" => "typescript",
            ".js" => "javascript",
            ".jsx" => "javascript",
            ".py" => "python",
            ".rs" => "rust",
            ".go" => "go",
            ".java" => "java",
            _ => "unknown"
        };
    }
}
*/
