namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Unix;

public sealed record UnixTaskStep(string Name, Func<string, string> Transform);

public interface IUnixComposableTaskRunner
{
    string Run(string input, IReadOnlyList<UnixTaskStep> steps);
}

public sealed class UnixComposableTaskRunner : IUnixComposableTaskRunner
{
    public string Run(string input, IReadOnlyList<UnixTaskStep> steps)
    {
        var current = input ?? string.Empty;
        if (steps is null || steps.Count == 0)
            return current;

        foreach (var step in steps)
        {
            if (step?.Transform is null)
                continue;
            current = step.Transform(current);
        }

        return current;
    }
}
