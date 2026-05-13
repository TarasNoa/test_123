using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Translation;

/// <summary>
/// Translation service using OpenAI API
/// </summary>
public sealed class OpenAITranslationService : ITranslationService
{
    private readonly IAIService _ai;
    private readonly ILogger<OpenAITranslationService> _logger;

    public OpenAITranslationService(IAIService ai, ILogger<OpenAITranslationService> logger)
    {
        _ai = ai;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var result = await TranslateBatchAsync(new[] { text }, targetLanguage, ct);
        return result[0];
    }

    public async Task<string[]> TranslateBatchAsync(string[] texts, string targetLanguage, CancellationToken ct = default)
    {
        if (texts.Length == 0)
            return Array.Empty<string>();

        // Build translation prompt
        var prompt = BuildTranslationPrompt(texts, targetLanguage);

        try
        {
            var response = await _ai.GenerateCompletionAsync(prompt, null, null);
            
            // Parse the response to extract translations
            var translations = ParseTranslationResponse(response, texts.Length);
            
            _logger.LogInformation(
                "Translated {Count} texts to {Language}",
                texts.Length,
                targetLanguage);

            return translations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed for {Count} texts to {Language}", texts.Length, targetLanguage);
            
            // Fallback: return original texts
            return texts;
        }
    }

    private string BuildTranslationPrompt(string[] texts, string targetLanguage)
    {
        var languageName = GetLanguageName(targetLanguage);
        
        var prompt = $@"Translate the following texts to {languageName}. Return ONLY a JSON array with the translated texts, in the same order. No prose, no markdown fences.

Texts to translate:
{string.Join("\n", texts.Select((t, i) => $"{i + 1}. {t}"))}

Expected output format:
[""translated text 1"", ""translated text 2"", ...]";

        return prompt;
    }

    private string[] ParseTranslationResponse(string response, int expectedCount)
    {
        try
        {
            // Try to parse as JSON array
            var startIndex = response.IndexOf('[');
            var endIndex = response.LastIndexOf(']');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                var jsonPart = response.Substring(startIndex, endIndex - startIndex + 1);
                var translations = System.Text.Json.JsonSerializer.Deserialize<string[]>(jsonPart);
                
                if (translations != null && translations.Length == expectedCount)
                    return translations;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "JSON parsing failed, falling back to line-based parsing");
        }

        // Fallback: try to parse line by line
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Skip numbered lines
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                result.Add(System.Text.RegularExpressions.Regex.Replace(trimmed, @"^\d+\.\s", ""));
            }
            else if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            {
                result.Add(trimmed.Trim('"'));
            }
            else if (!trimmed.StartsWith('{') && !trimmed.StartsWith('[') && !trimmed.StartsWith('"'))
            {
                result.Add(trimmed);
            }
        }

        // If we got the right number of translations, use them
        if (result.Count == expectedCount)
            return result.ToArray();

        // Last resort: return empty array (caller will fallback to originals)
        return Array.Empty<string>();
    }

    private static string GetLanguageName(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "ru" or "ru-ru" => "Russian",
            "en" or "en-us" => "English",
            "de" or "de-de" => "German",
            "fr" or "fr-fr" => "French",
            "es" or "es-es" => "Spanish",
            "it" or "it-it" => "Italian",
            "pt" or "pt-pt" => "Portuguese",
            "ja" or "ja-jp" => "Japanese",
            "ko" or "ko-kr" => "Korean",
            "zh" or "zh-cn" => "Chinese (Simplified)",
            "zh-tw" => "Chinese (Traditional)",
            "ar" or "ar-sa" => "Arabic",
            "hi" or "hi-in" => "Hindi",
            _ => code // Return the code itself if unknown
        };
    }
}
