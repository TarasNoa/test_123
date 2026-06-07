using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;

public static class PromptVariableSubstitutor
{
    private static readonly Regex VariablePattern = new(
        @"\{\{\s*(LIBR4_[A-Z0-9_]+)\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Apply(string template, IBuiltinPromptVarResolver resolver, BuiltinPromptVarContext context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return VariablePattern.Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            return resolver.Resolve(name, context);
        });
    }
}
