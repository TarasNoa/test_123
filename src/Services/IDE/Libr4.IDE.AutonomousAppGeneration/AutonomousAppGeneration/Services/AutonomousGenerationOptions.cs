namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class AutonomousGenerationOptions
{
    // Target batch size for file content generation.
    public int InitialBatchSize { get; set; } = 3;

    // Max per-batch retries before fallback/adaptation.
    public int MaxBatchAttempts { get; set; } = 2;

    // Hard timeout for one LLM generation request.
    public int LlmStepTimeoutSeconds { get; set; } = 180;

    // Upper bound for manifest size to avoid unbounded generation plans.
    public int MaxManifestFiles { get; set; } = 40;

    // Fix-loop safeguards: do not let the fixer rewrite too much at once.
    public int MaxFilesToPatchPerIteration { get; set; } = 8;

    // If relative change of a file exceeds this threshold (0..1), reject patch unless file is tiny/new.
    public double MaxRelativeFileRewriteRatio { get; set; } = 0.75;

    // Tiny files can be fully rewritten safely (e.g., short configs or tests).
    public int AllowFullRewriteIfFileSmallerThanChars { get; set; } = 240;
}
