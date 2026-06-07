using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public static class RunDiffServiceCollectionExtensions
{
    public static IServiceCollection AddRunDiffReview(this IServiceCollection services)
    {
        services.AddSingleton<IVerifyPassCheckpointService, VerifyPassCheckpointService>();
        services.AddSingleton<IRunDiffAggregator, RunDiffAggregator>();
        services.AddSingleton<IEvidenceDiffCorrelator, EvidenceDiffCorrelator>();
        services.AddSingleton<IRunReviewStore, FileRunReviewStore>();
        services.AddSingleton<IRunReviewService, RunReviewService>();
        services.AddSingleton<IReviewGate, ReviewGate>();
        services.AddSingleton<IReviewRepairDispatcher, ReviewRepairDispatcher>();
        return services;
    }
}
