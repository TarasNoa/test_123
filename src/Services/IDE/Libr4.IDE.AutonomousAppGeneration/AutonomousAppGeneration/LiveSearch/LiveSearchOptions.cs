namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public sealed class LiveSearchOptions
{
    public const string SectionName = "AutonomousAppGeneration:LiveSearch";

    public bool Enabled { get; set; } = true;

    public int MaxRequestsPerMinute { get; set; } = 20;

    public int CacheTtlSeconds { get; set; } = 900;

    public int MaxResults { get; set; } = 8;

    public int MaxSnippetChars { get; set; } = 500;

    public int MaxResponseChars { get; set; } = 12_000;

    public string DefaultWebProvider { get; set; } = "duckduckgo";

    public string? BraveApiKey { get; set; }

    public string? XApiBearerToken { get; set; }

    public bool EnableSearchX { get; set; }

    public bool BlockPrivateNetworkTargets { get; set; } = true;
}
