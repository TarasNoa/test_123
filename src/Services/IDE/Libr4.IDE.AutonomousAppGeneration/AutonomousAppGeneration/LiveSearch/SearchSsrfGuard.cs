using System.Net;
using System.Net.Sockets;

namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public static class SearchSsrfGuard
{
    private static readonly HashSet<string> BlockedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "0.0.0.0",
        "::1",
        "host.docker.internal",
        "metadata.google.internal",
        "169.254.169.254"
    };

    public static bool IsQuerySafe(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        foreach (Uri? uri in ExtractUrls(query))
        {
            if (uri is not null && IsBlockedTarget(uri))
                return false;
        }

        return !ContainsBlockedHostToken(query);
    }

    public static bool IsBlockedTarget(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            return true;

        if (uri.Scheme is not "http" and not "https")
            return true;

        var host = uri.IdnHost;
        if (BlockedHosts.Contains(host))
            return true;

        if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
            return true;

        if (IPAddress.TryParse(host, out var ip))
            return IsPrivateOrLoopback(ip);

        return false;
    }

    public static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] is >= 16 and <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
            return true;

        return false;
    }

    private static IEnumerable<Uri?> ExtractUrls(string text)
    {
        foreach (var token in text.Split([' ', '\t', '\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || token.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                yield return Uri.TryCreate(token.TrimEnd('.', ',', ';', ')'), UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    private static bool ContainsBlockedHostToken(string query) =>
        BlockedHosts.Any(host => query.Contains(host, StringComparison.OrdinalIgnoreCase));
}

public static class SearchContentTruncator
{
    public static LiveSearchResponse Truncate(LiveSearchResponse response, int maxSnippetChars, int maxResponseChars)
    {
        var hits = response.Hits
            .Select(hit => hit with
            {
                Snippet = TruncateText(hit.Snippet, maxSnippetChars),
                Title = TruncateText(hit.Title, maxSnippetChars),
                Url = TruncateText(hit.Url, 2048)
            })
            .ToList();

        var serializedLength = EstimateLength(response.Query, hits);
        if (serializedLength <= maxResponseChars)
            return response with { Hits = hits };

        var reduced = new List<LiveSearchHit>();
        foreach (var hit in hits)
        {
            reduced.Add(hit);
            if (EstimateLength(response.Query, reduced) > maxResponseChars)
            {
                reduced.RemoveAt(reduced.Count - 1);
                break;
            }
        }

        return response with
        {
            Hits = reduced,
            TruncationNotice = $"response_truncated_to_{maxResponseChars}_chars"
        };
    }

    private static string TruncateText(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars] + "…";

    private static int EstimateLength(string query, IReadOnlyList<LiveSearchHit> hits) =>
        query.Length + hits.Sum(h => h.Title.Length + h.Url.Length + h.Snippet.Length + 32);
}
