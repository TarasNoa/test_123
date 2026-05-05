# Backend Integration Plan

This document outlines the remaining work needed to fully implement the backend endpoints and features for the IDE frontend.

---

## Status Summary

### ✅ Completed (Stubs Created)
1. **Translation API endpoint** - `TranslationEndpoints.cs` created with stub
2. **Terminal API endpoints** - `TerminalEndpoints.cs` created with stub
3. **User profile languages field** - `User.cs` updated with `Languages` field
4. **WebSocket handler** - `TerminalWebSocketHandler.cs` created and registered

### ⚠️ Pending Implementation
1. Translation service implementation
2. Terminal session management
3. Agent reasoning/thinking output
4. Build/test/security event hooks
5. Agent orchestration tracking

---

## 1. Translation API Implementation

### File: `Libr4.IDE.Api/TranslationEndpoints.cs`

**Current Status:** Stub implementation returns input as-is.

**Required Implementation:**

```csharp
// TODO: Implement translation
// - Call translation service (OpenAI, DeepL, or custom)
// - Support multiple languages
// - Return translated items
```

**Implementation Steps:**

1. Create translation service interface and implementation:
   ```csharp
   public interface ITranslationService
   {
       Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken ct);
       Task<BatchTranslationResult> TranslateBatchAsync(string[] texts, string targetLanguage, CancellationToken ct);
   }
   ```

2. Integrate with translation provider:
   - Option A: OpenAI GPT-4 for translation
   - Option B: DeepL API
   - Option C: LibreTranslate (self-hosted)
   - Option D: Azure Translator

3. Add caching layer for translations (Redis or in-memory)

4. Register service in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<ITranslationService, OpenAITranslationService>();
   ```

---

## 2. Terminal API Implementation

### File: `Libr4.IDE.Api/TerminalEndpoints.cs`

**Current Status:** Stub implementation returns mock data.

**Required Implementation:**

### 2.1 Terminal Session Management

Create database entities:
```csharp
public class TerminalSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ShellType Shell { get; set; }
    public string WorkingDirectory { get; set; }
    public SessionStatus Status { get; set; }
    public int Rows { get; set; }
    public int Cols { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset? TerminatedAt { get; set; }
}
```

### 2.2 Command Execution

Integrate with shadow workspace execution:
- Use Docker exec to run commands in shadow workspace container
- Capture stdout/stderr
- Track exit codes and execution time
- Store command history

### 2.3 Shadow Workspace Integration

The terminal needs to execute commands in the shadow workspace environment.

**Implementation Options:**

1. **Docker-based** (recommended):
   ```csharp
   var result = await _dockerClient.Exec.CreateContainerCommandAsync(containerId, new ContainerExecCreateParameters
   {
       Cmd = new[] { "/bin/bash", "-c", command },
       AttachStdout = true,
       AttachStderr = true,
   });
   
   await _dockerClient.Exec.StartContainerCommandAsync(result.ID);
   var output = await _dockerClient.Exec.InspectContainerCommandAsync(result.ID);
   ```

2. **Process-based** (for local development):
   - Spawn shell process
   - Capture output streams
   - Manage process lifecycle

### 2.4 WebSocket Real-time Output

**Current Status:** `TerminalWebSocketHandler.cs` created with basic echo functionality.

**Required Enhancement:**

1. Connect WebSocket to actual terminal process output
2. Stream output as it's generated (not just at end)
3. Handle ANSI escape codes for formatting
4. Support multiple clients per session

---

## 3. Agent Reasoning/Thinking Output

### Goal
Enable AI agents to output their reasoning process (thoughts) like Cursor/Windsurf.

### Implementation Locations

#### 3.1 LLM Prompt Updates

**File:** `LlmAppPlannerService.cs` (and other LLM services)

**Current System Prompt:** No reasoning/thinking instruction.

**Add to System Prompt:**
```
====================== REASONING OUTPUT ======================
Before providing your final JSON output, include a <thinking> section
with your step-by-step reasoning process. This helps users understand
your decision-making.

