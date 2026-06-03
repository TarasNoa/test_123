namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Raised when autonomous generation cannot proceed without substituting synthetic LLM output.
/// Callers should mark the run failed and surface <see cref="Stage"/> + message to operators.
/// </summary>
public sealed class AutonomousGenerationFailedException : Exception
{
    public AutonomousGenerationFailedException(string stage, string message, Exception? innerException = null)
        : base(FormatMessage(stage, message), innerException)
    {
        Stage = stage;
    }

    public string Stage { get; }

    private static string FormatMessage(string stage, string message) =>
        string.IsNullOrWhiteSpace(stage) ? message : $"[{stage}] {message}";
}
