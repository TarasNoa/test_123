namespace Libr4.Shared.Contracts.Evolution;

/// <summary>
/// Represents a gene in the Genome Evolution Protocol.
/// A gene is a reusable evolution pattern that can be applied to the system.
/// </summary>
public record EvolutionGene
{
    /// <summary>
    /// Unique identifier for the gene.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gene name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the gene does.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Category of the gene.
    /// </summary>
    public GeneCategory Category { get; init; }

    /// <summary>
    /// Conditions under which this gene is applicable.
    /// </summary>
    public List<string> Conditions { get; init; } = new();

    /// <summary>
    /// Evolution prompt template.
    /// </summary>
    public string PromptTemplate { get; init; } = string.Empty;

    /// <summary>
    /// Success criteria for the gene.
    /// </summary>
    public List<string> SuccessCriteria { get; init; } = new();

    /// <summary>
    /// Gene metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Whether the gene is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Version of the gene.
    /// </summary>
    public string Version { get; init; } = "1.0.0";
}

/// <summary>
/// Category of an evolution gene.
/// </summary>
public enum GeneCategory
{
    ErrorRecovery,
    PerformanceOptimization,
    SecurityEnhancement,
    Refactoring,
    FeatureAddition,
    BugFix,
    Documentation,
    Testing
}

/// <summary>
/// Represents a capsule in the Genome Evolution Protocol.
/// A capsule is a collection of genes that work together for a specific evolution goal.
/// </summary>
public record EvolutionCapsule
{
    /// <summary>
    /// Unique identifier for the capsule.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Capsule name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of the capsule.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Genes in the capsule.
    /// </summary>
    public List<string> GeneIds { get; init; } = new();

    /// <summary>
    /// Execution order of genes.
    /// </summary>
    public List<string> ExecutionOrder { get; init; } = new();

    /// <summary>
    /// Capsule metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Whether the capsule is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Version of the capsule.
    /// </summary>
    public string Version { get; init; } = "1.0.0";
}

/// <summary>
/// Represents an evolution event for auditability.
/// </summary>
public record EvolutionEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Run ID associated with the event.
    /// </summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>
    /// Gene or capsule that was applied.
    /// </summary>
    public string AppliedGeneOrCapsuleId { get; init; } = string.Empty;

    /// <summary>
    /// Type of evolution (gene or capsule).
    /// </summary>
    public EvolutionType Type { get; init; }

    /// <summary>
    /// Context before evolution.
    /// </summary>
    public string BeforeContext { get; init; } = string.Empty;

    /// <summary>
    /// Context after evolution.
    /// </summary>
    public string AfterContext { get; init; } = string.Empty;

    /// <summary>
    /// Whether the evolution succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if evolution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Metrics from the evolution.
    /// </summary>
    public Dictionary<string, double> Metrics { get; init; } = new();

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Type of evolution.
/// </summary>
public enum EvolutionType
{
    Gene,
    Capsule
}

/// <summary>
/// Interface for Genome Evolution Protocol (GEP) service.
/// </summary>
public interface IGEPService
{
    /// <summary>
    /// Registers a gene.
    /// </summary>
    /// <param name="gene">Gene to register.</param>
    void RegisterGene(EvolutionGene gene);

    /// <summary>
    /// Unregisters a gene.
    /// </summary>
    /// <param name="geneId">Gene ID.</param>
    void UnregisterGene(string geneId);

    /// <summary>
    /// Gets a gene by ID.
    /// </summary>
    /// <param name="geneId">Gene ID.</param>
    /// <returns>The gene, or null if not found.</returns>
    EvolutionGene? GetGene(string geneId);

    /// <summary>
    /// Gets all genes.
    /// </summary>
    /// <returns>List of all genes.</returns>
    IReadOnlyList<EvolutionGene> GetAllGenes();

    /// <summary>
    /// Gets genes by category.
    /// </summary>
    /// <param name="category">Category to filter by.</param>
    /// <returns>List of genes in the category.</returns>
    IReadOnlyList<EvolutionGene> GetGenesByCategory(GeneCategory category);

