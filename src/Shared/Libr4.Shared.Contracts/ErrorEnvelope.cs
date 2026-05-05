namespace Libr4.Shared.Contracts;

/// <summary>
/// Standard error envelope for all HTTP responses.
/// Ensures consistent error handling across all endpoints.
/// </summary>
public sealed record ErrorEnvelope(
    string Code,
    string Message,
    object? Details = null);

/// <summary>
/// Standard error codes for the system.
/// </summary>
public static class ErrorCodes
{
    // General errors
    public const string InternalError = "INTERNAL_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string Conflict = "CONFLICT";
    public const string BadRequest = "BAD_REQUEST";

    // Autonomous generation specific errors
    public const string GenerationFailed = "GENERATION_FAILED";
    public const string GenerationTimeout = "GENERATION_TIMEOUT";
    public const string QualityGateFailed = "QUALITY_GATE_FAILED";
    public const string MaxIterationsExceeded = "MAX_ITERATIONS_EXCEEDED";
    public const string OrchestratorError = "ORCHESTRATOR_ERROR";
    public const string AgentError = "AGENT_ERROR";
    public const string LlmError = "LLM_ERROR";
    public const string RuntimeError = "RUNTIME_ERROR";
    public const string CompilationError = "COMPILATION_ERROR";
    public const string TestError = "TEST_ERROR";
}
