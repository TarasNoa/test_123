using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public static class FineTuningStackClassifier
{
    public static string Classify(AppGenerationOrchestrator orchestrator)
    {
        var plan = orchestrator.Plan;
        if (plan is not null)
        {
            var fromPlan = ClassifyText(string.Join(' ',
                plan.TechStack.Languages.Concat(plan.TechStack.Frameworks).Concat(plan.TechStack.Databases)));
            if (fromPlan is not "unknown")
                return fromPlan;

            fromPlan = ClassifyText(plan.RuntimeImage);
            if (fromPlan is not "unknown")
                return fromPlan;
        }

        var fromFiles = ClassifyFromFiles(orchestrator.Files.Select(f => f.RelativePath));
        return fromFiles;
    }

    private static string ClassifyFromFiles(IEnumerable<string> paths)
    {
        var list = paths.ToList();
        if (list.Any(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                          || p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
            return "dotnet";

        if (list.Any(p => p.Contains("manage.py", StringComparison.OrdinalIgnoreCase)
                          || p.EndsWith(".py", StringComparison.OrdinalIgnoreCase)))
            return "django";

        if (list.Any(p => p.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                          || p.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)
                          || p.Equals("package.json", StringComparison.OrdinalIgnoreCase)))
            return "react";

        return "unknown";
    }

    private static string ClassifyText(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("django") || lower.Contains("python"))
            return "django";
        if (lower.Contains("react") || lower.Contains("next") || lower.Contains("node"))
            return "react";
        if (lower.Contains("dotnet") || lower.Contains(".net") || lower.Contains("csharp"))
            return "dotnet";
        return "unknown";
    }
}
