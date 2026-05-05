namespace Libr4.AI.Infrastructure.Orchestration;

/// <summary>
/// Multi-Provider Orchestrator - coordinates up to 8 AI providers on a single task
/// Based on Claude Octopus pattern
/// </summary>
public interface IMultiProviderOrchestrator
{
    /// <summary>
    /// Execute task with multiple providers
    /// </summary>
    Task<MultiProviderResult> ExecuteWithProvidersAsync(
        AgentTask task,
        ProviderExecutionMode mode,
        List<string> providerIds,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get consensus among providers (quality gate)
    /// </summary>
    Task<ConsensusResult> GetConsensusAsync(
        Guid taskId,
        float threshold = 0.75f,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Run adversarial review between providers
    /// </summary>
    Task<AdversarialReviewResult> RunAdversarialReviewAsync(
        Guid taskId,
        List<string> reviewerIds,
        CancellationToken cancellationToken = default);
}

public enum ProviderExecutionMode
{
    Parallel,     // All providers run in parallel (for research)
    Sequential,   // Providers run sequentially (for problem scoping)
    Adversarial   // Providers review each other (for code review)
}

public class MultiProviderResult
{
    public Guid TaskId { get; set; }
    public Dictionary<string, AgentTaskResult> ProviderResults { get; set; } = new();
    public ConsensusResult Consensus { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}

public class ConsensusResult
{
    public bool PassesThreshold { get; set; }
    public float AgreementLevel { get; set; }
    public List<string> AgreeingProviders { get; set; } = new();
    public List<string> DisagreeingProviders { get; set; } = new();
    public string? DominantAnswer { get; set; }
}

public class AdversarialReviewResult
{
    public bool HasConsensus { get; set; }
    public List<ReviewComment> Comments { get; set; } = new();
    public string? FinalRecommendation { get; set; }
    public int ReviewRounds { get; set; }
}

public class ReviewComment
{
    public string ProviderId { get; set; } = string.Empty;
    public string TargetProviderId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public Exoskeleton.Severity Severity { get; set; }
}
