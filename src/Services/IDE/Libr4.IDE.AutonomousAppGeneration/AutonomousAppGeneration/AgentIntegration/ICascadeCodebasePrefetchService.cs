namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ICascadeCodebasePrefetchService
{
    /// <summary>
    /// Runs <c>search_codebase</c> against a shallow-cloned upstream repository discovered in the user request.
    /// Returns null when no clone URL is found or search yields no hits.
    /// </summary>
    Task<string?> BuildPrefetchContextAsync(string userRequest, int maxChars, CancellationToken ct = default);
}
