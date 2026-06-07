using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public sealed record PersonaConclusionEntry(string Text, string Source, DateTime RecordedAtUtc);

public sealed class PersonaDocument
{
    public string UserId { get; init; } = string.Empty;

    public string ProjectKey { get; init; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string CommunicationStyle { get; set; } = string.Empty;

    public List<string> Goals { get; } = [];

    public List<PersonaConclusionEntry> Conclusions { get; } = [];

    public List<string> ProjectPatterns { get; } = [];

    public static PersonaDocument Parse(string userId, string projectKey, string content)
    {
        var doc = new PersonaDocument { UserId = userId, ProjectKey = projectKey };
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

            if (line.StartsWith("communication_style:", StringComparison.OrdinalIgnoreCase))
            {
                doc.CommunicationStyle = line["communication_style:".Length..].Trim();
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
                case "goals":
                    if (!string.IsNullOrWhiteSpace(item))
                        doc.Goals.Add(item);
                    break;
                case "conclusions":
                    if (TryParseConclusion(item, out var conclusion))
                        doc.Conclusions.Add(conclusion);
                    break;
                case "project patterns":
                    if (!string.IsNullOrWhiteSpace(item))
                        doc.ProjectPatterns.Add(item);
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
        sb.AppendLine($"project_key: {ProjectKey}");
        sb.AppendLine($"updated_at: {UpdatedAtUtc:O}");
        sb.AppendLine("version: 1");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Project Persona");
        sb.AppendLine();
        sb.AppendLine($"communication_style: {CommunicationStyle}");
        sb.AppendLine();
        sb.AppendLine("## Goals");
        if (Goals.Count == 0)
            sb.AppendLine("- (none yet)");
        else
            foreach (var goal in Goals.Distinct(StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"- {goal}");

        sb.AppendLine();
        sb.AppendLine("## Conclusions");
        if (Conclusions.Count == 0)
            sb.AppendLine("- (none yet)");
        else
            foreach (var entry in Conclusions.OrderByDescending(c => c.RecordedAtUtc))
                sb.AppendLine($"- {entry.Text} (source: {entry.Source}, at: {entry.RecordedAtUtc:yyyy-MM-dd})");

        sb.AppendLine();
        sb.AppendLine("## Project Patterns");
        if (ProjectPatterns.Count == 0)
            sb.AppendLine("- (none yet)");
        else
            foreach (var pattern in ProjectPatterns.Distinct(StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"- {pattern}");

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    public string ToPlanningSection(int maxChars)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## honcho_persona");
        sb.AppendLine($"Project-scoped persona for `{ProjectKey}`.");

        if (!string.IsNullOrWhiteSpace(CommunicationStyle))
        {
            sb.AppendLine();
            sb.AppendLine("### Communication Style");
            sb.AppendLine($"- {CommunicationStyle}");
        }

        if (Goals.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Goals");
            foreach (var goal in Goals.Take(5))
                sb.AppendLine($"- {goal}");
        }

        if (Conclusions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Dialectic Conclusions");
            foreach (var conclusion in Conclusions.OrderByDescending(c => c.RecordedAtUtc).Take(6))
                sb.AppendLine($"- {conclusion.Text}");
        }

        if (ProjectPatterns.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Project Patterns");
            foreach (var pattern in ProjectPatterns.Take(5))
                sb.AppendLine($"- {pattern}");
        }

        var text = sb.ToString().TrimEnd();
        return text.Length <= maxChars ? text : text[..maxChars] + "…";
    }

    private static bool TryParseConclusion(string item, out PersonaConclusionEntry entry)
    {
        entry = new PersonaConclusionEntry(item, "local", DateTime.UtcNow);
        var sourceIdx = item.LastIndexOf("(source:", StringComparison.OrdinalIgnoreCase);
        if (sourceIdx < 0)
            return !string.IsNullOrWhiteSpace(item);

        var text = item[..sourceIdx].Trim();
        var meta = item[(sourceIdx + 1)..].Trim(')', ' ');
        var source = "local";
        var recorded = DateTime.UtcNow;
        foreach (var part in meta.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("source:", StringComparison.OrdinalIgnoreCase))
                source = part["source:".Length..].Trim();
            if (part.StartsWith("at:", StringComparison.OrdinalIgnoreCase)
                && DateTime.TryParse(part["at:".Length..].Trim(), out var parsed))
                recorded = parsed.ToUniversalTime();
        }

        entry = new PersonaConclusionEntry(text, source, recorded);
        return !string.IsNullOrWhiteSpace(text);
    }
}
