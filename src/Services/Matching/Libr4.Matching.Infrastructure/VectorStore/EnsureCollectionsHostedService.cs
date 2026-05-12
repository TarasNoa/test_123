using Libr4.Matching.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Infrastructure.VectorStore;

public class EnsureCollectionsHostedService : IHostedService
{
    private readonly IVectorIndex _index;
    private readonly IMatchRepository _matchRepo;
    private readonly ILogger<EnsureCollectionsHostedService> _logger;

    public EnsureCollectionsHostedService(
        IVectorIndex index,
        IMatchRepository matchRepo,
        ILogger<EnsureCollectionsHostedService> logger)
    {
        _index = index;
        _matchRepo = matchRepo;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Ensuring Qdrant collections exist...");
        await _index.EnsureCollectionsAsync(ct);
        _logger.LogInformation("Qdrant collections ready");

        var weights = await _matchRepo.GetCurrentWeightsAsync(ct);
        _logger.LogInformation(
            "Current scoring weights: keyword={Kw:F2}, semantic={Sm:F2}, exp={Ex:F2}, rep={Rp:F2}, rec={Rc:F2}, budget={Bg:F2}",
            weights.KeywordSkillWeight, weights.SemanticWeight, weights.ExperienceWeight,
            weights.ReputationWeight, weights.RecencyWeight, weights.BudgetFitWeight);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
