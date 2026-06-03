using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Detects when an IDE chat message should start autonomous app generation (not a short Q&amp;A).
/// </summary>
public static class AppGenerationChatIntentDetector
{
    private static readonly Regex GenerateVerb = new(
        @"\b(generate|build|create|scaffold|implement)\b|сгенерируй|создай|собери|разработай|напиши",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex AppNoun = new(
        @"\b(app|application|api|service|сервис|приложен|микросервис)\b|мобильн",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    public static bool IsAppGenerationRequest(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var trimmed = message.Trim();
        if (trimmed.Length < 12)
            return false;

        // Repo-bootstrap flows use dedicated triggers; still valid generation requests.
        if (trimmed.Contains("[[REPO_BOOTSTRAP_REQUIRED]]", StringComparison.OrdinalIgnoreCase))
            return true;

        return GenerateVerb.IsMatch(trimmed) && AppNoun.IsMatch(trimmed);
    }
}
