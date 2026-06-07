using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Commands;

/// <summary>
/// Entry point for the whole autonomous app generation flow. The orchestrator
/// will plan, create the needed agents, generate the application, run it in
/// the shadow workspace, collect console logs, apply fixes and retry until
/// the application works or the iteration budget is spent.
/// </summary>
public sealed record StartAppGenerationCommand(
    string UserRequest,
    int MaxIterations = 20,
    Guid? ResumeFromRunId = null,
    /// <summary>Path to JSON exported from GET /api/ide/app-generation/{id} (fix-only resume after host restart).</summary>
    string? ResumeSeedPath = null,
    string? TriggerSource = null,
    string? TriggerActor = null,
    string? TriggerPayloadJson = null,
    /// <summary>P2-3: optional tenant identifier. Null = single-tenant / default.</summary>
    string? TenantId = null,
    /// <summary>Optional on-disk project root for workspace trust hashing (IDE workspace path).</summary>
    string? ProjectWorkspacePath = null) : IRequest<AppGenerationResponse>;