Example:
<thinking>
1. User wants a web app with React and Node.js
2. Need to plan frontend (React) and backend (Node.js/Express)
3. PostgreSQL for database
4. Docker for deployment
5. Frontend needs build step, backend needs npm install
6. Testing: Jest for frontend, Mocha for backend
</thinking>

The <thinking> section will be extracted and shown to the user separately.
```

#### 3.2 Response Parsing

Create utility to extract thinking from LLM response:
```csharp
public static class ThinkingExtractor
{
    public static (string? thinking, string content) ExtractThinking(string response)
    {
        var thinkingMatch = Regex.Match(response, @"<thinking>(.*?)</thinking>", RegexOptions.Singleline);
        if (thinkingMatch.Success)
        {
            var thinking = thinkingMatch.Groups[1].Value.Trim();
            var content = response.Replace(thinkingMatch.Value, "").Trim();
            return (thinking, content);
        }
        return (null, response);
    }
}
```

#### 3.3 Message Type Update

When returning messages to frontend, include `thinking` field and set `type = 'thinking'`:
```csharp
return new ChatMessage
{
    id = Guid.NewGuid(),
    role = "assistant",
    content = jsonOutput,
    type = "thinking",
    thinking = extractedThinking,
    timestamp = DateTime.UtcNow,
};
```

---

## 4. Build/Test/Security Event Hooks

### Goal
Emit events when agent performs build, test, or security operations.

### Implementation

#### 4.1 Event Types

Create event domain:
```csharp
public enum AgentEventType
{
    BuildStart,
    BuildComplete,
    TestStart,
    TestComplete,
    SecurityScanStart,
    SecurityScanComplete,
    TerminalOutput,
}

