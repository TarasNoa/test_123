namespace Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;

public enum AutonomousHostProfile
{
    OpenRouter,
    DockerModelRunner,
    BatchCi,
    Benchmark
}

public sealed class AutonomousHostProfileOptions
{
    public const string SectionName = "AutonomousAppGeneration:HostProfile";

    public AutonomousHostProfile ActiveProfile { get; set; } = AutonomousHostProfile.DockerModelRunner;
}

public sealed record AutonomousHostProfileDescriptor(
    AutonomousHostProfile Profile,
    string AiDefaultProvider,
    string ProviderMatrixDefault,
    string AgentModelRoutingProfile,
    bool BatchLlmProfileEnabled,
    bool BenchmarkModeEnabled,
    bool GpuThrottleEnabled,
    string SwitchHint);
