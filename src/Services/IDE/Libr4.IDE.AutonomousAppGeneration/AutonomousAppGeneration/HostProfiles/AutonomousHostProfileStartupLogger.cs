using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;

public sealed class AutonomousHostProfileStartupLogger : IHostedService
{
    private readonly IAutonomousHostProfileService _profiles;
    private readonly ILogger<AutonomousHostProfileStartupLogger> _logger;

    public AutonomousHostProfileStartupLogger(
        IAutonomousHostProfileService profiles,
        ILogger<AutonomousHostProfileStartupLogger> logger)
    {
        _profiles = profiles;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var descriptor = _profiles.DescribeActive();
        _logger.LogInformation(
            "Autonomous host profile={Profile} ai={AiProvider} matrix={MatrixProvider} agentModels={AgentModels} batch={Batch} benchmark={Benchmark} gpuThrottle={GpuThrottle}",
            descriptor.Profile,
            descriptor.AiDefaultProvider,
            descriptor.ProviderMatrixDefault,
            descriptor.AgentModelRoutingProfile,
            descriptor.BatchLlmProfileEnabled,
            descriptor.BenchmarkModeEnabled,
            descriptor.GpuThrottleEnabled);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
