using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public static class HermesMemoryScopeResolver
{
    public const string Run = "run";
    public const string Project = "project";
    public const string User = "user";
    public const string SpacePrefix = "project:";

    public static string BuildSpaceFingerprint(Guid spaceId) => $"{SpacePrefix}{spaceId:D}";

    public static bool TryParseSpaceFingerprint(string? fingerprint, out Guid spaceId)
    {
        spaceId = default;
        if (string.IsNullOrWhiteSpace(fingerprint)
            || !fingerprint.StartsWith(SpacePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Guid.TryParse(fingerprint.AsSpan(SpacePrefix.Length), out spaceId);
    }

    public static string ResolveProjectFingerprint(GenerationPlan plan, string? requestFingerprint = null)
    {
        if (!string.IsNullOrWhiteSpace(requestFingerprint))
            return requestFingerprint;

        var stack = string.Join(',', plan.TechStack.Frameworks.Concat(plan.TechStack.Languages));
        return $"{plan.ApplicationName}|{stack}".Trim().ToLowerInvariant();
    }

    public static string ResolveFingerprint(
        string scope,
        ToolContext context,
        IHermesMemoryManager manager,
        string? userId = null)
    {
        var normalized = (scope ?? Project).Trim();
        if (TryParseSpaceFingerprint(normalized, out var explicitSpaceId))
            return BuildSpaceFingerprint(explicitSpaceId);

        var normalizedScope = normalized.ToLowerInvariant();
        return normalizedScope switch
        {
            Run => context.Session.RunId is Guid runId
                ? $"run:{runId:N}"
                : throw new InvalidOperationException("run scope requires Session.RunId"),
            User => $"user:{(string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim())}",
            Project => context.Session.SpaceId is Guid spaceId
                ? BuildSpaceFingerprint(spaceId)
                : context.Plan is GenerationPlan plan
                    ? manager.ResolveFingerprint(plan)
                    : throw new InvalidOperationException("project scope requires GenerationPlan or SpaceId"),
            _ => throw new InvalidOperationException($"unknown memory scope: {scope}")
        };
    }

    public static bool IsValidScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return false;

        if (TryParseSpaceFingerprint(scope, out _))
            return true;

        return string.Equals(scope, Run, StringComparison.OrdinalIgnoreCase)
               || string.Equals(scope, Project, StringComparison.OrdinalIgnoreCase)
               || string.Equals(scope, User, StringComparison.OrdinalIgnoreCase);
    }

    public static MemoryKind? ParseKind(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return raw.Trim().ToLowerInvariant() switch
        {
            "episodic" or "l0" or "l0_episodic" => MemoryKind.Episodic,
            "procedural" or "l1" or "l1_procedural" => MemoryKind.Procedural,
            "semantic" or "l2" or "l2_semantic" => MemoryKind.Semantic,
            "strategic" or "l3" or "l3_strategic" => MemoryKind.Strategic,
            "meta" or "l4" or "l4_meta" => MemoryKind.Meta,
            _ => null
        };
    }
}
