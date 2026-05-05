using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Uses an LLM (OpenRouter free models by default) to transform a natural
/// language user request into a fully structured <see cref="GenerationPlan"/>.
/// Decides tech stack, execution phases and which existing IDE agents must be
/// instantiated for the orchestration.
/// </summary>
public interface IAppPlannerService
{
    Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default);
}
