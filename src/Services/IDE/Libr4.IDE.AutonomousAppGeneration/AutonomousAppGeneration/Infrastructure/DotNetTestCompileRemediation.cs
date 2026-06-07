using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Ensures test .csproj files reference the main web/app project when CS0246 appears in tests.
/// </summary>
public static class DotNetTestCompileRemediation
{
    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog)
    {
        if (string.IsNullOrWhiteSpace(buildLog)
            || !buildLog.Contains("CS0246", StringComparison.OrdinalIgnoreCase))
            return 0;

        var testCsproj = files
            .FirstOrDefault(f => f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                 && (f.RelativePath.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
                                     || f.RelativePath.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase)
                                     || f.RelativePath.Contains(".Tests.", StringComparison.OrdinalIgnoreCase)));
        var mainCsproj = files
            .FirstOrDefault(f => f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                 && !f.RelativePath.Contains("test", StringComparison.OrdinalIgnoreCase)
                                 && (f.Content?.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) == true
                                     || f.RelativePath.Contains("/src/", StringComparison.OrdinalIgnoreCase)));

        if (testCsproj is null || mainCsproj is null)
            return 0;

        var testContent = testCsproj.Content ?? string.Empty;
        var mainName = Path.GetFileNameWithoutExtension(mainCsproj.RelativePath);
        if (testContent.Contains($"Include=\"{mainName}\"", StringComparison.OrdinalIgnoreCase)
            || testContent.Contains($"Include=\"..\\", StringComparison.OrdinalIgnoreCase)
            && testContent.Contains($"{mainName}.csproj", StringComparison.OrdinalIgnoreCase))
            return 0;

        var relRef = ComputeRelativeProjectReference(testCsproj.RelativePath, mainCsproj.RelativePath);
        if (string.IsNullOrWhiteSpace(relRef))
            return 0;

        var insert = $"  <ItemGroup>\n    <ProjectReference Include=\"{relRef}\" />\n  </ItemGroup>\n";
        var updated = testContent.Contains("</Project>", StringComparison.OrdinalIgnoreCase)
            ? testContent.Replace("</Project>", insert + "</Project>", StringComparison.OrdinalIgnoreCase)
            : testContent + insert;

        var idx = files.IndexOf(testCsproj);
        files[idx] = new GeneratedFile(testCsproj.RelativePath, testCsproj.Language, updated);
        return 1;
    }

    private static string ComputeRelativeProjectReference(string testCsprojPath, string mainCsprojPath)
    {
        var testDir = Path.GetDirectoryName(testCsprojPath.Replace('\\', '/')) ?? string.Empty;
        var mainPath = mainCsprojPath.Replace('\\', '/');
        var mainFile = Path.GetFileName(mainPath);
        var mainDir = Path.GetDirectoryName(mainPath) ?? string.Empty;

        var testParts = testDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var mainParts = mainDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var common = 0;
        while (common < testParts.Length && common < mainParts.Length
               && string.Equals(testParts[common], mainParts[common], StringComparison.OrdinalIgnoreCase))
            common++;

        var ups = testParts.Length - common;
        var rel = string.Join('/', Enumerable.Repeat("..", ups))
                  + (ups > 0 && mainParts.Length > common ? "/" : string.Empty)
                  + string.Join('/', mainParts.Skip(common));
        return string.IsNullOrEmpty(rel) ? mainFile : $"{rel}/{mainFile}";
    }
}
