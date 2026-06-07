using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;

public sealed record RepairFragmentInput(
    string? BuildLog,
    IReadOnlyList<ErrorReport> Errors,
    IReadOnlyList<GeneratedFile> WorkingFiles,
    int RepairAttempt = 1,
    string? DesignArtifactJson = null,
    string? VerifyEvidence = null,
    string? PlaybookHint = null,
    string? OrchestratorJitHint = null,
    string? LspDiagnostics = null,
    string? GitDiffEvidence = null,
    string? FastContextEvidence = null);