    /// <summary>
    /// Selects the best gene or capsule for the current context.
    /// </summary>
    /// <param name="context">Current context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Selected gene or capsule ID, or null if none applicable.</returns>
    Task<string?> SelectEvolutionAsync(
        EvolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a gene or capsule.
    /// </summary>
    /// <param name="geneOrCapsuleId">Gene or capsule ID.</param>
    /// <param name="context">Current context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Evolution result.</returns>
    Task<EvolutionResult> ApplyEvolutionAsync(
        string geneOrCapsuleId,
        EvolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an evolution event.
    /// </summary>
    /// <param name="event">Event to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordEventAsync(
        EvolutionEvent @event,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets evolution events for a run.
    /// </summary>
    /// <param name="runId">Run ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evolution events.</returns>
    Task<IReadOnlyList<EvolutionEvent>> GetEventsForRunAsync(
        string runId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for evolution selection and application.
/// </summary>
public record EvolutionContext
{
    /// <summary>
    /// Run ID.
    /// </summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>
    /// Current error or issue.
    /// </summary>
    public string? CurrentError { get; init; }

    /// <summary>
    /// Error type.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Recent history of the run.
    /// </summary>
    public List<string> History { get; init; } = new();

    /// <summary>
    /// Available resources or context.
    /// </summary>
    public Dictionary<string, object> Resources { get; init; } = new();

    /// <summary>
    /// Context metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Result of an evolution application.
/// </summary>
public record EvolutionResult
{
    /// <summary>
    /// Whether the evolution succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gene or capsule that was applied.
    /// </summary>
    public string AppliedGeneOrCapsuleId { get; init; } = string.Empty;

    /// <summary>
    /// Generated prompt for the evolution.
    /// </summary>
    public string GeneratedPrompt { get; init; } = string.Empty;

    /// <summary>
    /// Output from the evolution.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Error message if evolution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Metrics from the evolution.
    /// </summary>
    public Dictionary<string, double> Metrics { get; init; } = new();

    /// <summary>
    /// Evolution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When the evolution started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the evolution completed.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// In-memory implementation of GEP service.
/// </summary>
public class InMemoryGEPService : IGEPService
{
    private readonly Dictionary<string, EvolutionGene> _genes = new();
    private readonly Dictionary<string, EvolutionCapsule> _capsules = new();
    private readonly List<EvolutionEvent> _events = new();

    public void RegisterGene(EvolutionGene gene)
    {
        if (string.IsNullOrEmpty(gene.Id))
        {
            throw new ArgumentException("Gene ID cannot be null or empty", nameof(gene));
        }

        _genes[gene.Id] = gene;
    }

    public void UnregisterGene(string geneId)
    {
        _genes.Remove(geneId);
    }

    public EvolutionGene? GetGene(string geneId)
    {
        _genes.TryGetValue(geneId, out var gene);
        return gene;
    }

    public IReadOnlyList<EvolutionGene> GetAllGenes()
    {
        return _genes.Values.ToList().AsReadOnly();
    }

    public IReadOnlyList<EvolutionGene> GetGenesByCategory(GeneCategory category)
    {
        return _genes.Values
            .Where(g => g.Category == category)
            .ToList()
            .AsReadOnly();
    }

    public Task<string?> SelectEvolutionAsync(
        EvolutionContext context,
        CancellationToken cancellationToken = default)
    {
        // Simple selection logic based on error type
        if (string.IsNullOrEmpty(context.ErrorType))
        {
            return Task.FromResult<string?>(null);
        }

        var errorTypeLower = context.ErrorType.ToLowerInvariant();

        // Select gene based on error pattern
        var selectedGene = _genes.Values
            .Where(g => g.Enabled)
            .FirstOrDefault(g => 
                g.Conditions.Any(c => c.ToLowerInvariant().Contains(errorTypeLower)));

        return Task.FromResult<string?>(selectedGene?.Id);
    }

    public async Task<EvolutionResult> ApplyEvolutionAsync(
        string geneOrCapsuleId,
        EvolutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        
        var gene = _genes.GetValueOrDefault(geneOrCapsuleId);
        if (gene == null)
        {
            return new EvolutionResult
            {
                Success = false,
                ErrorMessage = $"Gene or capsule with ID {geneOrCapsuleId} not found",
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }

        // Generate prompt from gene template
        var prompt = gene.PromptTemplate
            .Replace("{error}", context.CurrentError ?? "")
            .Replace("{error_type}", context.ErrorType ?? "")
            .Replace("{history}", string.Join("\n", context.History));

        // In a real implementation, this would invoke the LLM with the prompt
        await Task.Delay(new Random().Next(100, 500), cancellationToken);

        var completedAt = DateTime.UtcNow;

        return new EvolutionResult
        {
            Success = true,
            AppliedGeneOrCapsuleId = geneOrCapsuleId,
            GeneratedPrompt = prompt,
            Output = $"Evolution applied using gene: {gene.Name}",
            Duration = completedAt - startedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
    }

    public Task RecordEventAsync(
        EvolutionEvent @event,
        CancellationToken cancellationToken = default)
    {
        _events.Add(@event);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EvolutionEvent>> GetEventsForRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        var events = _events
            .Where(e => e.RunId == runId)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<EvolutionEvent>>(events);
    }
}
