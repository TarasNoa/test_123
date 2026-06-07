namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

public sealed record CompactionRequest(
    Guid? RunId = null,
    string? SessionId = null,
    IReadOnlyList<string>? ManifestFiles = null,
    string? RequestFingerprint = null,
    string? Stage = null);
