using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public static class RepoBootstrapDetailsParser
{
    public static bool TryParse(string? details, out RepoBootstrapProbe probe)
    {
        probe = default;
        if (string.IsNullOrWhiteSpace(details))
            return false;

        var jsonStart = details.IndexOf('{');
        if (jsonStart < 0)
            return false;

        var json = details[jsonStart..];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var cloneUrl = FirstNonEmpty(
                root, "clone_url", "cloneUrl", "git_clone_url", "repository_url");
            if (string.IsNullOrWhiteSpace(cloneUrl) &&
                root.TryGetProperty("git_clone", out var gitClone) &&
                gitClone.ValueKind == JsonValueKind.String)
            {
                var raw = gitClone.GetString() ?? string.Empty;
                const string prefix = "git clone ";
                if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    cloneUrl = raw[prefix.Length..].Trim().Trim('"');
            }

            var repoUrl = FirstNonEmpty(root, "repo_url", "repoUrl", "html_url");
            var repository = FirstNonEmpty(root, "repository", "full_name");
            var license = FirstNonEmpty(root, "license", "spdx_id");

            if (string.IsNullOrWhiteSpace(cloneUrl) && !string.IsNullOrWhiteSpace(repoUrl))
                cloneUrl = repoUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
                    ? repoUrl
                    : repoUrl.TrimEnd('/') + ".git";

            if (string.IsNullOrWhiteSpace(cloneUrl))
                return false;

            probe = new RepoBootstrapProbe(cloneUrl, repoUrl, repository, license);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? FirstNonEmpty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var prop))
                continue;
            if (prop.ValueKind != JsonValueKind.String)
                continue;
            var value = prop.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}

public readonly record struct RepoBootstrapProbe(
    string CloneUrl,
    string? RepoUrl,
    string? Repository,
    string? License);
