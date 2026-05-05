using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Utility for extracting <thinking> sections from LLM responses
/// </summary>
public static class ThinkingExtractor
{
    private static readonly Regex ThinkingRegex = new(
        @"<thinking>(.*?)</thinking>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Extracts the thinking section and content from an LLM response
    /// </summary>
    /// <param name="response">The raw LLM response</param>
    /// <returns>A tuple of (thinking, content) where thinking may be null if not found</returns>
    public static (string? thinking, string content) ExtractThinking(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return (null, response);

        var match = ThinkingRegex.Match(response);
        if (match.Success)
        {
            var thinking = match.Groups[1].Value.Trim();
            var content = response.Replace(match.Value, "").Trim();
            return (thinking, content);
        }

        return (null, response);
    }

    /// <summary>
    /// Checks if a response contains a thinking section
    /// </summary>
    public static bool HasThinking(string response)
    {
        return !string.IsNullOrWhiteSpace(response) && ThinkingRegex.IsMatch(response);
    }
}
