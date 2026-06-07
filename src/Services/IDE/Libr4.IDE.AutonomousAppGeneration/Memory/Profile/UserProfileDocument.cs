using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public sealed record ProfileStackEntry(string Stack, int RunCount, DateTime LastSeenUtc);

public sealed record ProfileFailureEntry(string Signature, int OccurrenceCount, DateTime LastSeenUtc);

public sealed record ProfileSuccessEntry(string Pattern, int IterationCount, DateTime CompletedAtUtc);

public sealed class UserProfileDocument
{
    public string UserId { get; init; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ProfileStackEntry> PreferredStacks { get; } = [];

    public List<ProfileFailureEntry> RecurringFailures { get; } = [];

    public List<ProfileSuccessEntry> SuccessfulPatterns { get; } = [];

    public static UserProfileDocument Parse(string userId, string content)
    {
        var doc = new UserProfileDocument { UserId = userId };
        if (string.IsNullOrWhiteSpace(content))
            return doc;

        var section = string.Empty;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("updated_at:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["updated_at:".Length..].Trim();
                if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var updated))
                    doc.UpdatedAtUtc = updated;
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                section = line[3..].Trim().ToLowerInvariant();
                continue;
            }

            if (!line.StartsWith("- ", StringComparison.Ordinal))
                continue;

            var item = line[2..].Trim();
            switch (section)
            {
                case "preferred stacks":
                    if (TryParseCountedEntry(item, out var stack, out var stackRuns, out var stackLast))
                        doc.PreferredStacks.Add(new ProfileStackEntry(stack, stackRuns, stackLast));
                    break;
                case "recurring failures":
                    if (TryParseCountedEntry(item, out var failure, out var failCount, out var failLast))
                        doc.RecurringFailures.Add(new ProfileFailureEntry(failure, failCount, failLast));
                    break;
                case "successful patterns":
                    if (TryParseSuccessEntry(item, out var pattern, out var iterations, out var completed))
                        doc.SuccessfulPatterns.Add(new ProfileSuccessEntry(pattern, iterations, completed));
                    break;
            }
        }

        return doc;
    }

    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"user_id: {UserId}");
        sb.AppendLine($"updated_at: {UpdatedAtUtc:O}");
        sb.AppendLine("version: 1");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# USER Profile");
        sb.AppendLine();
        sb.AppendLine("## Preferred Stacks");
        if (PreferredStacks.Count == 0)
            sb.AppendLine("- (none yet)");
        else
            foreach (var entry in PreferredStacks.OrderByDescending(e => e.RunCount).ThenByDescending(e => e.LastSeenUtc))
                sb.AppendLine($"- {entry.Stack} (runs: {entry.RunCount}, last: {entry.LastSeenUtc:yyyy-MM-dd})");

        sb.AppendLine();
        sb.AppendLine("## Recurring Failures");
        if (RecurringFailures.Count == 0)
            sb.AppendLine("- (none yet)");
        else
            foreach (var entry in RecurringFailures.OrderByDescending(e => e.OccurrenceCount).ThenByDescending(e => e.LastSeenUtc))
                sb.AppendLine($"- {entry.Signature} (count: {entry.OccurrenceCount}, last: {entry.LastSeenUtc:yyyy-MM-dd})");

        sb.AppendLine();
        sb.AppendLine("## Successful Patterns");
        if (SuccessfulPatterns.Count == 0)
            sb.AppendLine("- (none yet)");
        else
            foreach (var entry in SuccessfulPatterns.OrderByDescending(e => e.CompletedAtUtc))
                sb.AppendLine($"- {entry.Pattern} → completed in {entry.IterationCount} iterations ({entry.CompletedAtUtc:yyyy-MM-dd})");

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    public string ToPlanningSection(int maxChars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## user_profile");
        sb.AppendLine("Personalized memory for this user — honor preferred stacks and avoid recurring failures.");

        if (PreferredStacks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Preferred Stacks");
            foreach (var entry in PreferredStacks.OrderByDescending(e => e.RunCount).Take(5))
                sb.AppendLine($"- {entry.Stack}");
        }

        if (RecurringFailures.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Recurring Failures");
            foreach (var entry in RecurringFailures.OrderByDescending(e => e.OccurrenceCount).Take(5))
                sb.AppendLine($"- {entry.Signature}");
        }

        if (SuccessfulPatterns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Successful Patterns");
            foreach (var entry in SuccessfulPatterns.OrderByDescending(e => e.CompletedAtUtc).Take(3))
                sb.AppendLine($"- {entry.Pattern}");
        }

        var text = sb.ToString().TrimEnd();
        return text.Length <= maxChars ? text : text[..maxChars] + "…";
    }

    private static bool TryParseCountedEntry(string item, out string value, out int count, out DateTime lastSeen)
    {
        value = item;
        count = 1;
        lastSeen = DateTime.UtcNow;

        var open = item.LastIndexOf(" (", StringComparison.Ordinal);
        if (open < 0)
            return !string.IsNullOrWhiteSpace(value);

        value = item[..open].Trim();
        var meta = item[(open + 2)..].TrimEnd(')');
        foreach (var part in meta.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("runs:", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(part["runs:".Length..].Trim(), out var runs))
                count = runs;
            if (part.StartsWith("count:", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(part["count:".Length..].Trim(), out var occurrences))
                count = occurrences;
            if (part.StartsWith("last:", StringComparison.OrdinalIgnoreCase)
                && DateTime.TryParse(part["last:".Length..].Trim(), out var parsed))
                lastSeen = parsed.ToUniversalTime();
        }

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseSuccessEntry(string item, out string pattern, out int iterations, out DateTime completed)
    {
        pattern = item;
        iterations = 0;
        completed = DateTime.UtcNow;

        var arrow = item.IndexOf(" → completed in ", StringComparison.Ordinal);
        if (arrow < 0)
            return !string.IsNullOrWhiteSpace(pattern);

        pattern = item[..arrow].Trim();
        var tail = item[(arrow + " → completed in ".Length)..];
        var iterEnd = tail.IndexOf(" iterations", StringComparison.Ordinal);
        if (iterEnd > 0 && int.TryParse(tail[..iterEnd].Trim(), out var parsedIterations))
            iterations = parsedIterations;

        var dateStart = tail.LastIndexOf('(');
        var dateEnd = tail.LastIndexOf(')');
        if (dateStart >= 0 && dateEnd > dateStart
            && DateTime.TryParse(tail[(dateStart + 1)..dateEnd].Trim(), out var parsedDate))
            completed = parsedDate.ToUniversalTime();

        return !string.IsNullOrWhiteSpace(pattern);
    }
}
