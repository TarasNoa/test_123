using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Detects cross-stack contamination (e.g. C# files in a Node project) after generation.
/// </summary>
public static class StackPurityValidator
{
    public sealed record Finding(string Code, string Message, string FilePath, bool Critical);

    public sealed record Result(IReadOnlyList<Finding> Findings, int FilesRemoved);

    public static Result ValidateAndPrune(IList<GeneratedFile> files, GenerationPlan plan, bool autoPrune = true)
    {
        var stack = StackPlanHeuristics.Classify(plan);
        var findings = new List<Finding>();
        var toRemove = new List<int>();

        for (var i = 0; i < files.Count; i++)
        {
            var path = files[i].RelativePath.Replace('\\', '/');
            var violation = ClassifyViolation(stack, path);
            if (violation is null)
                continue;

            findings.Add(violation);
            if (autoPrune && violation.Critical)
                toRemove.Add(i);
        }

        var removed = 0;
        for (var i = toRemove.Count - 1; i >= 0; i--)
        {
            files.RemoveAt(toRemove[i]);
            removed++;
        }

        return new Result(findings, removed);
    }

    private static Finding? ClassifyViolation(StackKind stack, string path)
    {
        var isBackendJava = path.Contains("/backend/", StringComparison.OrdinalIgnoreCase)
                            || path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase);
        var isFrontend = path.Contains("/frontend/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase);

        switch (stack)
        {
            case StackKind.Node:
                if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    || (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !path.Contains("/test", StringComparison.OrdinalIgnoreCase)))
                    return new Finding("STACK_CONTAMINATION", "C# artifact in Node stack", path, Critical: true);
                if (path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase) && !isBackendJava)
                    return new Finding("STACK_CONTAMINATION", "Maven pom.xml in Node-only project", path, Critical: true);
                if (path.EndsWith(".java", StringComparison.OrdinalIgnoreCase) && !isBackendJava)
                    return new Finding("STACK_CONTAMINATION", "Java source in Node-only project", path, Critical: true);
                break;

            case StackKind.Python:
                if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "Non-Python manifest in Python stack", path, Critical: true);
                break;

            case StackKind.Java:
                if (!isFrontend && path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "package.json outside frontend in Java-only stack", path, Critical: false);
                if (!isBackendJava && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "C# artifact in Java stack", path, Critical: true);
                if (!isBackendJava && path.Contains("express", StringComparison.OrdinalIgnoreCase)
                    && path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "Node server artifact in Java stack", path, Critical: true);
                break;

            case StackKind.JavaReactFullStack:
                if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    || (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !path.Contains("Tests", StringComparison.OrdinalIgnoreCase)))
                    return new Finding("STACK_CONTAMINATION", "C# artifact in Java+React stack", path, Critical: true);
                if (path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase) && isFrontend)
                    return new Finding("STACK_CONTAMINATION", "pom.xml under frontend/", path, Critical: true);
                if (isBackendJava && path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "package.json under backend/ in Java+React stack", path, Critical: true);
                break;

            case StackKind.DotNet:
                if (path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "Non-.NET manifest in .NET stack", path, Critical: true);
                if (path.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
                    return new Finding("STACK_CONTAMINATION", "Java source in .NET stack", path, Critical: true);
                break;
        }

        return null;
    }
}
