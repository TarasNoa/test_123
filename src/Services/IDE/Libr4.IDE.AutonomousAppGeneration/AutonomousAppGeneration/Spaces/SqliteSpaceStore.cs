using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class SqliteSpaceStore : ISpaceStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSpaceStore> _logger;

    public SqliteSpaceStore(IOptions<AgentSpaceOptions> options, ILogger<SqliteSpaceStore> logger)
    {
        var path = options.Value.StoreDbPath;
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
            CREATE TABLE IF NOT EXISTS agent_spaces (
              space_id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              repository_url TEXT,
              base_branch TEXT NOT NULL,
              owner_id TEXT NOT NULL,
              shared_memory_scope TEXT NOT NULL,
              mcp_profile TEXT,
              created_at_utc TEXT NOT NULL,
              root_path TEXT NOT NULL,
              integration_branch TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS space_members (
              member_id TEXT NOT NULL,
              space_id TEXT NOT NULL,
              role TEXT NOT NULL,
              run_id TEXT,
              worktree_path TEXT NOT NULL,
              branch_name TEXT NOT NULL,
              status TEXT NOT NULL,
              created_at_utc TEXT NOT NULL,
              updated_at_utc TEXT NOT NULL,
              last_error TEXT,
              PRIMARY KEY (space_id, member_id),
              FOREIGN KEY (space_id) REFERENCES agent_spaces(space_id)
            );
            CREATE INDEX IF NOT EXISTS ix_space_members_status ON space_members(space_id, status);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Agent spaces schema ensured");
    }

    public async Task InsertSpaceAsync(AgentSpace space, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO agent_spaces(
              space_id, name, repository_url, base_branch, owner_id, shared_memory_scope,
              mcp_profile, created_at_utc, root_path, integration_branch)
            VALUES ($id, $name, $repo, $base, $owner, $scope, $mcp, $created, $root, $integration)
            """;
        BindSpace(cmd, space);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<AgentSpace?> GetSpaceAsync(Guid spaceId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM agent_spaces WHERE space_id = $id";
        cmd.Parameters.AddWithValue("$id", spaceId.ToString("D"));
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadSpace(reader) : null;
    }

    public async Task<IReadOnlyList<AgentSpace>> ListSpacesAsync(string? ownerId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            cmd.CommandText = "SELECT * FROM agent_spaces ORDER BY created_at_utc DESC";
        }
        else
        {
            cmd.CommandText = "SELECT * FROM agent_spaces WHERE owner_id = $owner ORDER BY created_at_utc DESC";
            cmd.Parameters.AddWithValue("$owner", ownerId);
        }

        var list = new List<AgentSpace>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            list.Add(ReadSpace(reader));
        return list;
    }

    public async Task InsertMemberAsync(SpaceMember member, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO space_members(
              member_id, space_id, role, run_id, worktree_path, branch_name,
              status, created_at_utc, updated_at_utc, last_error)
            VALUES ($mid, $sid, $role, $run, $path, $branch, $status, $created, $updated, $err)
            """;
        BindMember(cmd, member);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateMemberAsync(SpaceMember member, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE space_members SET
              role = $role, run_id = $run, worktree_path = $path, branch_name = $branch,
              status = $status, updated_at_utc = $updated, last_error = $err
            WHERE space_id = $sid AND member_id = $mid
            """;
        BindMember(cmd, member);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<SpaceMember?> GetMemberAsync(Guid spaceId, string memberId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM space_members WHERE space_id = $sid AND member_id = $mid";
        cmd.Parameters.AddWithValue("$sid", spaceId.ToString("D"));
        cmd.Parameters.AddWithValue("$mid", memberId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? ReadMember(reader) : null;
    }

    public async Task<IReadOnlyList<SpaceMember>> ListMembersAsync(Guid spaceId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM space_members WHERE space_id = $sid ORDER BY created_at_utc DESC";
        cmd.Parameters.AddWithValue("$sid", spaceId.ToString("D"));
        var list = new List<SpaceMember>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            list.Add(ReadMember(reader));
        return list;
    }

    public async Task<IReadOnlyList<SpaceMember>> ListMembersByRunIdAsync(Guid runId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM space_members WHERE run_id = $run ORDER BY updated_at_utc DESC";
        cmd.Parameters.AddWithValue("$run", runId.ToString("D"));
        var list = new List<SpaceMember>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            list.Add(ReadMember(reader));
        return list;
    }

    public async Task<int> CountActiveMembersAsync(Guid spaceId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM space_members
            WHERE space_id = $sid AND status IN ('Queued', 'Running')
            """;
        cmd.Parameters.AddWithValue("$sid", spaceId.ToString("D"));
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return Convert.ToInt32(result);
    }

    private SqliteConnection Open() => new(_connectionString);

    private static void BindSpace(SqliteCommand cmd, AgentSpace space)
    {
        cmd.Parameters.AddWithValue("$id", space.SpaceId.ToString("D"));
        cmd.Parameters.AddWithValue("$name", space.Name);
        cmd.Parameters.AddWithValue("$repo", (object?)space.RepositoryUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$base", space.BaseBranch);
        cmd.Parameters.AddWithValue("$owner", space.OwnerId);
        cmd.Parameters.AddWithValue("$scope", space.SharedMemoryScope);
        cmd.Parameters.AddWithValue("$mcp", (object?)space.McpProfile ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", space.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$root", space.RootPath);
        cmd.Parameters.AddWithValue("$integration", space.IntegrationBranch);
    }

    private static void BindMember(SqliteCommand cmd, SpaceMember member)
    {
        cmd.Parameters.AddWithValue("$mid", member.MemberId);
        cmd.Parameters.AddWithValue("$sid", member.SpaceId.ToString("D"));
        cmd.Parameters.AddWithValue("$role", member.Role.ToString());
        cmd.Parameters.AddWithValue("$run", member.RunId?.ToString("D") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$path", member.WorktreePath);
        cmd.Parameters.AddWithValue("$branch", member.BranchName);
        cmd.Parameters.AddWithValue("$status", member.Status.ToString());
        cmd.Parameters.AddWithValue("$created", member.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", member.UpdatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$err", (object?)member.LastError ?? DBNull.Value);
    }

    private static AgentSpace ReadSpace(SqliteDataReader reader) =>
        new(
            SpaceId: Guid.Parse(reader.GetString(reader.GetOrdinal("space_id"))),
            Name: reader.GetString(reader.GetOrdinal("name")),
            RepositoryUrl: reader.IsDBNull(reader.GetOrdinal("repository_url")) ? null : reader.GetString(reader.GetOrdinal("repository_url")),
            BaseBranch: reader.GetString(reader.GetOrdinal("base_branch")),
            OwnerId: reader.GetString(reader.GetOrdinal("owner_id")),
            SharedMemoryScope: reader.GetString(reader.GetOrdinal("shared_memory_scope")),
            McpProfile: reader.IsDBNull(reader.GetOrdinal("mcp_profile")) ? null : reader.GetString(reader.GetOrdinal("mcp_profile")),
            CreatedAtUtc: DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at_utc"))).ToUniversalTime(),
            RootPath: reader.GetString(reader.GetOrdinal("root_path")),
            IntegrationBranch: reader.GetString(reader.GetOrdinal("integration_branch")));

    private static SpaceMember ReadMember(SqliteDataReader reader)
    {
        var runOrdinal = reader.GetOrdinal("run_id");
        Guid? runId = reader.IsDBNull(runOrdinal) ? null : Guid.Parse(reader.GetString(runOrdinal));
        var errOrdinal = reader.GetOrdinal("last_error");
        return new SpaceMember(
            MemberId: reader.GetString(reader.GetOrdinal("member_id")),
            SpaceId: Guid.Parse(reader.GetString(reader.GetOrdinal("space_id"))),
            Role: Enum.Parse<SpaceMemberRole>(reader.GetString(reader.GetOrdinal("role"))),
            RunId: runId,
            WorktreePath: reader.GetString(reader.GetOrdinal("worktree_path")),
            BranchName: reader.GetString(reader.GetOrdinal("branch_name")),
            Status: Enum.Parse<SpaceMemberStatus>(reader.GetString(reader.GetOrdinal("status"))),
            CreatedAtUtc: DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at_utc"))).ToUniversalTime(),
            UpdatedAtUtc: DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at_utc"))).ToUniversalTime(),
            LastError: reader.IsDBNull(errOrdinal) ? null : reader.GetString(errOrdinal));
    }
}
