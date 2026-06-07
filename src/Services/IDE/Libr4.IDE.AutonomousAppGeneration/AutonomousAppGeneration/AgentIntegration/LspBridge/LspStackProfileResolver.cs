using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

public static class LspStackProfileResolver
{
    public static string? ResolveProfileKey(GenerationPlan? plan, string? filePath)
    {
        var ext = string.IsNullOrWhiteSpace(filePath)
            ? null
            : Path.GetExtension(filePath).ToLowerInvariant();

        if (ext is ".ts" or ".tsx" or ".js" or ".jsx")
            return "typescript";

        if (ext is ".cs")
            return "csharp";

        if (ext is ".py")
            return "python";

        if (plan is null)
            return null;

        if (plan.TechStack.Languages.Any(l => l.Contains("TypeScript", StringComparison.OrdinalIgnoreCase)
                                               || l.Contains("JavaScript", StringComparison.OrdinalIgnoreCase)))
            return "typescript";

        if (plan.TechStack.Languages.Any(l => l.Contains("C#", StringComparison.OrdinalIgnoreCase)
                                               || l.Contains("csharp", StringComparison.OrdinalIgnoreCase)))
            return "csharp";

        if (plan.TechStack.Languages.Any(l => l.Contains("Python", StringComparison.OrdinalIgnoreCase)))
            return "python";

        return null;
    }
}
