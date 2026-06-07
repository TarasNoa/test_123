using System.Text.Json;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;

/// <summary>Classifies browser targets for stealth mode and cascade prefetch URL discovery.</summary>
public static partial class BrowserUrlClassifier
{
    private static readonly string[] LocalHostMarkers =
    [
        "localhost",
        "127.0.0.1",
        "0.0.0.0",
        "[::1]",
        "host.docker.internal"
    ];

    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HttpUrlPattern();

    public static bool IsLocalOrInternalHost(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host;
        if (LocalHostMarkers.Any(m => host.Contains(m, StringComparison.OrdinalIgnoreCase)))
            return true;

        return host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
    }

    public static bool RequiresStealthMode(IEnumerable<string> urls) =>
        urls.Any(u => !IsLocalOrInternalHost(u));

    public static bool RequiresStealthMode(string url) =>
        !IsLocalOrInternalHost(url);

    public static bool ResolveStealthMode(JsonElement input, IReadOnlyList<string> sources)
    {
        if (input.TryGetProperty("stealth_mode", out var el))
        {
            return el.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(el.GetString(), out var b) && b,
                _ => RequiresStealthMode(sources)
            };
        }

        return RequiresStealthMode(sources);
    }

    public static IReadOnlyList<string> ExtractHttpUrls(string text, int max = 5)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        return HttpUrlPattern()
            .Matches(text)
            .Select(m => m.Value.TrimEnd(',', ';', ')', ']', '"', '\''))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, max))
            .ToList();
    }
}
