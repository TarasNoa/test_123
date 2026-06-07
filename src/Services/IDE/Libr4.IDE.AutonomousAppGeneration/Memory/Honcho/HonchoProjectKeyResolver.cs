using System.Security.Cryptography;
using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public static class HonchoProjectKeyResolver
{
    public static string Resolve(AppGenerationOrchestrator orchestrator, string? projectWorkspacePath = null)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);

        if (!string.IsNullOrWhiteSpace(projectWorkspacePath))
            return WorkspaceTrustHasher.Compute(projectWorkspacePath, orchestrator.TenantId, orchestrator.RequestFingerprint);

        var planName = orchestrator.Plan?.ApplicationName;
        if (!string.IsNullOrWhiteSpace(planName))
            return SanitizeKey(planName);

        return SanitizeKey(orchestrator.RequestFingerprint);
    }

    public static string ResolveSessionId(AppGenerationOrchestrator orchestrator, string projectKey) =>
        $"libr4-{projectKey}-{orchestrator.Id:N}";

    private static string SanitizeKey(string raw)
    {
        var normalized = raw.Trim().ToLowerInvariant();
        if (normalized.Length <= 48)
            return normalized.Replace(' ', '-');

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
