namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;

public sealed record FimPrompt(
    string RelativePath,
    string Prefix,
    string Suffix,
    string HoleContent,
    int HoleStartLine,
    int HoleEndLine);

public sealed record FimGenerationContext(
    string RelativePath,
    string Prefix,
    string Suffix,
    string HoleContent,
    int HoleStartLine,
    int HoleEndLine);

public sealed class FimOptions
{
    public bool UseFimRepair { get; set; } = true;
    public int MinFileLines { get; set; } = 200;
    public int HoleRadiusLines { get; set; } = 4;
}
