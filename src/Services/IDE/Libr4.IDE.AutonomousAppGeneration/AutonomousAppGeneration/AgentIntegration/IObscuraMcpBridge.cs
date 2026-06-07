namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IObscuraMcpBridge
{
    bool CanHandle(string toolName, McpExecutionOptions options);

    Task<McpInvocationOutcome> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        Guid? runId,
        CancellationToken ct = default);
}
