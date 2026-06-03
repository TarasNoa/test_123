namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class SecurityReviewGateOptions
{
    /// <summary>Minimum score (0-10) required to pass the security review stage.</summary>
    public int MinScore { get; set; } = 7;

    /// <summary>
    /// <c>llm</c> — DeepSeek/OpenRouter security agent (default).
    /// <c>deterministic</c> — legacy regex gate (tests only).
    /// </summary>
    public string Mode { get; set; } = "llm";

    /// <summary>Optional model override; null uses the host AI default (e.g. OpenRouter DeepSeek).</summary>
    public string? Model { get; set; }

    public int MaxFilesToReview { get; set; } = 48;

    public int MaxCharsPerFile { get; set; } = 3500;

    public int MaxTotalPromptChars { get; set; } = 120_000;

    /// <summary>LLM fix passes after a failed review before deferring to the build/fix loop.</summary>
    public int MaxRemediationAttempts { get; set; } = 3;
}
