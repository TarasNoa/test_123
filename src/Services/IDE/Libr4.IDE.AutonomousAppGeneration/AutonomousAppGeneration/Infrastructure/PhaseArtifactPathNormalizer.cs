using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Ensures multi-agent phase artifacts land under backend/ or frontend/ (not swapped or unprefixed).
/// </summary>
public static class PhaseArtifactPathNormalizer
{
    public static List<DomainGeneratedFile> NormalizeForPhase(
        AgentPhase phase,
        IReadOnlyList<DomainGeneratedFile> files,
        GenerationPlan plan)
    {
        if (files.Count == 0)
            return new List<DomainGeneratedFile>();

        var isJavaReact = StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;
        var list = new List<DomainGeneratedFile>(files.Count);

        foreach (var file in files)
        {
            var path = StackArtifactCompleteness.SanitizeRelativePath(file.RelativePath);
            if (path.Length == 0)
                continue;

            path = RelocateByExtension(phase, path, isJavaReact);
            path = EnsurePhasePrefix(phase, path, isJavaReact);
            if (path.Length == 0)
                continue;

            list.Add(new DomainGeneratedFile(path, file.Language, file.Content));
        }

        return list;
    }

    private static string RelocateByExtension(AgentPhase phase, string path, bool isJavaReact)
    {
        if (!isJavaReact)
            return path;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var isJava = ext is ".java" or ".xml" or ".properties" or ".yml" or ".yaml" or ".sql";
        var isFrontend = ext is ".tsx" or ".ts" or ".jsx" or ".js" or ".css" or ".scss" or ".html";

        if (phase == AgentPhase.Frontend && isJava)
            return PrependPrefix("backend", StripPrefix("frontend", path));

        if ((phase == AgentPhase.Backend || phase == AgentPhase.Database) && isFrontend)
            return PrependPrefix("frontend", StripPrefix("backend", path));

        return path;
    }

    private static string EnsurePhasePrefix(AgentPhase phase, string path, bool isJavaReact)
    {
        if (!isJavaReact)
            return path;

        if (path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
            return path;

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var target = phase switch
        {
            AgentPhase.Backend or AgentPhase.Database => "backend",
            AgentPhase.Frontend => "frontend",
            _ => null
        };

        if (target is null)
            return path;

        if (ext is ".java" or ".xml" or ".properties" or ".yml" or ".yaml" or ".sql")
            target = "backend";
        else if (ext is ".tsx" or ".ts" or ".jsx" or ".js" or ".css" or ".scss" or ".html")
            target = "frontend";

        return PrependPrefix(target, path);
    }

    private static string PrependPrefix(string prefix, string path)
    {
        path = path.TrimStart('/');
        if (path.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
            return path;

        if (path.Equals("pom.xml", StringComparison.OrdinalIgnoreCase))
            return $"{prefix}/pom.xml";

        return $"{prefix}/{path}";
    }

    private static string StripPrefix(string prefix, string path)
    {
        path = path.TrimStart('/');
        if (path.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
            return path[(prefix.Length + 1)..];
        return path;
    }
}
