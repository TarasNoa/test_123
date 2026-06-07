using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public static class RunHandoffOrchestratorHydrator
{
    public static AppGenerationOrchestrator? TryHydrate(Guid runId, string runsRoot)
    {
        var runDir = Path.Combine(Path.GetFullPath(runsRoot), runId.ToString("D"));
        if (!Directory.Exists(runDir))
            return null;

        var orchestrator = AppGenerationOrchestrator.CreateFromRun(
            runId,
            $"Run {runId.ToString()[..8]}",
            $"fp-handoff-{runId:N}");

        var appName = ReadApplicationName(runDir) ?? $"run-{runId.ToString()[..8]}";
        orchestrator.AttachPlan(new GenerationPlan(
            appName,
            "Offline handoff export",
            new TechStack(["unknown"], [], [], [], "handoff"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            [],
            [],
            20));

        foreach (var file in LoadWorkspaceFiles(runDir))
            orchestrator.UpsertFile(file);

        return orchestrator;
    }

    private static string? ReadApplicationName(string runDir)
    {
        var resumePath = Path.Combine(runDir, "handoff", "resume.json");
        if (!File.Exists(resumePath))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(resumePath));
            if (doc.RootElement.TryGetProperty("applicationName", out var nameEl))
                return nameEl.GetString();
        }
        catch
        {
            // ignore malformed resume metadata
        }

        return null;
    }

    private static IEnumerable<GeneratedFile> LoadWorkspaceFiles(string runDir)
    {
        var workspaceDir = Path.Combine(runDir, "workspace");
        if (!Directory.Exists(workspaceDir))
            yield break;

        foreach (var path in Directory.EnumerateFiles(workspaceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(workspaceDir, path).Replace('\\', '/');
            if (relative.Split('/').Any(p => p is "node_modules" or ".venv" or ".git"))
                continue;

            var content = File.ReadAllText(path);
            yield return new GeneratedFile(relative, InferLanguage(relative), content);
        }
    }

    private static string InferLanguage(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "cs" => "csharp",
            "ts" or "tsx" => "typescript",
            "js" or "jsx" => "javascript",
            "py" => "python",
            "json" => "json",
            "md" => "markdown",
            _ => "text"
        };
    }
}
