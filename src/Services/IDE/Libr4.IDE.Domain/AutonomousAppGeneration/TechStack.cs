namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Tech stack chosen by the orchestrator during the Planning phase.
/// Follows the project convention: C# for infrastructure, F# for algorithms, Rust for media.
/// </summary>
public sealed class TechStack
{
    public IReadOnlyList<string> Languages { get; }
    public IReadOnlyList<string> Frameworks { get; }
    public IReadOnlyList<string> Databases { get; }
    public IReadOnlyList<string> Infrastructure { get; }
    public string Rationale { get; }

    public TechStack(
        IReadOnlyList<string> languages,
        IReadOnlyList<string> frameworks,
        IReadOnlyList<string> databases,
        IReadOnlyList<string> infrastructure,
        string rationale)
    {
        Languages = languages ?? new List<string>();
        Frameworks = frameworks ?? new List<string>();
        Databases = databases ?? new List<string>();
        Infrastructure = infrastructure ?? new List<string>();
        Rationale = rationale ?? string.Empty;
    }
}
