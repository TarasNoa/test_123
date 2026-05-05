using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Service for validating prompt outputs against contracts and managing token budgets.
/// </summary>
public sealed class PromptContractService : IPromptContractService
{
    private readonly ILogger<PromptContractService> _logger;
    private const int TokensPerWord = 1;
    private const int TokensPerCharacter = 4;

    public PromptContractService(ILogger<PromptContractService> logger)
    {
        _logger = logger;
    }

    public PromptValidationResult ValidatePromptOutput(
        string stage,
        string output,
        PromptOutputContract contract)
    {
        var errors = new List<string>();
        var missingFields = new List<string>();

        // Validate output format
        if (string.IsNullOrWhiteSpace(output))
        {
            errors.Add("output_is_empty");
        }

        // Validate required fields
        if (contract.RequiredFields.Count > 0)
        {
            try
            {
                var json = JsonDocument.Parse(output);
                var root = json.RootElement;

                foreach (var field in contract.RequiredFields)
                {
                    if (!root.TryGetProperty(field, out _))
                    {
                        missingFields.Add(field);
                        errors.Add($"missing_required_field:{field}");
                    }
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"invalid_json_format:{ex.Message}");
            }
        }

        // Validate token count
        var tokensUsed = EstimateTokens(output);
        if (tokensUsed > contract.MaxTokens)
        {
            errors.Add($"exceeds_token_limit:{tokensUsed}>{contract.MaxTokens}");
        }

        var isValid = errors.Count == 0;

        _logger.LogInformation(
            "Prompt validation: stage={Stage}, valid={Valid}, errors={ErrorCount}, tokens={Tokens}/{MaxTokens}",
            stage, isValid, errors.Count, tokensUsed, contract.MaxTokens);

        return new PromptValidationResult(
            isValid,
            stage,
            tokensUsed,
            errors,
            missingFields,
            DateTime.UtcNow);
    }

    public bool IsWithinTokenBudget(
        string stage,
        int tokensUsed,
        int allocatedTokens)
    {
        var isWithin = tokensUsed <= allocatedTokens;

        if (!isWithin)
        {
            _logger.LogWarning(
                "Token budget exceeded: stage={Stage}, used={Used}, allocated={Allocated}",
                stage, tokensUsed, allocatedTokens);
        }

        return isWithin;
    }

    public TokenBudgetAllocation GetTokenBudgetAllocation(
        string stage,
        int totalBudget,
        IReadOnlyList<string> stages)
    {
        if (stages.Count == 0)
            return new TokenBudgetAllocation(stage, totalBudget, 0, totalBudget, 0);

        // Allocate budget proportionally across stages
        var stageWeight = GetStageWeight(stage);
        var totalWeight = stages.Sum(s => GetStageWeight(s));
        var allocatedTokens = (int)(totalBudget * (stageWeight / totalWeight));

        return new TokenBudgetAllocation(
            stage,
            allocatedTokens,
            0,
            allocatedTokens,
            0);
    }

    public string GetOverflowStrategy(
        string stage,
        int tokensUsed,
        int allocatedTokens)
    {
        if (tokensUsed <= allocatedTokens)
            return "within_budget";

        var overflowPercent = ((double)(tokensUsed - allocatedTokens) / allocatedTokens) * 100;

        return stage switch
        {
            "planning" => overflowPercent > 50 ? "truncate_lowest_priority" : "compress_output",
            "generation" => overflowPercent > 30 ? "split_into_phases" : "compress_output",
            "consistency" => overflowPercent > 40 ? "skip_optional_checks" : "compress_output",
            "execution" => overflowPercent > 25 ? "deterministic_fallback" : "compress_output",
            "fixing" => overflowPercent > 35 ? "prioritize_critical_fixes" : "compress_output",
            _ => "compress_output",
        };
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Rough estimation: ~1 token per word + ~1 token per 4 characters
        var words = Regex.Matches(text, @"\b\w+\b").Count;
        var charTokens = text.Length / TokensPerCharacter;
        return Math.Max(1, words + charTokens);
    }

    private static double GetStageWeight(string stage)
    {
        return stage switch
        {
            "planning" => 1.5,
            "generation" => 3.0,
            "consistency" => 1.2,
            "execution" => 1.0,
            "fixing" => 2.0,
            _ => 1.0,
        };
    }
}
