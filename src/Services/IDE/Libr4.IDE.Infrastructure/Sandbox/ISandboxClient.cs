namespace Libr4.IDE.Infrastructure.Sandbox;

/// <summary>
/// Interface for sandbox code execution clients
/// </summary>
public interface ISandboxClient
{
    /// <summary>
    /// Execute code in the sandbox
    /// </summary>
    Task<SandboxExecutionResult> ExecuteAsync(
        string code,
        string language = "csharp",
        int timeoutSeconds = 30,
        int memoryLimitMb = 512,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if sandbox is healthy
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
