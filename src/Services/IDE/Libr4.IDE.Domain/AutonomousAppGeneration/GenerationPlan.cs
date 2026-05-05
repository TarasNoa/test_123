namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// The full plan produced by the orchestrator during the Planning phase.
/// Contains the chosen tech stack, ordered execution phases and the list of
/// IDE agents that will participate.
/// </summary>
public sealed class GenerationPlan
{
    public string ApplicationName { get; }
    public string ApplicationDescription { get; }
    public TechStack TechStack { get; }
    public IReadOnlyList<GenerationPhase> Phases { get; }
    public IReadOnlyList<string> RequiredAgents { get; }
    public int MaxIterations { get; }

    /// <summary>
    /// Container image / VM profile that provides the runtime toolchain for
    /// the generated application (e.g. "python:3.12-slim", "node:22-alpine",
    /// "mcr.microsoft.com/dotnet/sdk:8.0", "rust:1.80"). This is what the
    /// <see cref="IsolatedRuntime"/> spins up to build and run the code.
    /// </summary>
    public string RuntimeImage { get; }

    /// <summary>Shell commands that build the project, executed in order.</summary>
    public IReadOnlyList<string> BuildCommands { get; }

    /// <summary>Shell commands that run the tests; exit code 0 = success.</summary>
    public IReadOnlyList<string> TestCommands { get; }

    public GenerationPlan(
        string applicationName,
        string applicationDescription,
        TechStack techStack,
        IReadOnlyList<GenerationPhase> phases,
        IReadOnlyList<string> requiredAgents,
        string runtimeImage,
        IReadOnlyList<string> buildCommands,
        IReadOnlyList<string> testCommands,
        int maxIterations = 10)
    {
        ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        ApplicationDescription = applicationDescription ?? string.Empty;
        TechStack = techStack ?? throw new ArgumentNullException(nameof(techStack));
        Phases = phases ?? new List<GenerationPhase>();
        RequiredAgents = requiredAgents ?? new List<string>();
        RuntimeImage = string.IsNullOrWhiteSpace(runtimeImage) ? "mcr.microsoft.com/dotnet/sdk:8.0" : runtimeImage;
        BuildCommands = buildCommands?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList()
            ?? new List<string>();
        TestCommands = testCommands?.Where(c => !string.IsNullOrWhiteSpace(c)).ToList()
            ?? new List<string>();
        MaxIterations = maxIterations <= 0 ? 10 : maxIterations;
    }
}
