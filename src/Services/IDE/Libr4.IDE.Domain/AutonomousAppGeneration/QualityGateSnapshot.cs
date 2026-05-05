namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Certainty level for quality gate findings (from agentsys)
/// </summary>
public enum CertaintyLevel
{
    /// <summary>
    /// Definitely a problem - safe to auto-fix
    /// </summary>
    High,
    
    /// <summary>
    /// Probably a problem - needs context
    /// </summary>
    Medium,
    
    /// <summary>
    /// Might be a problem - needs human judgment
    /// </summary>
    Low
}

public sealed class QualityGateSnapshot
{
    public string Stage { get; }
    public int Score { get; }
    public bool Passed { get; }
    public IReadOnlyList<string> Reasons => _reasons.AsReadOnly();
    public DateTime EvaluatedAtUtc { get; }
    public CertaintyLevel Certainty { get; }

    private readonly List<string> _reasons = new();

    public QualityGateSnapshot(string stage, int score, bool passed, IReadOnlyList<string>? reasons = null, CertaintyLevel certainty = CertaintyLevel.Medium)
    {
        Stage = string.IsNullOrWhiteSpace(stage) ? "unknown" : stage;
        Score = Math.Clamp(score, 0, 10);
        Passed = passed;
        Certainty = certainty;
        EvaluatedAtUtc = DateTime.UtcNow;
        if (reasons is null) return;
        foreach (var reason in reasons.Where(r => !string.IsNullOrWhiteSpace(r)))
            _reasons.Add(reason);
    }
}
