namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifySubagentOptions
{
    public const string SectionName = "AutonomousAppGeneration:Verify";

    public bool Enabled { get; set; } = true;

    public bool EnableAgentSubagent { get; set; } = true;

    /// <summary>
    /// Run deterministic Obscura browser_* smoke flow for Browser smoke targets (no LLM).
    /// </summary>
    public bool EnableObscuraSmokeRunner { get; set; } = true;

    public string ObscuraSmokeWaitSelector { get; set; } = "body";

    public int ObscuraSmokeWaitTimeoutMs { get; set; } = 8_000;

    /// <summary>
    /// Fallback click selector when snapshot has no interactive nodes.
    /// </summary>
    public string? ObscuraSmokeClickSelector { get; set; } = "body a, body button";

    public string EvidenceRoot { get; set; } = ".logs/runs";

    public bool RequirePassInProduction { get; set; } = true;

    public bool EnableRecipeLlmFallback { get; set; } = true;

    public bool EnableReadinessProbe { get; set; } = true;

    public int ReadinessMaxAttempts { get; set; } = 30;

    public int ReadinessPollIntervalMs { get; set; } = 2_000;

    public int ReadinessStartupDelayMs { get; set; } = 3_000;

    public int ReadinessRequestTimeoutSeconds { get; set; } = 5;
}

public sealed record VerifySubagentResult(
    bool Passed,
    string Summary,
    string? EvidencePath,
    bool Skipped = false,
    string? SkipReason = null);
