using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Groups manifest paths by feature cohesion (Django app API, Spring vertical slice, etc.)
/// instead of only parent-folder proximity.
/// </summary>
public static partial class FeatureDependencyGrouper
{
    public static string? TryResolveFeatureBucket(string relativePath)
    {
        var normalized = StackArtifactCompleteness.SanitizeRelativePath(relativePath);
        if (normalized.Length == 0)
            return null;

        if (ShouldStayOutsideFeatureBatch(normalized))
            return null;

        var django = DjangoAppApiPath().Match(normalized);
        if (django.Success)
            return $"feature:django-app:{django.Groups[1].Value}";

        var djangoService = DjangoAppServicePath().Match(normalized);
        if (djangoService.Success)
            return $"feature:django-app:{djangoService.Groups[1].Value}";

        var fastApi = FastApiFeaturePath().Match(normalized);
        if (fastApi.Success)
            return $"feature:fastapi:{fastApi.Groups[1].Value}";

        var spring = SpringVerticalSlicePath().Match(normalized);
        if (spring.Success)
            return $"feature:spring:{spring.Groups[1].Value}";

        return null;
    }

    public static IReadOnlyList<string> RefineBucketOrder(
        IReadOnlyList<string> pathsInBucket,
        IRepoGraphBuilder graphBuilder,
        IReadOnlyDictionary<string, string>? contentsByPath = null) =>
        graphBuilder.OrderForGeneration(pathsInBucket, contentsByPath);

    public static bool ShouldStayOutsideFeatureBatch(string normalizedPath)
    {
        if (normalizedPath.EndsWith("/apps.py", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith("/__init__.py", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Contains("/migrations/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedPath.EndsWith("manage.py", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith("settings.py", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith("wsgi.py", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith("asgi.py", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith("urls.py", StringComparison.OrdinalIgnoreCase)
               && normalizedPath.Contains('/', StringComparison.Ordinal)
               && !normalizedPath.Contains("/meals/", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    [GeneratedRegex(
        @"^backend/([^/]+)/(models|serializers|views|urls|tests|exceptions)\.py$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DjangoAppApiPath();

    [GeneratedRegex(
        @"^backend/([^/]+)/services/[^/]+\.py$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DjangoAppServicePath();

    [GeneratedRegex(
        @"^backend/app/(routers|services|models)/[^/]+\.py$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FastApiFeaturePath();

    [GeneratedRegex(
        @"^backend/src/main/java/([^/]+/[^/]+)/(?:web|service|repository|model)/[^/]+\.java$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpringVerticalSlicePath();
}
