using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed record RunPlaybookStats(int Attempts, int Hits)
{
    public double HitRate => Attempts > 0 ? (double)Hits / Attempts : 0;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static RunPlaybookStats Read(string runDir)
    {
        var path = StatsPath(runDir);
        if (!File.Exists(path))
            return new RunPlaybookStats(0, 0);

        try
        {
            var doc = JsonSerializer.Deserialize<RunPlaybookStatsDto>(File.ReadAllText(path));
            return new RunPlaybookStats(Math.Max(0, doc?.Attempts ?? 0), Math.Max(0, doc?.Hits ?? 0));
        }
        catch
        {
            return new RunPlaybookStats(0, 0);
        }
    }

    public static void RecordAttempt(string runDir)
    {
        var stats = Read(runDir);
        Write(runDir, stats with { Attempts = stats.Attempts + 1 });
    }

    public static void RecordHit(string runDir)
    {
        var stats = Read(runDir);
        Write(runDir, stats with { Hits = stats.Hits + 1 });
    }

    private static void Write(string runDir, RunPlaybookStats stats)
    {
        Directory.CreateDirectory(runDir);
        var payload = new RunPlaybookStatsDto(stats.Attempts, stats.Hits, DateTime.UtcNow);
        File.WriteAllText(StatsPath(runDir), JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static string StatsPath(string runDir) =>
        Path.Combine(runDir, "playbook-stats.json");

    private sealed record RunPlaybookStatsDto(int Attempts, int Hits, DateTime UpdatedAtUtc);
}
