namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Entity for inter-agent communication
/// </summary>
public class AgentCommunication
{
    public Guid Id { get; private set; }
    public Guid FromAgentId { get; private set; }
    public Guid ToAgentId { get; private set; }
    public string Message { get; private set; }
    public string MessageType { get; private set; }
    public DateTime SentAt { get; private set; }
    
    private AgentCommunication() { }
    
    public AgentCommunication(
        Guid fromAgentId,
        Guid toAgentId,
        string message,
        string messageType = "notification")
    {
        Id = Guid.NewGuid();
        FromAgentId = fromAgentId;
        ToAgentId = toAgentId;
        Message = message;
        MessageType = messageType;
        SentAt = DateTime.UtcNow;
    }
    
    public static AgentCommunication Create(
        Guid fromAgentId,
        Guid toAgentId,
        string message,
        string messageType = "notification")
    {
        return new AgentCommunication(fromAgentId, toAgentId, message, messageType);
    }
}
