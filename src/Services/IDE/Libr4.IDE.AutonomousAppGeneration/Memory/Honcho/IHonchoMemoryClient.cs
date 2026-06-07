namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public sealed record HonchoChatRequest(
    string UserId,
    string SessionId,
    string Query,
    string? ReasoningLevel = null);

public sealed record HonchoChatResult(string Content, bool FromRemote);

public sealed record HonchoRunSyncRequest(
    string UserId,
    string ProjectKey,
    string SessionId,
    string UserMessage,
    string AssistantMessage,
    string DialecticQuery);

public interface IHonchoMemoryClient
{
    bool IsRemoteEnabled { get; }

    Task EnsurePeerAsync(string peerId, CancellationToken ct = default);

    Task EnsureSessionAsync(string sessionId, CancellationToken ct = default);

    Task AppendMessagesAsync(
        string sessionId,
        string userPeerId,
        string agentPeerId,
        string userMessage,
        string assistantMessage,
        CancellationToken ct = default);

    Task<HonchoChatResult> ChatAsync(HonchoChatRequest request, CancellationToken ct = default);
}
