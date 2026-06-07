using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public sealed class RunReviewService : IRunReviewService
{
    private readonly IRunReviewStore _store;
    private readonly IAppGenerationRepository? _repository;
    private readonly IReviewRepairDispatcher? _repairDispatcher;
    private readonly HumanReviewOptions _options;
    private readonly ILogger<RunReviewService> _logger;

    public RunReviewService(
        IRunReviewStore store,
        IOptions<HumanReviewOptions> options,
        ILogger<RunReviewService> logger,
        IAppGenerationRepository? repository = null,
        IReviewRepairDispatcher? repairDispatcher = null)
    {
        _store = store;
        _repository = repository;
        _repairDispatcher = repairDispatcher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RunReviewStatusResponse> GetStatusAsync(Guid runId, CancellationToken ct = default)
    {
        var filePaths = await ResolveReviewablePathsAsync(runId, ct).ConfigureAwait(false);
        var entries = await _store.LoadAsync(runId, ct).ConfigureAwait(false);
        return BuildStatus(runId, filePaths, entries);
    }

    public async Task<RunReviewStatusResponse> SubmitAsync(
        Guid runId,
        ReviewSubmissionRequest request,
        CancellationToken ct = default)
    {
        if (request.Paths.Count == 0)
            throw new ArgumentException("At least one path is required", nameof(request));

        var normalizedPaths = request.Paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(NormalizePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedPaths.Count == 0)
            throw new ArgumentException("No valid paths provided", nameof(request));

        var timestamp = DateTime.UtcNow;
        foreach (var path in normalizedPaths)
        {
            await _store.AppendAsync(
                new ReviewDecisionAuditEntry(
                    runId,
                    path,
                    request.Decision,
                    request.Notes,
                    request.ReviewerId,
                    timestamp),
                ct).ConfigureAwait(false);
        }

        IReadOnlyList<string> repairPaths = request.Decision is ReviewDecision.Reject or ReviewDecision.RequestRepair
            ? normalizedPaths
            : Array.Empty<string>();

        if (_options.AutoSpawnRepairOnReject
            && repairPaths.Count > 0
            && _repairDispatcher is not null)
        {
            _repairDispatcher.DispatchScopedRepair(runId, repairPaths, request.Notes);
            _logger.LogInformation(
                "[RunReview] Dispatched scoped repair for run {RunId} paths={Count}",
                runId,
                repairPaths.Count);
        }

        var filePaths = await ResolveReviewablePathsAsync(runId, ct).ConfigureAwait(false);
        var entries = await _store.LoadAsync(runId, ct).ConfigureAwait(false);
        return BuildStatus(runId, filePaths, entries);
    }

    internal RunReviewStatusResponse BuildStatus(
        Guid runId,
        IReadOnlyList<string> reviewablePaths,
        IReadOnlyList<ReviewDecisionAuditEntry> entries)
    {
        if (!_options.RequireHumanReview)
        {
            return new RunReviewStatusResponse(
                runId,
                RunReviewStatus.NotRequired,
                false,
                reviewablePaths.Count,
                reviewablePaths.Count,
                reviewablePaths.Count,
                0,
                0,
                Array.Empty<FileReviewState>(),
                Array.Empty<string>());
        }

        var latestByPath = entries
            .GroupBy(e => NormalizePath(e.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(e => e.TimestampUtc).First(),
                StringComparer.OrdinalIgnoreCase);

        var fileStates = reviewablePaths
            .Select(path =>
            {
                if (!latestByPath.TryGetValue(path, out var entry))
                    return null;

                return new FileReviewState(
                    path,
                    entry.Decision,
                    entry.Notes,
                    entry.ReviewerId,
                    entry.TimestampUtc);
            })
            .Where(s => s is not null)
            .Cast<FileReviewState>()
            .OrderBy(s => s.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pendingPaths = reviewablePaths
            .Where(p => !latestByPath.ContainsKey(p))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var decided = fileStates.Count;
        var approved = fileStates.Count(s =>
            s.Decision is ReviewDecision.Approve or ReviewDecision.ApproveWithNotes);
        var rejected = fileStates.Count(s => s.Decision == ReviewDecision.Reject);
        var repairRequested = fileStates.Count(s => s.Decision == ReviewDecision.RequestRepair);

        var status = ResolveAggregateStatus(reviewablePaths.Count, fileStates, pendingPaths.Count);

        return new RunReviewStatusResponse(
            runId,
            status,
            true,
            reviewablePaths.Count,
            decided,
            approved,
            rejected,
            repairRequested,
            fileStates,
            pendingPaths);
    }

    private async Task<IReadOnlyList<string>> ResolveReviewablePathsAsync(Guid runId, CancellationToken ct)
    {
        if (_repository is null)
            return Array.Empty<string>();

        var orchestrator = await _repository.GetAsync(runId, ct).ConfigureAwait(false);
        if (orchestrator is null || orchestrator.Files.Count == 0)
            return Array.Empty<string>();

        return orchestrator.Files
            .Select(f => NormalizePath(f.RelativePath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static RunReviewStatus ResolveAggregateStatus(
        int totalFiles,
        IReadOnlyList<FileReviewState> fileStates,
        int pendingCount)
    {
        if (totalFiles == 0)
            return RunReviewStatus.Approved;

        if (fileStates.Any(s => s.Decision == ReviewDecision.RequestRepair))
            return RunReviewStatus.RepairRequested;

        if (fileStates.Any(s => s.Decision == ReviewDecision.Reject))
            return RunReviewStatus.Rejected;

        if (pendingCount == 0
            && fileStates.Count == totalFiles
            && fileStates.All(s => s.Decision is ReviewDecision.Approve or ReviewDecision.ApproveWithNotes))
            return RunReviewStatus.Approved;

        if (fileStates.Count == 0)
            return RunReviewStatus.Pending;

        return RunReviewStatus.Partial;
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
