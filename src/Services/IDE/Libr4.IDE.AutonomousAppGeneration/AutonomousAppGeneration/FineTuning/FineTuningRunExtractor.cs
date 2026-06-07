using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public static class FineTuningRunExtractor
{
    public static FineTuningExample? TryExtract(AppGenerationOrchestrator orchestrator, string stack)
    {
        if (orchestrator.Status != GenerationStatus.Completed || orchestrator.Files.Count == 0)
            return null;

        var instruction = BuildInstruction(orchestrator);
        var output = BuildOutput(orchestrator);
        if (string.IsNullOrWhiteSpace(output))
            return null;

        return new FineTuningExample(
            instruction,
            output,
            stack,
            orchestrator.Id,
            DateTime.UtcNow);
    }

    private static string BuildInstruction(AppGenerationOrchestrator orchestrator)
    {
        var sb = new StringBuilder();
        sb.AppendLine(orchestrator.UserRequest.Trim());
        if (orchestrator.Plan is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"Application: {orchestrator.Plan.ApplicationName}");
            sb.AppendLine($"Stack: {string.Join(", ", orchestrator.Plan.TechStack.Languages.Concat(orchestrator.Plan.TechStack.Frameworks))}");
        }

        return sb.ToString().Trim();
    }

    private static string BuildOutput(AppGenerationOrchestrator orchestrator)
    {
        var sb = new StringBuilder();
        foreach (var file in orchestrator.Files.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"### {file.RelativePath}");
            sb.AppendLine(file.Content);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }
}
