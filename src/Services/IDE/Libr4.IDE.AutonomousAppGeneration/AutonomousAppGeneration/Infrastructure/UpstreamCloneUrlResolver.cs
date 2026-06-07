using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>Extracts git clone URLs from free-form user requests for cascade codebase prefetch.</summary>
public static partial class UpstreamCloneUrlResolver
{
    [GeneratedRegex(@"github\.com/([^/\s]+)/([^/\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GitHubRepoPattern();

    public static IReadOnlyList<string> ExtractCloneUrls(string text, int max = 2)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        void Add(string? cloneUrl)
        {
            if (string.IsNullOrWhiteSpace(cloneUrl))
                return;
            if (seen.Add(cloneUrl))
                result.Add(cloneUrl);
        }

        foreach (var url in BrowserUrlClassifier.ExtractHttpUrls(text, max * 3))
        {
            Add(NormalizeHttpUrlToCloneUrl(url));
            if (result.Count >= max)
                return result;
        }

        foreach (Match match in GitHubRepoPattern().Matches(text))
        {
            var owner = match.Groups[1].Value.Trim().TrimEnd('.');
            var repo = match.Groups[2].Value.Trim().TrimEnd('.', ',', ';', ')');
            if (repo.Contains('/'))
                repo = repo.Split('/')[0];
            Add($"https://github.com/{owner}/{repo}.git");
            if (result.Count >= max)
                return result;
        }

        return result;
    }

    internal static string? NormalizeHttpUrlToCloneUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host;
        if (host.Contains("github.com", StringComparison.OrdinalIgnoreCase)
            || host.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase)
            || host.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                return null;

            var owner = segments[0];
            var repo = segments[1].Replace(".git", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (repo is "tree" or "blob" or "pull" or "issues")
                return null;

            return $"https://{host}/{owner}/{repo}.git";
        }

        return url.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? url : null;
    }
}
