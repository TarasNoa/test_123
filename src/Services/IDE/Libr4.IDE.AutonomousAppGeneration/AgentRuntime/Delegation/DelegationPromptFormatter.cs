namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public static class DelegationPromptFormatter
{
    public static string FormatResultsSection(DelegationNotification notification)
    {
        var lines = new List<string>
        {
            "## delegation_results",
            $"- id: `{notification.DelegationId}`",
            $"- completed: {notification.CompletedAtUtc:O}",
            $"- summary: {notification.Summary}"
        };

        if (!string.IsNullOrWhiteSpace(notification.OutputRelativePath))
        {
            lines.Add($"- output: `{notification.OutputRelativePath}`");
            lines.Add($"- read: use delegation_read with {{\"id\":\"{notification.DelegationId}\"}}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
