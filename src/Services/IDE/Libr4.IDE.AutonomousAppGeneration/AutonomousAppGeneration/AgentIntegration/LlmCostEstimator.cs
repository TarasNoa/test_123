namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public static class LlmCostEstimator
{
    public static int EstimateTokens(string text) =>
        Math.Max(1, (text?.Length ?? 0) / 4);

    public static long EstimateRequestTokens(string prompt, string systemPrompt, int expectedOutputTokens = 2048) =>
        EstimateTokens(prompt) + EstimateTokens(systemPrompt) + Math.Max(256, expectedOutputTokens);

    public static decimal EstimateCostUsd(long tokens, double costPer1kTokens) =>
        tokens <= 0 || costPer1kTokens <= 0
            ? 0m
            : Math.Round(tokens / 1000m * (decimal)costPer1kTokens, 6);
}
