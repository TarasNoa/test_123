using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

public sealed record SemanticCompactionSummary(
    IReadOnlyList<string> Decisions,
    IReadOnlyList<string> FilesTouched,
    IReadOnlyList<string> OpenIssues,
    IReadOnlyList<string> NextActions,
    IReadOnlyList<string> ErrorsResolved)
{
    public string ToPromptBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine("SEMANTIC COMPACTION SUMMARY (older turns collapsed):");
        AppendList(sb, "decisions", Decisions);
        AppendList(sb, "files_touched", FilesTouched);
        AppendList(sb, "open_issues", OpenIssues);
        AppendList(sb, "next_actions", NextActions);
        AppendList(sb, "errors_resolved", ErrorsResolved);
        return sb.ToString().TrimEnd();
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, CompactionJson.Options);

    public static SemanticCompactionSummary FromJson(string json) =>
        JsonSerializer.Deserialize<SemanticCompactionSummary>(json, CompactionJson.Options)
        ?? new SemanticCompactionSummary([], [], [], [], []);

    private static void AppendList(StringBuilder sb, string key, IReadOnlyList<string> items)
    {
        sb.Append(key).Append(':');
        if (items.Count == 0)
        {
            sb.AppendLine(" (none)");
            return;
        }

        foreach (var item in items)
            sb.AppendLine().Append("  - ").Append(item);
        sb.AppendLine();
    }
}

internal static class CompactionJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
