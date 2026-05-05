namespace Libr4.IDE.Application.Cascade.DTOs;

/// <summary>
/// DTO for OrchestratorPlan
/// </summary>
public record OrchestratorPlanDto
{
    public Guid Id { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string OriginalPrompt { get; init; } = string.Empty;
    public string TaskDescription { get; init; } = string.Empty;
    public List<string> Subtasks { get; init; } = new();
    public string Complexity { get; init; } = string.Empty;
    public List<OrchestratorPhaseDto> Phases { get; init; } = new();
    public PrefetchContextDto PrefetchContext { get; init; } = new();
    public string OrchestratorJson { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for PrefetchContext
/// </summary>
public record PrefetchContextDto
{
    public bool PrefetchEnabled { get; init; }
    public List<WebSearchResultDto> WebSearchResults { get; init; } = new();
    public Dictionary<string, string> DocumentationReferences { get; init; } = new();
    public DateTime PrefetchedAt { get; init; }
}

/// <summary>
/// DTO for WebSearchResult
/// </summary>
public record WebSearchResultDto
{
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Snippet { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}
