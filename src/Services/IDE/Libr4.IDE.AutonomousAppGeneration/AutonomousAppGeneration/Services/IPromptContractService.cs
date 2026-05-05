namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Schema contract for prompt output validation.
/// </summary>
public sealed record PromptOutputContract(
    string Stage,
    string OutputFormat,
    IReadOnlyList<string> RequiredFields,
    int MaxTokens,
    string? JsonSchema);

/// <summary>
/// Validation result for prompt output against contract.
/// </summary>
public sealed record PromptValidationResult(
    bool IsValid,
    string Stage,
    int TokensUsed,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> MissingFields,
    DateTime ValidatedAtUtc);

/// <summary>
/// Token budget allocation per stage.
/// </summary>
public sealed record TokenBudgetAllocation(
    string Stage,
    int AllocatedTokens,
    int UsedTokens,
    int RemainingTokens,
    double UtilizationPercent);

/// <summary>
/// Service for validating prompt outputs against contracts and managing token budgets.
/// </summary>
public interface IPromptContractService
{
    /// <summary>
    /// Validate prompt output against contract schema.
    /// </summary>
    PromptValidationResult ValidatePromptOutput(
        string stage,
        string output,
        PromptOutputContract contract);

    /// <summary>
    /// Check if output conforms to token budget.
    /// </summary>
    bool IsWithinTokenBudget(
        string stage,
        int tokensUsed,
        int allocatedTokens);

    /// <summary>
    /// Get token budget allocation for stage.
    /// </summary>
    TokenBudgetAllocation GetTokenBudgetAllocation(
        string stage,
        int totalBudget,
        IReadOnlyList<string> stages);

    /// <summary>
    /// Get overflow strategy when token budget exceeded.
    /// </summary>
    string GetOverflowStrategy(
        string stage,
        int tokensUsed,
        int allocatedTokens);
}
