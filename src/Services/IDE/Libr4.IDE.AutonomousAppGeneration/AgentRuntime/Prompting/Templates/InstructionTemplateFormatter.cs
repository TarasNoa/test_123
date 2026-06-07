namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;

public static class InstructionTemplateFormatter
{
    public const string InstructionMarker = "### Instruction:";
    public const string ResponseMarker = "### Response:";

    public static string Format(string instruction, string? responseHint = null)
    {
        var body = instruction.Trim();
        if (string.IsNullOrWhiteSpace(body))
            return $"{InstructionMarker}\n\n{ResponseMarker}\n";

        var hint = string.IsNullOrWhiteSpace(responseHint)
            ? string.Empty
            : responseHint.Trim() + "\n";

        return $"{InstructionMarker}\n{body}\n\n{ResponseMarker}\n{hint}";
    }
}
