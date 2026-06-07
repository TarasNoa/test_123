namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

/// <summary>Minimal 5-field cron matcher (minute hour day month dow). Supports * and integer literals.</summary>
public static class FlowCronParser
{
    public static bool IsDue(string cronExpression, DateTime utcNow, DateTime? lastRunUtc)
    {
        if (!TryParse(cronExpression, out var minute, out var hour, out var day, out var month, out var dow))
            return false;

        if (!Matches(minute, utcNow.Minute)
            || !Matches(hour, utcNow.Hour)
            || !Matches(day, utcNow.Day)
            || !Matches(month, utcNow.Month)
            || !Matches(dow, (int)utcNow.DayOfWeek))
            return false;

        if (lastRunUtc is null)
            return true;

        return lastRunUtc.Value.Year != utcNow.Year
               || lastRunUtc.Value.Month != utcNow.Month
               || lastRunUtc.Value.Day != utcNow.Day
               || lastRunUtc.Value.Hour != utcNow.Hour
               || lastRunUtc.Value.Minute != utcNow.Minute;
    }

    public static DateTime? GetNextUtc(string cronExpression, DateTime utcNow)
    {
        for (var i = 0; i < 366 * 24 * 60; i++)
        {
            var candidate = utcNow.AddMinutes(i);
            if (IsDue(cronExpression, candidate, null))
                return candidate;
        }

        return null;
    }

    private static bool Matches(string field, int value) =>
        field == "*" || int.TryParse(field, out var parsed) && parsed == value;

    private static bool TryParse(
        string cronExpression,
        out string minute,
        out string hour,
        out string day,
        out string month,
        out string dow)
    {
        minute = hour = day = month = dow = string.Empty;
        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5)
            return false;

        minute = parts[0];
        hour = parts[1];
        day = parts[2];
        month = parts[3];
        dow = parts[4];
        return true;
    }
}
