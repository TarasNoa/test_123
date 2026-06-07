using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic security hardening for Java/Spring backends before LLM security remediation.
/// </summary>
public static class JavaSpringSecurityRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan)
    {
        if (!JavaMonorepoPaths.IsJavaReactPlan(plan))
            return 0;

        var changed = 0;
        changed += ExternalizeJwtSecrets(files);
        changed += RestrictH2ConsoleInProd(files);
        return changed;
    }

    private static int ExternalizeJwtSecrets(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var path = files[i].RelativePath.Replace('\\', '/');
            if (!path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!path.Contains("application", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".properties", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            if (content.Contains("${APP_JWT_SECRET", StringComparison.Ordinal)
                || content.Contains("${JWT_SECRET", StringComparison.Ordinal))
                continue;

            var updated = Regex.Replace(
                content,
                @"jwt\.secret\s*[:=]\s*[^\s#\r\n]+",
                "jwt.secret: ${APP_JWT_SECRET:}",
                RegexOptions.IgnoreCase);
            updated = Regex.Replace(
                updated,
                @"spring\.security\.jwt\.secret\s*[:=]\s*[^\s#\r\n]+",
                "spring.security.jwt.secret: ${APP_JWT_SECRET:}",
                RegexOptions.IgnoreCase);

            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }

    private static int RestrictH2ConsoleInProd(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var path = files[i].RelativePath.Replace('\\', '/');
            if (!path.Contains("application", StringComparison.OrdinalIgnoreCase))
                continue;
            if (path.Contains("prod", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            if (!content.Contains("h2.console", StringComparison.OrdinalIgnoreCase)
                && !content.Contains("console.enabled", StringComparison.OrdinalIgnoreCase))
                continue;

            if (content.Contains("on-profile: prod", StringComparison.OrdinalIgnoreCase)
                && content.Contains("h2.console.enabled: false", StringComparison.OrdinalIgnoreCase))
                continue;

            var updated = content.TrimEnd() + """

                ---
                spring.config.activate.on-profile: prod
                spring.h2.console.enabled: false
                """;
            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }
}
