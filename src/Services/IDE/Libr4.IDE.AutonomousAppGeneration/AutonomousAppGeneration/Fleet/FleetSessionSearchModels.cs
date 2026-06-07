namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed record FleetSessionSearchQuery(
    string Query,
    string? Stack = null,
    string? Outcome = null,
    string? SpaceId = null,
    string? DateBucket = null,
    int Limit = 50);

public sealed record FleetSessionSearchHit(
    Guid RunId,
    string Title,
    AgentFleetStatus Status,
    string? Stack,
    string? SpaceId,
    string Snippet,
    double Score,
    DateTime LastActivityAtUtc,
    bool Pinned);

public sealed record FleetSessionSearchFacets(
    IReadOnlyList<string> Stacks,
    IReadOnlyList<string> Outcomes,
    IReadOnlyList<string> DateBuckets);

public sealed record FleetSessionSearchResult(
    string Query,
    int Count,
    FleetSessionSearchFacets Facets,
    IReadOnlyList<FleetSessionSearchHit> Hits);

public sealed record FleetSessionIndexDocument(
    Guid RunId,
    string Title,
    string? UserRequest,
    string? ErrorSignature,
    string? FilesTouched,
    string? SpaceName,
    string? StackTags,
    string Outcome,
    DateTime LastActivityAtUtc,
    bool Pinned);

public sealed record RunForkResult(
    Guid SourceRunId,
    Guid NewRunId,
    string Title,
    bool PlanCopied);

public sealed record FleetGdprEraseResult(
    Guid RunId,
    bool FleetIndexRemoved,
    bool SearchIndexRemoved,
    bool RunDirectoryRemoved);
