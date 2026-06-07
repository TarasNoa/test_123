using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Pairs web controllers with their backing services for LLM repair prompts so the fixer
/// sees both APIs at once (prevents controller/service naming drift fixes).
/// </summary>
public static class ControllerServiceRepairContext
{
    public sealed record AlignmentPair(GeneratedFile Controller, GeneratedFile Service);

    public static bool TryResolveAlignmentPair(
        IReadOnlyList<ErrorReport> errors,
        IReadOnlyList<GeneratedFile> currentFiles,
        out AlignmentPair? pair)
    {
        pair = null;
        var controller = FindControllerFromErrors(errors, currentFiles)
                         ?? FindControllerMentionedInErrors(errors, currentFiles);
        if (controller is null)
            return false;

        var service = FindPairedService(controller, currentFiles);
        if (service is null)
            return false;

        pair = new AlignmentPair(controller, service);
        return true;
    }

    public static void AppendAlignmentInstructions(
        StringBuilder sb,
        AlignmentPair pair,
        HashSet<string>? skipPathsInGenericSection = null)
    {
        skipPathsInGenericSection?.Add(pair.Controller.RelativePath);
        skipPathsInGenericSection?.Add(pair.Service.RelativePath);

        sb.AppendLine("CONTROLLER/SERVICE ALIGNMENT (critical):");
        sb.AppendLine($"EXISTING contract (read-only — do NOT modify): {pair.Service.RelativePath}");
        sb.AppendLine("---");
        sb.AppendLine(pair.Service.Content ?? string.Empty);
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"BROKEN target (fix to match service API — modify ONLY this file): {pair.Controller.RelativePath}");
        sb.AppendLine("---");
        sb.AppendLine(pair.Controller.Content ?? string.Empty);
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(
            "TASK: Align controller method calls, parameter types, and return types to the EXISTING service above. " +
            "Do not rename service methods. Do not change the service file. Return ONLY the fixed controller in JSON.");
        sb.AppendLine();
    }

    public static void EnsurePairInContext(
        IDictionary<string, GeneratedFile> selected,
        IReadOnlyList<GeneratedFile> currentFiles,
        IReadOnlyList<ErrorReport> errors)
    {
        if (!TryResolveAlignmentPair(errors, currentFiles, out var pair) || pair is null)
            return;

        selected[pair.Controller.RelativePath] = pair.Controller;
        selected[pair.Service.RelativePath] = pair.Service;
    }

    private static GeneratedFile? FindControllerFromErrors(
        IReadOnlyList<ErrorReport> errors,
        IReadOnlyList<GeneratedFile> currentFiles)
    {
        foreach (var err in errors)
        {
            if (string.IsNullOrWhiteSpace(err.FilePath))
                continue;
            if (!err.FilePath.Contains("Controller", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = currentFiles.FirstOrDefault(f =>
                f.RelativePath.Contains("Controller", StringComparison.OrdinalIgnoreCase)
                && f.RelativePath.EndsWith(
                    Path.GetFileName(err.FilePath.Replace('\\', '/')),
                    StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return null;
    }

    private static GeneratedFile? FindControllerMentionedInErrors(
        IReadOnlyList<ErrorReport> errors,
        IReadOnlyList<GeneratedFile> currentFiles)
    {
        var blob = string.Join(' ', errors.Select(e => $"{e.Message} {e.FilePath}"));
        if (!blob.Contains("Controller", StringComparison.OrdinalIgnoreCase))
            return null;

        return currentFiles
            .Where(f => f.RelativePath.Contains("/web/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith("Controller.java", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => blob.Contains(Path.GetFileNameWithoutExtension(f.RelativePath), StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    private static GeneratedFile? FindPairedService(GeneratedFile controller, IReadOnlyList<GeneratedFile> currentFiles)
    {
        var controllerName = Path.GetFileNameWithoutExtension(controller.RelativePath);
        if (!controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            return null;

        var serviceName = controllerName[..^"Controller".Length] + "Service";
        return currentFiles.FirstOrDefault(f =>
            string.Equals(Path.GetFileNameWithoutExtension(f.RelativePath), serviceName, StringComparison.OrdinalIgnoreCase)
            && f.RelativePath.Contains("/service/", StringComparison.OrdinalIgnoreCase));
    }
}
