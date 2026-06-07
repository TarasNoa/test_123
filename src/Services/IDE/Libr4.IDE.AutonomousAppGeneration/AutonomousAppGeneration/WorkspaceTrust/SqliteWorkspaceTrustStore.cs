using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public sealed class SqliteWorkspaceTrustStore : IWorkspaceTrustStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteWorkspaceTrustStore> _logger;

    public SqliteWorkspaceTrustStore(IOptions<WorkspaceTrustOptions> options, ILogger<SqliteWorkspaceTrustStore> logger)
    {
        var path = options.Value.DbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS workspace_trust (
              workspace_hash TEXT PRIMARY KEY,
              sandbox_policy TEXT NOT NULL,
              host_mode TEXT NOT NULL,
              decided_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_workspace_trust_decided ON workspace_trust(decided_at_utc DESC);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<WorkspaceTrustRecord?> GetAsync(string workspaceHash, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT workspace_hash, sandbox_policy, host_mode, decided_at_utc
            FROM workspace_trust WHERE workspace_hash = $hash
            """;
        cmd.Parameters.AddWithValue("$hash", workspaceHash);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return ReadRecord(reader);
    }

    public async Task UpsertAsync(WorkspaceTrustRecord record, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO workspace_trust(workspace_hash, sandbox_policy, host_mode, decided_at_utc)
            VALUES ($hash, $sandbox, $host, $decided)
            ON CONFLICT(workspace_hash) DO UPDATE SET
              sandbox_policy = excluded.sandbox_policy,
              host_mode = excluded.host_mode,
              decided_at_utc = excluded.decided_at_utc
            """;
        cmd.Parameters.AddWithValue("$hash", record.WorkspaceHash);
        cmd.Parameters.AddWithValue("$sandbox", record.SandboxPolicy.ToString());
        cmd.Parameters.AddWithValue("$host", record.HostMode.ToString());
        cmd.Parameters.AddWithValue("$decided", record.DecidedAtUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Persisted workspace trust for {Hash}: sandbox={Sandbox}, host={Host}",
            record.WorkspaceHash,
            record.SandboxPolicy,
            record.HostMode);
    }

    private static WorkspaceTrustRecord ReadRecord(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            Enum.Parse<WorkspaceSandboxPolicy>(reader.GetString(1), ignoreCase: true),
            Enum.Parse<WorkspaceHostMode>(reader.GetString(2), ignoreCase: true),
            DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind));

    private SqliteConnection Open() => new(_connectionString);
}
