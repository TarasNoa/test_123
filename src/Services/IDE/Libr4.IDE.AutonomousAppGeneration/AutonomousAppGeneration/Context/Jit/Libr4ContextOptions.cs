namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;

public sealed class Libr4ContextOptions
{
    public bool EnableJitInjection { get; set; } = true;
    public int MaxCharsPerInjection { get; set; } = 2_000;
}
