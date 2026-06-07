using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;

public sealed class SqliteAgentSpecProposalStore : IAgentSpecProposalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _connectionString;

    public SqliteAgentSpecProposalStore(IOptions<AgentSpecEvolutionOptions> options)
    {
        var path = options.Value.ProposalsDbPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
    }

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS agent_spec_proposals (
              id TEXT PRIMARY KEY,
              run_id TEXT NOT NULL,
              spec_name TEXT NOT NULL,
              diff_json TEXT NOT NULL,
              rationale TEXT NOT NULL,
              status TEXT NOT NULL,
              created_at_utc TEXT NOT NULL,
              resolved_at_utc TEXT,
              resolved_by TEXT,
              rejection_reason TEXT,
              applied_version INTEGER
            );
            CREATE INDEX IF NOT EXISTS ix_agent_spec_proposals_status ON agent_spec_proposals(status);
            CREATE INDEX IF NOT EXISTS ix_agent_spec_proposals_run ON agent_spec_proposals(run_id);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task InsertAsync(AgentSpecProposal proposal, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_spec_proposals(
              id, run_id, spec_name, diff_json, rationale, status,
              created_at_utc, resolved_at_utc, resolved_by, rejection_reason, applied_version)
            VALUES ($id, $run, $spec, $diff, $rat, $status, $created, $resolved, $by, $reject, $ver)
            """;
        Bind(cmd, proposal);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<AgentSpecProposal?> GetAsync(Guid proposalId, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_spec_proposals WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", proposalId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<AgentSpecProposal>> ListAsync(
        AgentSpecProposalStatus? status = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = status is null
            ? "SELECT * FROM agent_spec_proposals ORDER BY created_at_utc DESC"
            : "SELECT * FROM agent_spec_proposals WHERE status = $status ORDER BY created_at_utc DESC";
        if (status is not null)
            cmd.Parameters.AddWithValue("$status", status.Value.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var list = new List<AgentSpecProposal>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            list.Add(Read(reader));
        return list;
    }

    public async Task UpdateStatusAsync(
        Guid proposalId,
        AgentSpecProposalStatus status,
        string? resolvedBy = null,
        string? rejectionReason = null,
        int? appliedVersion = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct).ConfigureAwait(false);
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE agent_spec_proposals
            SET status = $status,
                resolved_at_utc = $resolved,
                resolved_by = $by,
                rejection_reason = $reject,
                applied_version = $ver
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", proposalId.ToString("D"));
        cmd.Parameters.AddWithValue("$status", status.ToString());
        cmd.Parameters.AddWithValue("$resolved", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$by", (object?)resolvedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$reject", (object?)rejectionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ver", (object?)appliedVersion ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static void Bind(SqliteCommand cmd, AgentSpecProposal proposal)
    {
        cmd.Parameters.AddWithValue("$id", proposal.Id.ToString("D"));
        cmd.Parameters.AddWithValue("$run", proposal.RunId.ToString("D"));
        cmd.Parameters.AddWithValue("$spec", proposal.SpecName);
        cmd.Parameters.AddWithValue("$diff", JsonSerializer.Serialize(proposal.Diff, JsonOptions));
        cmd.Parameters.AddWithValue("$rat", proposal.Rationale);
        cmd.Parameters.AddWithValue("$status", proposal.Status.ToString());
        cmd.Parameters.AddWithValue("$created", proposal.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$resolved", proposal.ResolvedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$by", (object?)proposal.ResolvedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$reject", (object?)proposal.RejectionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ver", (object?)proposal.AppliedVersion ?? DBNull.Value);
    }

    private static AgentSpecProposal Read(SqliteDataReader reader)
    {
        var diffJson = reader.GetString(reader.GetOrdinal("diff_json"));
        var diff = JsonSerializer.Deserialize<AgentSpecProposalDiff>(diffJson, JsonOptions)
                   ?? new AgentSpecProposalDiff();

        return new AgentSpecProposal(
            Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Guid.Parse(reader.GetString(reader.GetOrdinal("run_id"))),
            reader.GetString(reader.GetOrdinal("spec_name")),
            diff,
            reader.GetString(reader.GetOrdinal("rationale")),
            Enum.Parse<AgentSpecProposalStatus>(reader.GetString(reader.GetOrdinal("status"))),
            DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(reader.GetOrdinal("resolved_at_utc"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("resolved_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(reader.GetOrdinal("resolved_by")) ? null : reader.GetString(reader.GetOrdinal("resolved_by")),
            reader.IsDBNull(reader.GetOrdinal("rejection_reason")) ? null : reader.GetString(reader.GetOrdinal("rejection_reason")),
            reader.IsDBNull(reader.GetOrdinal("applied_version")) ? null : reader.GetInt32(reader.GetOrdinal("applied_version")));
    }

    private SqliteConnection Open() => new(_connectionString);
}