public class AgentEvent
{
    public Guid Id { get; set; }
    public AgentEventType Type { get; set; }
    public Guid RunId { get; set; }
    public string? Command { get; set; }
    public string? Output { get; set; }
    public int? ExitCode { get; set; }
    public long? DurationMs { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

#### 4.2 Event Emitter Service

```csharp
public interface IAgentEventEmitter
{
    Task EmitBuildStartAsync(Guid runId, string command);
    Task EmitBuildCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs);
    Task EmitTestStartAsync(Guid runId, string command);
    Task EmitTestCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs);
    Task EmitSecurityScanAsync(Guid runId, string command, string output, int exitCode, long durationMs);
    Task EmitTerminalOutputAsync(Guid runId, string command, string output);
}
```

#### 4.3 Integration Points

**AutonomousAppGeneration orchestrator:**
- Before executing build commands → `EmitBuildStartAsync`
- After build completes → `EmitBuildCompleteAsync`
- Before running tests → `EmitTestStartAsync`
- After tests complete → `EmitTestCompleteAsync`
- Before security scan → `EmitSecurityScanStartAsync`
- After security scan → `EmitSecurityScanCompleteAsync`

**File:** `StartAppGenerationCommandHandler.cs` - Add event emission at appropriate points.

#### 4.4 Event Delivery to Frontend

Options:
1. **WebSocket** - Push events to connected clients
2. **Server-Sent Events (SSE)** - Stream events to frontend
3. **Polling** - Frontend queries for new events (less ideal)

**Recommended:** WebSocket for real-time delivery.

---

## 5. Agent Orchestration Tracking

### Goal
Track which agent is called, which sub-agents it uses, and their purposes.

### Implementation

#### 5.1 Agent Hierarchy Domain

```csharp
public class AgentInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string? Description { get; set; }
    public AgentStatus Status { get; set; }
    public List<AgentInfo> SubAgents { get; set; } = new();
    public string? Purpose { get; set; }
    public string? Input { get; set; }
    public string? Output { get; set; }
}

public enum AgentStatus
{
    Idle,
    Working,
    Completed,
    Failed
}

public class AgentOrchestrationEvent
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public AgentInfo RootAgent { get; set; }
    public string? TriggeredBy { get; set; } // "LLM", "user", "system"
    public DateTimeOffset Timestamp { get; set; }
}
```

#### 5.2 Orchestration Tracking Service

```csharp
public interface IAgentOrchestrationTracker
{
    Task StartAgentCallAsync(Guid runId, AgentInfo agent, string triggeredBy);
    Task AddSubAgentAsync(Guid runId, Guid parentAgentId, AgentInfo subAgent);
    Task CompleteAgentAsync(Guid runId, Guid agentId, string? output);
    Task FailAgentAsync(Guid runId, Guid agentId, string error);
    Task<AgentOrchestrationEvent?> GetOrchestrationAsync(Guid runId);
}
```

#### 5.3 Integration with Multi-Agent System

**File:** `MultiAgentOrchestrationEndpoints.cs` or related orchestrator

When an agent is invoked:
```csharp
await _tracker.StartAgentCallAsync(runId, new AgentInfo
{
    Name = "CodeGenerationAgent",
    Role = "Generator",
    Purpose = "Generate code for frontend components",
    Input = prompt,
    Status = AgentStatus.Working
}, "LLM");
```

When sub-agents are called:
```csharp
await _tracker.AddSubAgentAsync(runId, parentId, new AgentInfo
{
    Name = "CodeReviewAgent",
    Role = "Reviewer",
    Purpose = "Review generated code for quality",
    Status = AgentStatus.Working
});
```

#### 5.4 Delivery to Frontend

Include in chat messages:
```csharp
return new ChatMessage
{
    id = Guid.NewGuid(),
    role = "assistant",
    content = "Agent execution complete",
    type = "agent-call",
    agentOrchestration = new AgentOrchestrationEvent { ... },
    timestamp = DateTime.UtcNow,
};
```

---

## 6. Database Schema Updates

### Required Tables/Entities

1. **TerminalSessions**
   - Id, UserId, Shell, WorkingDirectory, Status, Rows, Cols, CreatedAt, LastActivityAt, TerminatedAt

2. **CommandHistory**
   - Id, SessionId, Command, Output, ExitCode, DurationMs, ExecutedAt

3. **AgentEvents**
   - Id, RunId, Type, Command, Output, ExitCode, DurationMs, Timestamp

4. **AgentOrchestrations**
   - Id, RunId, RootAgentJson, TriggeredBy, Timestamp
   - (Use JSON column for nested agent hierarchy)

### Migration Scripts

Create EF Core migrations or SQL scripts to add these tables.

---

## 7. Dependencies to Add

### NuGet Packages

1. **Docker.DotNet** - For Docker-based terminal execution
2. **StackExchange.Redis** - For translation caching
3. **System.Reactive** - For event streaming (optional)
4. **Microsoft.AspNetCore.SignalR** - Alternative to raw WebSocket (optional)

---

## 8. Testing Strategy

### Unit Tests
- Translation service
- Command parsing
- Thinking extraction
- Event emission

### Integration Tests
- Terminal command execution in Docker
- WebSocket connection
- Event delivery

### E2E Tests
- Full flow: user request → agent planning → build → test → event emission → frontend display

---

## 9. Priority Order

1. **High Priority**
   - Terminal API implementation (Docker-based)
   - WebSocket real-time output
   - Translation service integration

2. **Medium Priority**
   - Agent reasoning output (prompt updates)
   - Build/test/security event hooks
   - Agent orchestration tracking

3. **Low Priority**
   - Advanced caching
   - Event replay
   - Analytics dashboard

---

## 10. Notes

- All endpoints currently return stub/mock data
- TODO comments in code indicate where implementation is needed
- Frontend is fully implemented and waiting for backend
- Consider using existing infrastructure (e.g., shadow workspace) for terminal execution
- WebSocket can be replaced with SignalR for easier management
