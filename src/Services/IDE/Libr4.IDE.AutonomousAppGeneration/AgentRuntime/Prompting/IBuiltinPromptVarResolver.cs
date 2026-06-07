namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;

public interface IBuiltinPromptVarResolver
{
    string Resolve(string variableName, BuiltinPromptVarContext context);
    IReadOnlyDictionary<string, string> ResolveAll(BuiltinPromptVarContext context);
}
