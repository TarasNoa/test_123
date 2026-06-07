namespace Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;

public sealed class InlineCompletionOptions
{
    public const string SectionName = "AutonomousAppGeneration:InlineCompletion";

    /// <summary>Master switch for Supercomplete / ghost text.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Model routing role (fast local/cloud model).</summary>
    public string ModelRole { get; set; } = "explore";

    /// <summary>Hard timeout for a single completion request.</summary>
    public int MaxLatencyMs { get; set; } = 2000;

    /// <summary>Lines of context before cursor.</summary>
    public int MaxPrefixLines { get; set; } = 80;

    /// <summary>Lines of context after cursor.</summary>
    public int MaxSuffixLines { get; set; } = 40;

    /// <summary>Max characters returned as ghost text.</summary>
    public int MaxCompletionChars { get; set; } = 512;

    /// <summary>Related imports snippet from RepoGraph (optional).</summary>
    public int MaxRelatedImportLines { get; set; } = 12;
}
