using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public static class UserProfileIdentityResolver
{
    public static string? Resolve(AppGenerationOrchestrator orchestrator)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);

        if (!string.IsNullOrWhiteSpace(orchestrator.TenantId))
            return SanitizeUserId(orchestrator.TenantId);

        var actor = orchestrator.Triggers.FirstOrDefault()?.Actor;
        if (!string.IsNullOrWhiteSpace(actor))
            return SanitizeUserId(actor);

        return null;
    }

    public static string SanitizeUserId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "anonymous";

        var cleaned = new string(raw.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.').ToArray());
        if (cleaned.Length == 0)
            return "anonymous";

        cleaned = cleaned.Replace("..", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Trim('.');

        return cleaned.Length <= 64 ? cleaned : cleaned[..64];
    }
}
