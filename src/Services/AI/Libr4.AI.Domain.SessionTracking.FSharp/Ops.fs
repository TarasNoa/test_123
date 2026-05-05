namespace Libr4.AI.Domain.SessionTracking.FSharp

open System
open System.Security.Cryptography
open System.Text
open Microsoft.Data.Sqlite

module SessionOps =
    let generateSessionId (projectPath: string) : string =
        use sha256 = SHA256.Create()
        let bytes = Encoding.UTF8.GetBytes(projectPath)
        let hash = sha256.ComputeHash(bytes)
        BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 16)

    let createSessionDB (dbPath: string) : unit =
        use connection = new SqliteConnection($"Data Source={dbPath}")
        connection.Open()
        
        let createTables = """
            CREATE TABLE IF NOT EXISTS sessions (
                id TEXT PRIMARY KEY,
                project_path TEXT NOT NULL,
                user_id TEXT,
                agent_id TEXT,
                created_at TEXT NOT NULL,
                last_accessed_at TEXT NOT NULL,
                message_count INTEGER DEFAULT 0
            );
            
            CREATE TABLE IF NOT EXISTS session_events (
                id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                data TEXT NOT NULL,
                metadata TEXT NOT NULL,
                FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
            );
            
            CREATE INDEX IF NOT EXISTS idx_session_events_session_id ON session_events(session_id);
            CREATE INDEX IF NOT EXISTS idx_session_events_timestamp ON session_events(timestamp);
            
            CREATE VIRTUAL TABLE IF NOT EXISTS session_events_fts USING fts5(
                session_id, event_type, data, metadata,
                content='session_events',
                content_rowid='rowid'
            );
            
            CREATE TRIGGER IF NOT EXISTS session_events_fts_insert AFTER INSERT ON session_events BEGIN
                INSERT INTO session_events_fts(rowid, session_id, event_type, data, metadata)
                VALUES (new.rowid, new.session_id, new.event_type, new.data, new.metadata);
            END;
            
            CREATE TRIGGER IF NOT EXISTS session_events_fts_delete AFTER DELETE ON session_events BEGIN
                DELETE FROM session_events_fts WHERE rowid = old.rowid;
            END;
            
            CREATE TRIGGER IF NOT EXISTS session_events_fts_update AFTER UPDATE ON session_events BEGIN
                DELETE FROM session_events_fts WHERE rowid = old.rowid;
                INSERT INTO session_events_fts(rowid, session_id, event_type, data, metadata)
                VALUES (new.rowid, new.session_id, new.event_type, new.data, new.metadata);
            END;
        """
        
        use command = new SqliteCommand(createTables, connection)
        command.ExecuteNonQuery() |> ignore

    let getSession (dbPath: string) (sessionId: string) : Session option =
        use connection = new SqliteConnection($"Data Source={dbPath}")
        connection.Open()
        
        let query = "SELECT id, project_path, user_id, agent_id, created_at, last_accessed_at, message_count FROM sessions WHERE id = @id"
        use command = new SqliteCommand(query, connection)
        command.Parameters.AddWithValue("@id", sessionId) |> ignore
        
        use reader = command.ExecuteReader()
        if reader.Read() then
            let userId = if reader.IsDBNull(2) then None else Some reader.GetString(2)
            let agentIdStr = if reader.IsDBNull(3) then None else Some reader.GetString(3)
            let agentId = agentIdStr |> Option.map Guid.Parse
            
            Some {
                id = reader.GetString(0)
                projectPath = reader.GetString(1)
                userId = userId
                agentId = agentId
                createdAt = DateTimeOffset.Parse(reader.GetString(4))
                lastAccessedAt = DateTimeOffset.Parse(reader.GetString(5))
                messageCount = reader.GetInt32(6)
            }
        else
            None

    let createOrUpdateSession (dbPath: string) (session: Session) : unit =
        use connection = new SqliteConnection($"Data Source={dbPath}")
        connection.Open()
        
        let query = """
            INSERT INTO sessions (id, project_path, user_id, agent_id, created_at, last_accessed_at, message_count)
            VALUES (@id, @project_path, @user_id, @agent_id, @created_at, @last_accessed_at, @message_count)
            ON CONFLICT(id) DO UPDATE SET
                last_accessed_at = @last_accessed_at,
                message_count = @message_count
        """
        
        use command = new SqliteCommand(query, connection)
        command.Parameters.AddWithValue("@id", session.id) |> ignore
        command.Parameters.AddWithValue("@project_path", session.projectPath) |> ignore
        command.Parameters.AddWithValue("@user_id", if session.userId.IsSome then box session.userId.Value else box DBNull.Value) |> ignore
        command.Parameters.AddWithValue("@agent_id", if session.agentId.IsSome then box (session.agentId.Value.ToString()) else box DBNull.Value) |> ignore
        command.Parameters.AddWithValue("@created_at", session.createdAt.ToString("o")) |> ignore
        command.Parameters.AddWithValue("@last_accessed_at", session.lastAccessedAt.ToString("o")) |> ignore
        command.Parameters.AddWithValue("@message_count", session.messageCount) |> ignore
        
        command.ExecuteNonQuery() |> ignore

    let addSessionEvent (dbPath: string) (event: SessionEvent) : unit =
        use connection = new SqliteConnection($"Data Source={dbPath}")
        connection.Open()
        
        let metadataJson = event.metadata |> Seq.map (fun kvp -> $"\"{kvp.Key}\":\"{kvp.Value}\"") |> String.concat "," |> sprintf "{%s}"
        
        let query = """
            INSERT INTO session_events (id, session_id, event_type, timestamp, data, metadata)
            VALUES (@id, @session_id, @event_type, @timestamp, @data, @metadata)
        """
        
        use command = new SqliteCommand(query, connection)
        command.Parameters.AddWithValue("@id", event.id.ToString()) |> ignore
        command.Parameters.AddWithValue("@session_id", event.sessionId) |> ignore
        command.Parameters.AddWithValue("@event_type", event.eventType.ToString()) |> ignore
        command.Parameters.AddWithValue("@timestamp", event.timestamp.ToString("o")) |> ignore
        command.Parameters.AddWithValue("@data", event.data) |> ignore
        command.Parameters.AddWithValue("@metadata", metadataJson) |> ignore
        
        command.ExecuteNonQuery() |> ignore

    let searchSessions (dbPath: string) (query: string) (topK: int) : SessionSearchResult list =
        use connection = new SqliteConnection($"Data Source={dbPath}")
        connection.Open()
        
        let ftsQuery = """
            SELECT se.session_id, s.project_path, s.last_accessed_at, bm25(session_events_fts) as score
            FROM session_events_fts se
            JOIN sessions s ON se.session_id = s.id
            WHERE session_events_fts MATCH @query
            ORDER BY score
            LIMIT @limit
        """
        
        use command = new SqliteCommand(ftsQuery, connection)
        command.Parameters.AddWithValue("@query", query) |> ignore
        command.Parameters.AddWithValue("@limit", topK) |> ignore
        
        use reader = command.ExecuteReader()
        let results = ref []
        
        while reader.Read() do
            results := {
                sessionId = reader.GetString(0)
                projectPath = reader.GetString(1)
                lastAccessedAt = DateTimeOffset.Parse(reader.GetString(2))
                relevanceScore = reader.GetDouble(3)
            } :: !results
        
        List.rev !results

    let getLastMessages (dbPath: string) (sessionId: string) (limit: int) : SessionEvent list =
        use connection = new SqliteConnection($"Data Source={dbPath}")
        connection.Open()
        
        let query = """
            SELECT id, session_id, event_type, timestamp, data, metadata
            FROM session_events
            WHERE session_id = @session_id AND event_type IN ('ToolResult', 'Message')
            ORDER BY timestamp DESC
            LIMIT @limit
        """
        
        use command = new SqliteCommand(query, connection)
        command.Parameters.AddWithValue("@session_id", sessionId) |> ignore
        command.Parameters.AddWithValue("@limit", limit) |> ignore
        
        use reader = command.ExecuteReader()
        let results = ref []
        
        while reader.Read() do
            let eventTypeStr = reader.GetString(2)
            let eventType = 
                match eventTypeStr with
                | "ToolUse" -> ToolUse
                | "ToolResult" -> ToolResult
                | "Message" -> Message
                | "SessionStart" -> SessionStart
                | "SessionEnd" -> SessionEnd
                | _ -> Message
            
            let metadataStr = reader.GetString(5)
            let metadata = 
                try
                    // Simple JSON parsing for metadata
                    if metadataStr.StartsWith("{") && metadataStr.EndsWith("}") then
                        metadataStr.Substring(1, metadataStr.Length - 2)
                            .Split(',')
                            |> Array.map (fun pair ->
                                let parts = pair.Split(':')
                                if parts.Length = 2 then
                                    let key = parts.[0].Trim().Trim('"')
                                    let value = parts.[1].Trim().Trim('"')
                                    (key, value)
                                else
                                    ("", "")
                            )
                            |> Map.ofArray
                    else
                        Map.empty
                with _ ->
                    Map.empty
            
            results := {
                id = Guid.Parse(reader.GetString(0))
                sessionId = reader.GetString(1)
                eventType = eventType
                timestamp = DateTimeOffset.Parse(reader.GetString(3))
                data = reader.GetString(4)
                metadata = metadata
            } :: !results
        
        List.rev !results

    // C# interop helper functions
    let updateSessionLastAccessed (session: Session) (lastAccessedAt: DateTimeOffset) : Session =
        { session with lastAccessedAt = lastAccessedAt }

    let createSession (sessionId: string) (projectPath: string) (userId: string option) (agentId: Guid option) (createdAt: DateTimeOffset) (messageCount: int) : Session =
        {
            id = sessionId
            projectPath = projectPath
            userId = userId
            agentId = agentId
            createdAt = createdAt
            lastAccessedAt = createdAt
            messageCount = messageCount
        }

    let createSessionEvent (id: Guid) (sessionId: string) (eventType: SessionEventType) (timestamp: DateTimeOffset) (data: string) (metadata: Dictionary<string, string>) : SessionEvent =
        {
            id = id
            sessionId = sessionId
            eventType = eventType
            timestamp = timestamp
            data = data
            metadata = metadata |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq
        }
