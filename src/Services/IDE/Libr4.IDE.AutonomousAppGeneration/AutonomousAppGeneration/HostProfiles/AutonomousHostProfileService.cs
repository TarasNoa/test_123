using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;

public interface IAutonomousHostProfileService
{
    AutonomousHostProfile ActiveProfile { get; }

    AutonomousHostProfileDescriptor DescribeActive();
}

public sealed class AutonomousHostProfileService : IAutonomousHostProfileService
{
    private readonly AutonomousHostProfileOptions _options;
    private readonly IConfiguration _configuration;

    public AutonomousHostProfileService(
        IOptions<AutonomousHostProfileOptions> options,
        IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
    }

    public AutonomousHostProfile ActiveProfile => _options.ActiveProfile;

    public AutonomousHostProfileDescriptor DescribeActive()
    {
        var profile = _options.ActiveProfile;
        return profile switch
        {
            AutonomousHostProfile.OpenRouter => new AutonomousHostProfileDescriptor(
                profile,
                AiDefaultProvider: _configuration["AI:DefaultProvider"] ?? "OpenRouter",
                ProviderMatrixDefault: _configuration["ProviderCapabilityMatrix:DefaultProvider"] ?? "openrouter",
                AgentModelRoutingProfile: ReadAgentModelProfile("OpenRouter"),
                BatchLlmProfileEnabled: ReadBool(AutonomousBatchLlmProfileOptions.SectionName + ":UseBatchLlmProfile"),
                BenchmarkModeEnabled: ReadBool(AutonomousBenchmarkModeOptions.SectionName + ":EnableBenchmarkMode"),
                GpuThrottleEnabled: ReadBool("AI:GpuThrottle:Enabled"),
                SwitchHint: "scripts/Run-AutonomousHost.ps1 -Profile OpenRouter"),
            AutonomousHostProfile.DockerModelRunner => new AutonomousHostProfileDescriptor(
                profile,
                AiDefaultProvider: _configuration["AI:DefaultProvider"] ?? "DockerModelRunner",
                ProviderMatrixDefault: _configuration["ProviderCapabilityMatrix:DefaultProvider"] ?? "dockermodelrunner",
                AgentModelRoutingProfile: ReadAgentModelProfile("Dmr"),
                BatchLlmProfileEnabled: ReadBool(AutonomousBatchLlmProfileOptions.SectionName + ":UseBatchLlmProfile"),
                BenchmarkModeEnabled: ReadBool(AutonomousBenchmarkModeOptions.SectionName + ":EnableBenchmarkMode"),
                GpuThrottleEnabled: ReadBool("AI:GpuThrottle:Enabled"),
                SwitchHint: "scripts/Run-AutonomousHost.ps1 -Profile DockerModelRunner"),
            AutonomousHostProfile.BatchCi => new AutonomousHostProfileDescriptor(
                profile,
                AiDefaultProvider: _configuration["AI:DefaultProvider"] ?? "OpenRouter",
                ProviderMatrixDefault: _configuration["ProviderCapabilityMatrix:DefaultProvider"] ?? "openrouter",
                AgentModelRoutingProfile: ReadAgentModelProfile("Batch"),
                BatchLlmProfileEnabled: true,
                BenchmarkModeEnabled: ReadBool(AutonomousBenchmarkModeOptions.SectionName + ":EnableBenchmarkMode"),
                GpuThrottleEnabled: ReadBool("AI:GpuThrottle:Enabled"),
                SwitchHint: "scripts/Run-AutonomousHost.ps1 -Profile BatchCi"),
            AutonomousHostProfile.Benchmark => new AutonomousHostProfileDescriptor(
                profile,
                AiDefaultProvider: _configuration["AI:DefaultProvider"] ?? "DockerModelRunner",
                ProviderMatrixDefault: _configuration["ProviderCapabilityMatrix:DefaultProvider"] ?? "dockermodelrunner",
                AgentModelRoutingProfile: ReadAgentModelProfile("Auto"),
                BatchLlmProfileEnabled: ReadBool(AutonomousBatchLlmProfileOptions.SectionName + ":UseBatchLlmProfile"),
                BenchmarkModeEnabled: true,
                GpuThrottleEnabled: ReadBool("AI:GpuThrottle:Enabled"),
                SwitchHint: "scripts/Run-AutonomousHost.ps1 -Profile Benchmark"),
            _ => throw new InvalidOperationException($"unknown_host_profile:{profile}")
        };
    }

    private string ReadAgentModelProfile(string fallback) =>
        _configuration[$"{AgentModelRoutingOptions.SectionName}:ActiveProfile"] ?? fallback;

    private bool ReadBool(string key) =>
        bool.TryParse(_configuration[key], out var value) && value;
}
