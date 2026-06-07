namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Cross-stack root cause buckets for recovery analytics (framework-agnostic).
/// Prefer investing in coverage of these categories over per-framework handlers.
/// </summary>
public enum RecoveryRootCauseCategory
{
    Configuration,
    EntryPoints,
    Dependencies,
    Imports,
    /// <summary>Generated or referenced type/source file is absent (not a wrong import line).</summary>
    MissingType,
    RuntimeWiring,
    TestInfrastructure,
    ArtifactContamination,
    Unknown
}
