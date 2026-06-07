namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public enum BuildErrorCategory
{
    Unknown,
    Environment,
    Dependency,
    Compilation,
    Configuration,
    Runtime,
    TestFailure,
    Security
}

public static class BuildErrorCategoryClassifier
{
    public static (BuildErrorCategory Category, string Hint) Classify(string? buildLog, string? errorMessage = null)
    {
        var blob = $"{buildLog}\n{errorMessage}".ToLowerInvariant();

        if (ContainsAny(blob, "pip not found", "pip: command not found", "'pip' is not recognized", "no module named pip"))
            return (BuildErrorCategory.Environment, "Use python -m pip instead of bare pip.");

        if (ContainsAny(blob, "docker daemon", "cannot connect to the docker", "docker not found"))
            return (BuildErrorCategory.Environment, "Docker is unavailable; ensure runtime provider is docker or fix WSL fallback.");

        if (ContainsAny(blob, "python: command not found", "pyenv", "no python"))
            return (BuildErrorCategory.Environment, "Python runtime missing in shadow workspace image.");

        if (ContainsAny(blob, "modulenotfounderror", "no module named", "cannot find module", "package not found", "npm err!", "could not resolve"))
            return (BuildErrorCategory.Dependency, "Install missing dependency or fix import path.");

        if (ContainsAny(blob, "syntaxerror", "indentationerror", "ts(", "error cs", "compilation failed", "cannot find symbol"))
            return (BuildErrorCategory.Compilation, "Read failing file, fix syntax/types/imports.");

        if (ContainsAny(blob, "traceback", "runtimeerror", "segmentation fault", "killed", "oom", "exit code 1", "importerror", "attributeerror"))
            return (BuildErrorCategory.Runtime, "Runtime failure after compile — inspect stack trace and fix logic/imports.");

        if (ContainsAny(blob, "build failed"))
            return (BuildErrorCategory.Compilation, "Build step failed — read log and fix compile/config issues.");

        if (ContainsAny(blob, "manage.py", "settings.py", "django.core", "allowed_hosts", "secret_key", "cors", "port already in use"))
            return (BuildErrorCategory.Configuration, "Check Django/settings, CORS, env vars, PORT.");

        if (ContainsAny(blob, "assertionerror", "pytest", "test failed", "failures=", "npm test", "vitest"))
            return (BuildErrorCategory.TestFailure, "Read failing test and implementation; fix behavior not just compile.");

        if (ContainsAny(blob, "permission denied", "eacces", "unsafe", "secret", "api key"))
            return (BuildErrorCategory.Security, "Remove hardcoded secrets; use environment variables.");

        return (BuildErrorCategory.Unknown, "Investigate build log with read_file/grep, then minimal edit.");
    }

    public static string FormatForAgent(string? buildLog, string? errorMessage = null)
    {
        var (cat, hint) = Classify(buildLog, errorMessage);
        return $"error_category={cat}\nhint={hint}";
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (haystack.Contains(n, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
