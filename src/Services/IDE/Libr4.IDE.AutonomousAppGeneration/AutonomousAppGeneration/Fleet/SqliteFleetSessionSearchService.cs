using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetSessionSearchService
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task IndexAsync(FleetSessionIndexDocument document, CancellationToken ct = default);
    Task RemoveAsync(Guid runId, CancellationToken ct = default);
    Task<FleetSessionSearchResult> SearchAsync(FleetSessionSearchQuery query, CancellationToken ct = default);
    Task RebuildFromFleetIndexAsync(CancellationToken ct = default);
}

public sealed class SqliteFleetSessionSearchService : IFleetSessionSearchService
{
    private readonly string _connectionString;
    private readonly IAgentFleetIndexStore _fleetIndex;
    private readonly ILogger<SqliteFleetSessionSearchService> _logger;

    public SqliteFleetSessionSearchService(
        IOptions<AgentFleetOptions> options,
        IAgentFleetIndexStore fleetIndex,
        ILogger<SqliteFleetSessionSearchService> logger)
    {
        var path = options.Value.IndexDbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        _fleetIndex = fleetIndex;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS fleet_session_fts USING fts5(
              run_id UNINDEXED,
              body,
              stack UNINDEXED,
              outcome UNINDEXED,
              space_id UNINDEXED,
              date_bucket UNINDEXED,
              last_activity_unix UNINDEXED,
              pinned UNINDEXED
            );
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task IndexAsync(FleetSessionIndexDocument document, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        var body = BuildBody(document);
        var dateBucket = ToDateBucket(document.LastActivityAtUtc);
        var runId = document.RunId.ToString("D");

        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var delete = conn.CreateCommand();
        delete.CommandText = "DELETE FROM fleet_session_fts WHERE run_id = $run";
        delete.Parameters.AddWithValue("$run", runId);
        await delete.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        await using var insert = conn.CreateCommand();
        insert.CommandText = """
            INSERT INTO fleet_session_fts(
              run_id, body, stack, outcome, space_id, date_bucket, last_activity_unix, pinned)
            VALUES ($run, $body, $stack, $outcome, $space, $bucket, $last, $pinned)
            """;
        insert.Parameters.AddWithValue("$run", runId);
        insert.Parameters.AddWithValue("$body", body);
        insert.Parameters.AddWithValue("$stack", document.StackTags ?? string.Empty);
        insert.Parameters.AddWithValue("$outcome", document.Outcome);
        insert.Parameters.AddWithValue("$space", document.SpaceName ?? string.Empty);
        insert.Parameters.AddWithValue("$bucket", dateBucket);
        insert.Parameters.AddWithValue("$last", new DateTimeOffset(document.LastActivityAtUtc).ToUnixTimeSeconds());
        insert.Parameters.AddWithValue("$pinned", document.Pinned ? 1 : 0);
        await insert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(Guid runId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM fleet_session_fts WHERE run_id = $run";
        cmd.Parameters.AddWithValue("$run", runId.ToString("D"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<FleetSessionSearchResult> SearchAsync(FleetSessionSearchQuery query, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new FleetSessionSearchResult(
                query.Query,
                0,
                new FleetSessionSearchFacets([], [], []),
                []);
        }

        var ftsQuery = ToFtsQuery(query.Query);
        var hits = new List<FleetSessionSearchHit>();
        await using (var conn = Open())
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT run_id,
                       snippet(fleet_session_fts, 1, '', '', '…', 48) AS snippet,
                       bm25(fleet_session_fts) AS score,
                       stack,
                       outcome,
                       space_id,
                       last_activity_unix,
                       pinned
                FROM fleet_session_fts
                WHERE fleet_session_fts MATCH $q
                  AND ($stack = '' OR stack = $stack)
                  AND ($outcome = '' OR outcome = $outcome)
                  AND ($space = '' OR space_id = $space)
                  AND ($bucket = '' OR date_bucket = $bucket)
                ORDER BY pinned DESC, score
                LIMIT $limit
                """;
            cmd.Parameters.AddWithValue("$q", ftsQuery);
            cmd.Parameters.AddWithValue("$stack", query.Stack ?? string.Empty);
            cmd.Parameters.AddWithValue("$outcome", query.Outcome ?? string.Empty);
            cmd.Parameters.AddWithValue("$space", query.SpaceId ?? string.Empty);
            cmd.Parameters.AddWithValue("$bucket", query.DateBucket ?? string.Empty);
            cmd.Parameters.AddWithValue("$limit", Math.Clamp(query.Limit, 1, 200));

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var runId = Guid.Parse(reader.GetString(0));
                var snippet = reader.GetString(1);
                var score = reader.GetDouble(2);
                var stack = reader.IsDBNull(3) ? null : reader.GetString(3);
                var outcome = reader.IsDBNull(4) ? null : reader.GetString(4);
                var spaceId = reader.IsDBNull(5) ? null : reader.GetString(5);
                var lastUnix = reader.GetInt64(6);
                var pinned = reader.GetInt64(7) == 1;

                var entry = await _fleetIndex.GetAsync(runId, ct).ConfigureAwait(false);
                hits.Add(new FleetSessionSearchHit(
                    runId,
                    entry?.Title ?? $"Run {runId.ToString()[..8]}",
                    entry?.Status ?? AgentFleetStatus.Queued,
                    stack ?? entry?.Stack,
                    string.IsNullOrWhiteSpace(spaceId) ? entry?.SpaceId : spaceId,
                    snippet,
                    score,
                    DateTimeOffset.FromUnixTimeSeconds(lastUnix).UtcDateTime,
                    pinned));
            }
        }

        var facets = await LoadFacetsAsync(ct).ConfigureAwait(false);
        return new FleetSessionSearchResult(query.Query, hits.Count, facets, hits);
    }

    public async Task RebuildFromFleetIndexAsync(CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using (var clear = conn.CreateCommand())
        {
            clear.CommandText = "DELETE FROM fleet_session_fts";
            await clear.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        var entries = await _fleetIndex.ListAsync(new AgentFleetListQuery(IncludeArchived: true, Limit: 500), ct)
            .ConfigureAwait(false);
        foreach (var entry in entries)
        {
            await IndexAsync(new FleetSessionIndexDocument(
                entry.RunId,
                entry.Title,
                UserRequest: null,
                ErrorSignature: entry.FailureReason,
                FilesTouched: null,
                SpaceName: entry.SpaceId,
                StackTags: entry.Stack,
                Outcome: ToOutcome(entry.Status),
                entry.LastActivityAtUtc,
                entry.Pinned), ct).ConfigureAwait(false);
        }

        _logger.LogInformation("Rebuilt fleet session FTS index with {Count} documents", entries.Count);
    }

    private async Task<FleetSessionSearchFacets> LoadFacetsAsync(CancellationToken ct)
    {
        var stacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outcomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var buckets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT stack, outcome, date_bucket
            FROM fleet_session_fts
            WHERE stack != '' OR outcome != '' OR date_bucket != ''
            LIMIT 500
            """;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (!reader.IsDBNull(0) && !string.IsNullOrWhiteSpace(reader.GetString(0)))
                stacks.Add(reader.GetString(0));
            if (!reader.IsDBNull(1) && !string.IsNullOrWhiteSpace(reader.GetString(1)))
                outcomes.Add(reader.GetString(1));
            if (!reader.IsDBNull(2) && !string.IsNullOrWhiteSpace(reader.GetString(2)))
                buckets.Add(reader.GetString(2));
        }

        return new FleetSessionSearchFacets(
            stacks.OrderBy(x => x).ToList(),
            outcomes.OrderBy(x => x).ToList(),
            buckets.OrderBy(x => x).ToList());
    }

    internal static string BuildBody(FleetSessionIndexDocument document)
    {
        var parts = new List<string>
        {
            document.Title,
            document.UserRequest ?? string.Empty,
            document.ErrorSignature ?? string.Empty,
            document.FilesTouched ?? string.Empty,
            document.SpaceName ?? string.Empty,
            document.StackTags ?? string.Empty
        };
        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    internal static string ToFtsQuery(string raw)
    {
        var tokens = raw.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return raw;

        return string.Join(' ', tokens.Select(t => $"\"{t.Replace("\"", "\"\"")}\""));
    }

    internal static string ToOutcome(AgentFleetStatus status) =>
        status is AgentFleetStatus.Completed or AgentFleetStatus.HandoffComplete ? "pass"
        : status is AgentFleetStatus.Failed or AgentFleetStatus.Cancelled ? "fail"
        : "running";

    internal static string ToDateBucket(DateTime utc) =>
        utc.Date == DateTime.UtcNow.Date ? "today"
        : utc.Date >= DateTime.UtcNow.Date.AddDays(-7) ? "week"
        : utc.Date >= DateTime.UtcNow.Date.AddDays(-30) ? "month"
        : "older";

    private SqliteConnection Open() => new(_connectionString);
}
