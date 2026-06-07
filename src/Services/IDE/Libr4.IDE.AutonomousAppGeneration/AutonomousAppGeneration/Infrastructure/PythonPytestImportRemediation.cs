using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Back-compat facade for pytest import remediation; delegates to <see cref="PythonProjectLayoutNormalizer"/>.
/// </summary>
public static class PythonPytestImportRemediation
{
    public static int Apply(IList<GeneratedFile> files, string? buildLog) =>
        PythonProjectLayoutNormalizer.Normalize(files, buildLog);

    internal static string ResolveImportModule(string mainRelativePath) =>
        PythonProjectLayoutNormalizer.ResolveImportModule(mainRelativePath);

    internal static string GetPythonPathTarget(string mainRelativePath)
    {
        var importModule = ResolveImportModule(mainRelativePath);
        return PythonProjectLayoutNormalizer.ResolveSysPathRoot(mainRelativePath, importModule);
    }

    internal static string BuildSysPathInsert(string testRelativePath, string mainRelativePath)
    {
        var importModule = ResolveImportModule(mainRelativePath);
        var sysPathRoot = PythonProjectLayoutNormalizer.ResolveSysPathRoot(mainRelativePath, importModule);
        return PythonProjectLayoutNormalizer.BuildSysPathInsert(testRelativePath, sysPathRoot);
    }

    internal static string BuildMinimalFastApiTest(string testRelativePath, string mainPath)
    {
        var discovery = PythonProjectLayoutNormalizer.DiscoverAppEntry(new[]
        {
            new GeneratedFile(mainPath, "python", "from fastapi import FastAPI\napp = FastAPI()\n")
        });
        if (discovery is null)
            return string.Empty;

        return PythonProjectLayoutNormalizer.BuildMinimalFastApiTest(testRelativePath, discovery);
    }

    public static bool IsLocalPythonModule(string moduleName, IEnumerable<GeneratedFile> files) =>
        PythonProjectLayoutNormalizer.IsLocalPythonModule(moduleName, files);
}
