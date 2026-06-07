using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed record FleetRunQualityBreakdown(
    int Score,
    int VerifyPoints,
    int PatchPoints,
    int PlaybookPoints,
    int ReviewPoints,
    int PatchCount,
    double? ReviewMinutes);

public static class FleetRunQualityCalculator
{
    private static readonly JsonSerializerOptions ReviewJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static FleetRunQualityBreakdown Compute(
        AppGenerationOrchestrator? run,
        string runDir,
        RunPlaybookStats playbook)
    {
        var verifyPoints = ScoreVerify(run);
        var patchCount = CountPatchToolUses(runDir);
        var patchPoints = ScorePatches(patchCount);
        var playbookPoints = ScorePlaybook(playbook);
        var (reviewPoints, reviewMinutes) = ScoreReview(runDir);
        var score = Math.Clamp(verifyPoints + patchPoints + playbookPoints + reviewPoints, 0, 100);

        return new FleetRunQualityBreakdown(
            score,
            verifyPoints,
            patchPoints,
            playbookPoints,
            reviewPoints,
            patchCount,
            reviewMinutes);
    }

    private static int ScoreVerify(AppGenerationOrchestrator? run)
    {
        if (run is null)
            return 0;

        var verifyGate = run.QualityGates
            .LastOrDefault(g => g.Stage.Equals("verify_subagent", StringComparison.OrdinalIgnoreCase));
        if (verifyGate is null)
            return 10;

        if (verifyGate.Passed)
            return 35;

        return verifyGate.Score >= 7 ? 15 : 0;
    }

    private static int CountPatchToolUses(string runDir)
    {
        var rolloutPath = Path.Combine(runDir, "rollout.jsonl");
        if (!File.Exists(rolloutPath))
            return 0;

        var count = 0;
        foreach (var line in File.ReadLines(rolloutPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "tool_use")
                    continue;

                var toolName = root.TryGetProperty("toolName", out var toolEl)
                    ? toolEl.GetString()
                    : null;
                if (IsPatchTool(toolName))
                    count++;
            }
            catch
            {
                // skip malformed lines
            }
        }

        return count;
    }

    private static bool IsPatchTool(string? toolName) =>
        !string.IsNullOrWhiteSpace(toolName)
        && (toolName.Contains("apply_patch", StringComparison.OrdinalIgnoreCase)
            || toolName.Contains("write_file", StringComparison.OrdinalIgnoreCase)
            || toolName.Contains("edit_file", StringComparison.OrdinalIgnoreCase));

    private static int ScorePatches(int patchCount) =>
        patchCount switch
        {
            <= 5 => 25,
            <= 12 => 20,
            <= 25 => 12,
            <= 40 => 6,
            _ => 0
        };

    private static int ScorePlaybook(RunPlaybookStats playbook)
    {
        if (playbook.Attempts <= 0)
            return 10;

        return (int)Math.Round(playbook.HitRate * 20, MidpointRounding.AwayFromZero);
    }

    private static (int Points, double? Minutes) ScoreReview(string runDir)
    {
        var path = Path.Combine(runDir, "review", "decisions.jsonl");
        if (!File.Exists(path))
            return (20, null);

        var entries = LoadReviewEntries(path);
        if (entries.Count == 0)
            return (20, null);

        var first = entries.Min(e => e.TimestampUtc);
        var last = entries.Max(e => e.TimestampUtc);
        var minutes = Math.Max(0, (last - first).TotalMinutes);

        var paths = entries.Select(e => e.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var latestByPath = entries
            .GroupBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.TimestampUtc).Last(), StringComparer.OrdinalIgnoreCase);

        var allApproved = paths.Count > 0 && paths.All(p =>
        {
            var decision = latestByPath[p].Decision;
            return decision is ReviewDecision.Approve or ReviewDecision.ApproveWithNotes;
        });

        if (!allApproved)
            return (5, minutes);

        var points = minutes switch
        {
            <= 5 => 20,
            <= 30 => 15,
            <= 120 => 10,
            _ => 5
        };

        return (points, minutes);
    }

    private static List<ReviewDecisionAuditEntry> LoadReviewEntries(string path)
    {
        var entries = new List<ReviewDecisionAuditEntry>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var entry = JsonSerializer.Deserialize<ReviewDecisionAuditEntry>(line, ReviewJsonOptions);
                if (entry is not null)
                    entries.Add(entry);
            }
            catch
            {
                // skip malformed lines
            }
        }

        return entries;
    }
}
