using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace Libr4.AI.Infrastructure.MCP;

/// <summary>
/// C# implementation of Agent Bridge MCP Server.
/// Replaces Python version with native .NET implementation.
/// Supports stdio transport for MCP protocol.
/// </summary>
public sealed class AgentBridgeMcpServer : IDisposable
{
    private readonly BridgeStore _store;
    private readonly string? _channelPath;
    private readonly ILogger<AgentBridgeMcpServer> _logger;
    private bool _running;

    public AgentBridgeMcpServer(
        string dbPath,
        string? channelPath = null,
        int lockStaleSeconds = 0,
        ILogger<AgentBridgeMcpServer>? logger = null)
    {
        _store = new BridgeStore(dbPath, lockStaleSeconds);
        _channelPath = channelPath;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AgentBridgeMcpServer>.Instance;
    }

    /// <summary>
    /// Run the MCP server over stdio.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _running = true;
        _logger.LogInformation("Agent Bridge MCP Server started");

        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin, Encoding.UTF8);
        using var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };

        // Send initialization notification
        await SendJsonAsync(writer, new { jsonrpc = "2.0", id = 0, result = new { protocolVersion = "2024-11-05", capabilities = new { }, serverInfo = new { name = "libr4-agent-bridge", version = "1.0.0" } } }, ct);

        while (_running && !ct.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var request = JsonSerializer.Deserialize<McpRequest>(line);
                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                await SendJsonAsync(writer, response, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request");
            }
        }

        _logger.LogInformation("Agent Bridge MCP Server stopped");
    }

    private async Task<object> HandleRequestAsync(McpRequest request)
    {
        try
        {
            var method = request.Method;
            var args = request.Params ?? new Dictionary<string, JsonElement>();

            object? result = method switch
            {
                "tools/list" => GetToolSchema(),
                "tools/call" => await HandleToolCallAsync(args),
                _ => new { error = $"Unknown method: {method}" }
            };

            return new { jsonrpc = "2.0", id = request.Id, result };
        }
        catch (Exception ex)
        {
            return new { jsonrpc = "2.0", id = request.Id, error = new { code = -32603, message = ex.Message } };
        }
    }

    private async Task<object> HandleToolCallAsync(Dictionary<string, JsonElement> args)
    {
        var toolName = args["name"].GetString();
        var toolArgs = args["arguments"];

        return toolName switch
        {
            "send_message" => await HandleSendMessageAsync(toolArgs),
            "read_messages" => await HandleReadMessagesAsync(toolArgs),
            "ack_message" => await HandleAckMessageAsync(toolArgs),
            "reserve_task" => await HandleReserveTaskAsync(toolArgs),
            "release_task" => await HandleReleaseTaskAsync(toolArgs),
            "heartbeat_task" => await HandleHeartbeatTaskAsync(toolArgs),
            "list_task_locks" => await HandleListTaskLocksAsync(),
            _ => new { content = new[] { new { type = "text", text = $"Unknown tool: {toolName}" } } }
        };
    }

    private async Task<object> HandleSendMessageAsync(JsonElement args)
    {
        var sender = args.GetProperty("sender").GetString()!;
        var recipient = args.GetProperty("recipient").GetString()!;
        var msgType = args.GetProperty("type").GetString()!;
        var goal = args.GetProperty("goal").GetString()!;
        var update = args.GetProperty("update").GetString()!;
        var artifacts = args.GetProperty("artifacts").GetString()!;
        var nextStep = args.GetProperty("next_step").GetString()!;

        var messageId = await Task.Run(() => _store.AddMessage(
            sender, recipient, msgType, goal, update, artifacts, nextStep));

        // Mirror to channel file
        if (!string.IsNullOrEmpty(_channelPath))
        {
            await Task.Run(() => AppendToChannel(
                _channelPath, sender, recipient, msgType, goal, update, artifacts, nextStep));
        }

        _logger.LogDebug("Message {MessageId} sent from {Sender} to {Recipient}", messageId, sender, recipient);

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(new { message_id = messageId, status = "sent" }) }
            }
        };
    }

    private async Task<object> HandleReadMessagesAsync(JsonElement args)
    {
        var recipient = args.GetProperty("recipient").GetString()!;
        var includeAcked = args.TryGetProperty("include_acked", out var ackedProp) && ackedProp.GetBoolean();
        var limit = args.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 100;
        var offset = args.TryGetProperty("offset", out var offsetProp) ? offsetProp.GetInt32() : 0;

        var messages = await Task.Run(() => _store.ReadMessages(recipient, includeAcked, limit, offset));

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(new { count = messages.Count, messages }) }
            }
        };
    }

    private async Task<object> HandleAckMessageAsync(JsonElement args)
    {
        var messageId = args.GetProperty("id").GetInt32();
        var ackedBy = args.GetProperty("acked_by").GetString()!;

        var success = await Task.Run(() => _store.AckMessage(messageId, ackedBy));

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(new { acked = success, message_id = messageId }) }
            }
        };
    }

    private async Task<object> HandleReserveTaskAsync(JsonElement args)
    {
        var taskId = args.GetProperty("task_id").GetString()!;
        var reservedBy = args.GetProperty("reserved_by").GetString()!;
        var priority = args.TryGetProperty("priority", out var prioProp) ? prioProp.GetInt32() : 0;
        var note = args.TryGetProperty("note", out var noteProp) ? noteProp.GetString() : "";

        var result = await Task.Run(() => _store.ReserveTask(taskId, reservedBy, priority, note ?? ""));

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(result) }
            }
        };
    }

    private async Task<object> HandleReleaseTaskAsync(JsonElement args)
    {
        var taskId = args.GetProperty("task_id").GetString()!;
        var releasedBy = args.GetProperty("released_by").GetString()!;

        var result = await Task.Run(() => _store.ReleaseTask(taskId, releasedBy));

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(result) }
            }
        };
    }

    private async Task<object> HandleHeartbeatTaskAsync(JsonElement args)
    {
        var taskId = args.GetProperty("task_id").GetString()!;
        var owner = args.GetProperty("owner").GetString()!;

        var result = await Task.Run(() => _store.HeartbeatTask(taskId, owner));

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(result) }
            }
        };
    }

    private async Task<object> HandleListTaskLocksAsync()
    {
        var locks = await Task.Run(() => _store.ListTaskLocks());

        return new
        {
            content = new[]
            {
                new { type = "text", text = JsonSerializer.Serialize(new { count = locks.Count, locks }) }
            }
        };
    }

    private static object GetToolSchema()
    {
        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "send_message",
                    description = "Send message to another agent and mirror to channel file.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            sender = new { type = "string" },
                            recipient = new { type = "string" },
                            type = new { type = "string" },
                            goal = new { type = "string" },
                            update = new { type = "string" },
                            artifacts = new { type = "string" },
                            next_step = new { type = "string" }
                        },
                        required = new[] { "sender", "recipient", "type", "goal", "update", "artifacts", "next_step" }
                    }
                },
                new
                {
                    name = "read_messages",
                    description = "Read queued messages for recipient.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            recipient = new { type = "string" },
                            include_acked = new { type = "boolean" },
                            limit = new { type = "integer" },
                            offset = new { type = "integer" }
                        },
                        required = new[] { "recipient" }
                    }
                },
                new
                {
                    name = "ack_message",
                    description = "Mark message as acknowledged.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "integer" },
                            acked_by = new { type = "string" }
                        },
                        required = new[] { "id", "acked_by" }
                    }
                },
                new
                {
                    name = "reserve_task",
                    description = "Reserve task ownership to avoid duplicate work.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            task_id = new { type = "string" },
                            reserved_by = new { type = "string" },
                            priority = new { type = "integer" },
                            note = new { type = "string" }
                        },
                        required = new[] { "task_id", "reserved_by" }
                    }
                },
                new
                {
                    name = "release_task",
                    description = "Release task ownership lock.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            task_id = new { type = "string" },
                            released_by = new { type = "string" }
                        },
                        required = new[] { "task_id", "released_by" }
                    }
                },
                new
                {
                    name = "heartbeat_task",
                    description = "Update task heartbeat to keep lock alive during long work.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            task_id = new { type = "string" },
                            owner = new { type = "string" }
                        },
                        required = new[] { "task_id", "owner" }
                    }
                },
                new
                {
                    name = "list_task_locks",
                    description = "List all active task locks with priorities.",
                    inputSchema = new { type = "object", properties = new { } }
                }
            }
        };
    }

    private static void AppendToChannel(
        string channelPath,
        string sender,
        string recipient,
        string msgType,
        string goal,
        string updateText,
        string artifacts,
        string nextStep)
    {
        var dir = Path.GetDirectoryName(channelPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var block = $"""
[{UtcNow()}] [{sender}] TYPE={msgType}
To: {recipient}
Goal:
{goal}
Request/Update:
{updateText}
Artifacts/Paths:
{artifacts}
Next step:
{nextStep}

""";

        File.AppendAllText(channelPath, block, Encoding.UTF8);
    }

    private static async Task SendJsonAsync(StreamWriter writer, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data);
        await writer.WriteLineAsync(json.ToCharArray(), ct);
    }

    private static string UtcNow() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    public void Dispose()
    {
        _store?.Dispose();
    }

    private sealed class McpRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("params")]
        public Dictionary<string, JsonElement>? Params { get; set; }
    }
}

