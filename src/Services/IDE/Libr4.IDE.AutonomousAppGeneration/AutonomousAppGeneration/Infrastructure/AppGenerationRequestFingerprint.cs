using System.Security.Cryptography;
using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public static class AppGenerationRequestFingerprint
{
    public static string Build(
        string userRequest,
        int maxIterations,
        string? triggerSource = null,
        string? triggerActor = null,
        string? tenantId = null)
    {
        var normalized = string.Concat(
            (userRequest ?? string.Empty).Trim().ToLowerInvariant(),
            "|",
            maxIterations.ToString(),
            "|",
            (triggerSource ?? string.Empty).ToLowerInvariant(),
            "|",
            (triggerActor ?? string.Empty).ToLowerInvariant(),
            "|",
            (tenantId ?? string.Empty).ToLowerInvariant());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
