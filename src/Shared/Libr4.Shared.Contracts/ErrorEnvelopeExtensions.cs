namespace Libr4.Shared.Contracts;

/// <summary>
/// Extension methods for creating error envelopes.
/// </summary>
public static class ErrorEnvelopeExtensions
{
    /// <summary>
    /// Creates a standard error envelope from an exception.
    /// </summary>
    public static ErrorEnvelope ToErrorEnvelope(this Exception ex, string? customCode = null, object? details = null)
    {
        var code = customCode ?? InferErrorCode(ex);
        var message = ex.Message;
        
        return new ErrorEnvelope(code, message, details ?? new
        {
            Type = ex.GetType().Name,
            StackTrace = ex.StackTrace
        });
    }

    /// <summary>
    /// Infers an error code from an exception type.
    /// </summary>
    private static string InferErrorCode(Exception ex)
    {
        return ex.GetType().Name switch
        {
            nameof(TimeoutException) => ErrorCodes.GenerationTimeout,
            nameof(OperationCanceledException) => ErrorCodes.GenerationTimeout,
            nameof(ArgumentException) => ErrorCodes.ValidationError,
            nameof(ArgumentNullException) => ErrorCodes.ValidationError,
            nameof(UnauthorizedAccessException) => ErrorCodes.Unauthorized,
            _ when ex.Message.Contains("quality gate", StringComparison.OrdinalIgnoreCase) => ErrorCodes.QualityGateFailed,
            _ when ex.Message.Contains("generation", StringComparison.OrdinalIgnoreCase) => ErrorCodes.GenerationFailed,
            _ when ex.Message.Contains("orchestrator", StringComparison.OrdinalIgnoreCase) => ErrorCodes.OrchestratorError,
            _ when ex.Message.Contains("agent", StringComparison.OrdinalIgnoreCase) => ErrorCodes.AgentError,
            _ when ex.Message.Contains("LLM", StringComparison.OrdinalIgnoreCase) => ErrorCodes.LlmError,
            _ when ex.Message.Contains("runtime", StringComparison.OrdinalIgnoreCase) => ErrorCodes.RuntimeError,
            _ when ex.Message.Contains("compilation", StringComparison.OrdinalIgnoreCase) => ErrorCodes.CompilationError,
            _ when ex.Message.Contains("test", StringComparison.OrdinalIgnoreCase) => ErrorCodes.TestError,
            _ => ErrorCodes.InternalError
        };
    }

    /// <summary>
    /// Creates a validation error envelope.
    /// </summary>
    public static ErrorEnvelope ValidationError(string message, object? details = null)
    {
        return new ErrorEnvelope(ErrorCodes.ValidationError, message, details);
    }

    /// <summary>
    /// Creates a not found error envelope.
    /// </summary>
    public static ErrorEnvelope NotFound(string message, object? details = null)
    {
        return new ErrorEnvelope(ErrorCodes.NotFound, message, details);
    }

    /// <summary>
    /// Creates a generation failed error envelope.
    /// </summary>
    public static ErrorEnvelope GenerationFailed(string message, object? details = null)
    {
        return new ErrorEnvelope(ErrorCodes.GenerationFailed, message, details);
    }

    /// <summary>
    /// Creates a quality gate failed error envelope.
    /// </summary>
    public static ErrorEnvelope QualityGateFailed(string message, object? details = null)
    {
        return new ErrorEnvelope(ErrorCodes.QualityGateFailed, message, details);
    }
}