/// <summary>
/// SQLite storage for bridge messages and task locks.
/// </summary>
public sealed class BridgeStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly int _lockStaleSeconds;

    public BridgeStore(string dbPath, int lockStaleSeconds = 0)
    {
        _lockStaleSeconds = Math.Max(0, lockStaleSeconds);
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitSchema();
    }

    private void InitSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS bridge_messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                ts_utc TEXT NOT NULL,
                sender TEXT NOT NULL,
                recipient TEXT NOT NULL,
                type TEXT NOT NULL,
                goal TEXT NOT NULL,
                update_text TEXT NOT NULL,
                artifacts TEXT NOT NULL,
                next_step TEXT NOT NULL,
                acked_by TEXT,
                acked_at_utc TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_messages_recipient ON bridge_messages(recipient);
            CREATE INDEX IF NOT EXISTS idx_messages_acked ON bridge_messages(acked_by);
            
            CREATE TABLE IF NOT EXISTS task_locks (
                task_id TEXT PRIMARY KEY,
                reserved_by TEXT NOT NULL,
                priority INTEGER NOT NULL,
                note TEXT NOT NULL,
                reserved_at_utc TEXT NOT NULL,
                heartbeat_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_locks_priority ON task_locks(priority DESC, reserved_at_utc ASC);
        """;
        cmd.ExecuteNonQuery();
    }

    public int PurgeStaleLocks()
    {
        if (_lockStaleSeconds <= 0) return 0;

        var cutoff = DateTime.UtcNow.AddSeconds(-_lockStaleSeconds).ToString("yyyy-MM-ddTHH:mm:ssZ");
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT task_id, heartbeat_at_utc FROM task_locks";

        var toDelete = new List<string>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var hb = reader.GetString(1);
                if (string.Compare(hb, cutoff) < 0)
                {
                    toDelete.Add(reader.GetString(0));
                }
            }
        }

        foreach (var taskId in toDelete)
        {
            using var delCmd = _connection.CreateCommand();
            delCmd.CommandText = "DELETE FROM task_locks WHERE task_id = @taskId";
            delCmd.Parameters.AddWithValue("@taskId", taskId);
            delCmd.ExecuteNonQuery();
        }

        return toDelete.Count;
    }

    public long AddMessage(string sender, string recipient, string msgType, string goal, string updateText, string artifacts, string nextStep)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO bridge_messages (
                ts_utc, sender, recipient, type, goal, update_text, artifacts, next_step
            ) VALUES (@ts, @sender, @recipient, @type, @goal, @update, @artifacts, @nextStep);
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("@ts", UtcNow());
        cmd.Parameters.AddWithValue("@sender", sender);
        cmd.Parameters.AddWithValue("@recipient", recipient);
        cmd.Parameters.AddWithValue("@type", msgType);
        cmd.Parameters.AddWithValue("@goal", goal);
        cmd.Parameters.AddWithValue("@update", updateText);
        cmd.Parameters.AddWithValue("@artifacts", artifacts);
        cmd.Parameters.AddWithValue("@nextStep", nextStep);

        return (long)cmd.ExecuteScalar()!;
    }

    public List<Dictionary<string, object?>> ReadMessages(string recipient, bool includeAcked, int limit, int offset)
    {
        PurgeStaleLocks();

        using var cmd = _connection.CreateCommand();
        var whereClause = "recipient = @recipient";
        if (!includeAcked)
        {
            whereClause += " AND acked_by IS NULL";
        }

        cmd.CommandText = $@"
            SELECT * FROM bridge_messages
            WHERE {whereClause}
            ORDER BY id DESC
            LIMIT @limit OFFSET @offset
        ";
        cmd.Parameters.AddWithValue("@recipient", recipient);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);

        var results = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return results;
    }

    public bool AckMessage(int messageId, string ackedBy)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE bridge_messages
            SET acked_by = @ackedBy, acked_at_utc = @ts
            WHERE id = @id AND acked_by IS NULL
        """;
        cmd.Parameters.AddWithValue("@ackedBy", ackedBy);
        cmd.Parameters.AddWithValue("@ts", UtcNow());
        cmd.Parameters.AddWithValue("@id", messageId);

        return cmd.ExecuteNonQuery() > 0;
    }

    public Dictionary<string, object> ReserveTask(string taskId, string reservedBy, int priority, string note)
    {
        PurgeStaleLocks();

        // Check if already reserved
        using var checkCmd = _connection.CreateCommand();
        checkCmd.CommandText = "SELECT reserved_by FROM task_locks WHERE task_id = @taskId";
        checkCmd.Parameters.AddWithValue("@taskId", taskId);
        var existing = checkCmd.ExecuteScalar();

        if (existing != null)
        {
            return new Dictionary<string, object>
            {
                ["reserved"] = false,
                ["reason"] = "already_reserved",
                ["owner"] = existing
            };
        }

        var now = UtcNow();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO task_locks (
                task_id, reserved_by, priority, note, reserved_at_utc, heartbeat_at_utc
            ) VALUES (@taskId, @reservedBy, @priority, @note, @ts, @ts)
        """;
        cmd.Parameters.AddWithValue("@taskId", taskId);
        cmd.Parameters.AddWithValue("@reservedBy", reservedBy);
        cmd.Parameters.AddWithValue("@priority", priority);
        cmd.Parameters.AddWithValue("@note", note);
        cmd.Parameters.AddWithValue("@ts", now);
        cmd.ExecuteNonQuery();

        return new Dictionary<string, object>
        {
            ["reserved"] = true,
            ["task_id"] = taskId,
            ["owner"] = reservedBy
        };
    }

    public Dictionary<string, object> ReleaseTask(string taskId, string releasedBy)
    {
        using var checkCmd = _connection.CreateCommand();
        checkCmd.CommandText = "SELECT reserved_by FROM task_locks WHERE task_id = @taskId";
        checkCmd.Parameters.AddWithValue("@taskId", taskId);
        var existing = checkCmd.ExecuteScalar();

        if (existing == null)
        {
            return new Dictionary<string, object> { ["released"] = false, ["reason"] = "not_found" };
        }

        if ((string)existing != releasedBy)
        {
            return new Dictionary<string, object> { ["released"] = false, ["reason"] = "not_owner", ["owner"] = existing };
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM task_locks WHERE task_id = @taskId";
        cmd.Parameters.AddWithValue("@taskId", taskId);
        cmd.ExecuteNonQuery();

        return new Dictionary<string, object> { ["released"] = true, ["task_id"] = taskId };
    }

    public Dictionary<string, object> HeartbeatTask(string taskId, string owner)
    {
        using var checkCmd = _connection.CreateCommand();
        checkCmd.CommandText = "SELECT reserved_by FROM task_locks WHERE task_id = @taskId";
        checkCmd.Parameters.AddWithValue("@taskId", taskId);
        var existing = checkCmd.ExecuteScalar();

        if (existing == null)
        {
            return new Dictionary<string, object> { ["updated"] = false, ["reason"] = "not_found" };
        }

        if ((string)existing != owner)
        {
            return new Dictionary<string, object> { ["updated"] = false, ["reason"] = "not_owner", ["owner"] = existing };
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE task_locks SET heartbeat_at_utc = @ts WHERE task_id = @taskId";
        cmd.Parameters.AddWithValue("@ts", UtcNow());
        cmd.Parameters.AddWithValue("@taskId", taskId);
        cmd.ExecuteNonQuery();

        return new Dictionary<string, object> { ["updated"] = true, ["task_id"] = taskId };
    }

    public List<Dictionary<string, object?>> ListTaskLocks()
    {
        PurgeStaleLocks();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM task_locks ORDER BY priority DESC, reserved_at_utc ASC";

        var results = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return results;
    }

    private static string UtcNow() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
