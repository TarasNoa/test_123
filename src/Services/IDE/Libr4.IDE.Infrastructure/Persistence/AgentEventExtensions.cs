namespace Libr4.IDE.Infrastructure.Persistence;

/// <summary>
/// Extension methods for AgentEventEntity
/// </summary>
public static class AgentEventExtensions
{
    /// <summary>
    /// Convert AgentEventEntity Type to display string
    /// </summary>
    public static string ToDisplayString(this string? eventType)
    {
        return eventType switch
        {
            "Idle" => "Idle",
            "Processing" or "Busy" => "Processing",
            "Validating" => "Validating",
            "Error" or "Failed" => "Error",
            _ => "Unknown"
        };
    }
}
