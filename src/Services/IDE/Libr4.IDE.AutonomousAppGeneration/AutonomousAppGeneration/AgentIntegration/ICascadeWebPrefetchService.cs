namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ICascadeWebPrefetchService
{
    /// <summary>
    /// Native multi-URL research prefetch for cascade orchestrator (replaces MCP browser.smoke lane).
    /// Returns null when no URLs can be discovered or research yields no content.
    /// </summary>
    Task<string?> BuildPrefetchContextAsync(string userRequest, int maxChars, CancellationToken ct = default);
}
