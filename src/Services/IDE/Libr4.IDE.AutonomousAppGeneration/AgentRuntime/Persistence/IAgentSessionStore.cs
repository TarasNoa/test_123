using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;

public interface IAgentSessionStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task<AgentSessionRecord> CreateSessionAsync(AgentSessionRecord session, CancellationToken ct = default);
    Task<AgentSessionRecord?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task UpdateSessionAsync(AgentSessionRecord session, CancellationToken ct = default);
    Task AppendMessageAsync(AgentMessageRecord message, CancellationToken ct = default);
    Task<IReadOnlyList<AgentMessageRecord>> GetMessagesAsync(string sessionId, CancellationToken ct = default);
    Task AppendToolCallAsync(AgentToolCallRecord toolCall, CancellationToken ct = default);
    Task SaveCheckpointAsync(AgentCheckpointRecord checkpoint, CancellationToken ct = default);
    Task<AgentCheckpointRecord?> GetCheckpointAsync(string checkpointId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentCheckpointRecord>> ListCheckpointsAsync(string sessionId, CancellationToken ct = default);
    Task<AgentSessionRecord?> GetLatestSessionByRunIdAsync(Guid runId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentToolCallRecord>> GetToolCallsAsync(string sessionId, CancellationToken ct = default);
}

public interface IAgentSessionResumeService
{
    Task<AgentSessionResumeBundle?> LoadResumeBundleAsync(string sessionId, CancellationToken ct = default);
    Task SaveTurnAsync(
        string sessionId,
        int stepNumber,
        AgentConversationTurn turn,
        AgentToolCallRecord? toolCall,
        CancellationToken ct = default);
}

public sealed record AgentSessionResumeBundle(
    AgentSessionRecord Session,
    IReadOnlyList<AgentConversationTurn> Turns,
    int NextStepNumber);
