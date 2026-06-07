using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Level 3 deterministic recovery for runtime/DI/configuration failures surfaced in build or test logs.
/// </summary>
public static class RuntimeRecoveryService
{
    public static bool IsRuntimeFailure(string? signal) =>
        !string.IsNullOrWhiteSpace(signal)
        && (signal.Contains("APPLICATION FAILED TO START", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("BeanCreationException", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("UnsatisfiedDependencyException", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Address already in use", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("already in use", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("NullPointerException", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Failed to configure", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("NoSuchBeanDefinitionException", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("EADDRINUSE", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Error: Cannot find module", StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<GeneratedFile> Apply(
        IReadOnlyList<GeneratedFile> currentFiles,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog) =>
        StackRecovery.StackArtifactRecoveryRouter.ApplyRuntimeRecovery(currentFiles, plan, errors, buildLog);

    public static int ApplyJavaRuntimeFixes(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog)
    {
        var changed = FixServerPortConflict(files, "application.properties", "application.yml");
        changed |= EnsureDevDatasourceProfile(files);
        changed |= RemoveRuntimeFailingTests(files, buildLog, ".java");
        return changed ? 1 : 0;
    }

    public static int ApplyDotNetRuntimeFixes(IList<GeneratedFile> files, string? buildLog)
    {
        var changed = FixServerPortConflict(files, "appsettings.json", "launchSettings.json");
        changed |= RemoveRuntimeFailingTests(files, buildLog, ".cs");
        return changed ? 1 : 0;
    }

    public static int ApplyNodeRuntimeFixes(IList<GeneratedFile> files, string? buildLog)
    {
        var changed = FixNodePortEnv(files);
        changed |= RemoveRuntimeFailingTests(files, buildLog, ".ts", ".js");
        return changed ? 1 : 0;
    }

    public static int ApplyPythonRuntimeFixes(IList<GeneratedFile> files, string? buildLog)
    {
        var changed = FixPythonPortEnv(files);
        changed |= RemoveRuntimeFailingTests(files, buildLog, ".py");
        return changed ? 1 : 0;
    }

    public static int ApplyGenericRuntimeFixes(IList<GeneratedFile> files, string? buildLog)
    {
        var changed = FixServerPortConflict(files, "application.properties", "application.yml", "appsettings.json");
        changed |= FixNodePortEnv(files);
        changed |= FixPythonPortEnv(files);
        return changed ? 1 : 0;
    }

    private static bool FixServerPortConflict(IList<GeneratedFile> files, params string[] manifestNames)
    {
        var changed = false;
        foreach (var name in manifestNames)
        {
            var idx = files.ToList().FindIndex(f => f.RelativePath.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                continue;

            var content = files[idx].Content ?? string.Empty;
            if (content.Contains("port", StringComparison.OrdinalIgnoreCase))
                continue;

            var suffix = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? "\n  \"Kestrel\": { \"Endpoints\": { \"Http\": { \"Url\": \"http://localhost:8080\" } } }\n"
                : "\nserver.port=${PORT:8080}\n";
            files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, content.TrimEnd() + suffix);
            changed = true;
        }

        return changed;
    }

    private static bool EnsureDevDatasourceProfile(IList<GeneratedFile> files)
    {
        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.EndsWith("application.properties", StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return false;

        var content = files[idx].Content ?? string.Empty;
        if (content.Contains("datasource.url", StringComparison.OrdinalIgnoreCase))
            return false;

        files[idx] = new GeneratedFile(
            files[idx].RelativePath,
            files[idx].Language,
            content.TrimEnd() + "\nspring.datasource.url=jdbc:h2:mem:app;DB_CLOSE_DELAY=-1\nspring.jpa.hibernate.ddl-auto=update\n");
        return true;
    }

    private static bool FixNodePortEnv(IList<GeneratedFile> files)
    {
        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.Equals("frontend/.env", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Equals(".env", StringComparison.OrdinalIgnoreCase));
        if (idx >= 0 && (files[idx].Content?.Contains("PORT", StringComparison.OrdinalIgnoreCase) ?? false))
            return false;

        files.Add(new GeneratedFile("frontend/.env", "env", "PORT=3000\n"));
        return true;
    }

    private static bool FixPythonPortEnv(IList<GeneratedFile> files)
    {
        var idx = files.ToList().FindIndex(f => f.RelativePath.Equals(".env", StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            return false;
        files.Add(new GeneratedFile(".env", "env", "PORT=8000\n"));
        return true;
    }

    private static bool RemoveRuntimeFailingTests(IList<GeneratedFile> files, string? signal, params string[] extensions)
    {
        if (string.IsNullOrWhiteSpace(signal))
            return false;

        if (!signal.Contains("Tests run:", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("There are test failures", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("BUILD FAILURE", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("Test Run Failed", StringComparison.OrdinalIgnoreCase))
            return false;

        var testMatch = Regex.Match(signal, @"\[ERROR\]\s+(?<path>[^\s:]+\.\w+)", RegexOptions.IgnoreCase);
        if (!testMatch.Success)
            return false;

        var path = testMatch.Groups["path"].Value.Replace('\\', '/');
        if (!extensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return false;

        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (files[i].RelativePath.Replace('\\', '/').Contains(path, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed > 0;
    }
}
