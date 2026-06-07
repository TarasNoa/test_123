namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;

public sealed record PromptTemplate(
    string Id,
    string Version,
    string Role,
    string InstructionBody,
    string? ResponseHint = null);
