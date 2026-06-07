namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;

public interface IPromptTemplateRegistry
{
    PromptTemplate? TryGet(string role, string? variantId = null);

    IReadOnlyList<PromptTemplate> ListByRole(string role);

    string FormatRolePrompt(string role, string? variantId = null);
}
