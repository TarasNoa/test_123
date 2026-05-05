namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Shared path checks for quality gates and stack safety-net (normalize slashes, common test layouts).
/// </summary>
internal static class GenerationPathHeuristics
{
    public static string NormalizeSlashes(string relativePath) =>
        relativePath.Replace('\\', '/');

    public static bool LooksLikeDotNetTestPath(string relativePath)
    {
        var n = NormalizeSlashes(relativePath);
        var fn = Path.GetFileName(n);
        return n.Contains("Tests", StringComparison.OrdinalIgnoreCase)
               || n.Contains("/test/", StringComparison.OrdinalIgnoreCase)
               || fn.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase)
               || (fn.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase)
                   && !fn.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase));
    }

    public static bool LooksLikePythonTestPath(string relativePath)
    {
        var n = NormalizeSlashes(relativePath);
        return n.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
           || n.Contains("/test/", StringComparison.OrdinalIgnoreCase)
           || n.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
           || n.StartsWith("test/", StringComparison.OrdinalIgnoreCase)
           || n.EndsWith("_test.py", StringComparison.OrdinalIgnoreCase)
           || n.EndsWith("test.py", StringComparison.OrdinalIgnoreCase);
    }

    public static bool LooksLikeNodeTestPath(string relativePath)
    {
        var n = NormalizeSlashes(relativePath);
        return n.Contains("/test/", StringComparison.OrdinalIgnoreCase)
           || n.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
           || n.Contains("/__tests__/", StringComparison.OrdinalIgnoreCase)
           || n.StartsWith("test/", StringComparison.OrdinalIgnoreCase)
           || n.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
           || n.StartsWith("__tests__/", StringComparison.OrdinalIgnoreCase)
           || n.EndsWith(".test.js", StringComparison.OrdinalIgnoreCase)
           || n.EndsWith(".spec.js", StringComparison.OrdinalIgnoreCase)
           || n.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase)
           || n.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase);
    }
}
