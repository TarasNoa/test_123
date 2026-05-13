using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Escrow;

/// <summary>
/// Escrow code service with AI-powered automated pre-review and security analysis.
/// </summary>
public class EscrowCodeService : IEscrowCodeService
{
    private readonly IAIService _aiService;
    private readonly ILogger<EscrowCodeService> _logger;
    private readonly Dictionary<string, EscrowEntry> _escrows = new();

    public EscrowCodeService(
        IAIService aiService,
        ILogger<EscrowCodeService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> CreateEscrowAsync(string code, string creatorId, string[] reviewerIds, CancellationToken ct = default)
    {
        var escrowId = Guid.NewGuid().ToString("N");

        // AI-powered automated pre-review
        var analysis = await _aiService.AnalyzeTextAsync(
            code,
            "security and quality review",
            null);

        _escrows[escrowId] = new EscrowEntry
        {
            EscrowId = escrowId,
            Code = code,
            CreatorId = creatorId,
            ReviewerIds = reviewerIds,
            State = "Pending",
            CreatedAt = DateTime.UtcNow,
            AiPreReview = analysis
        };

        _logger.LogInformation(
            "Created escrow {EscrowId} for creator {CreatorId} with {ReviewerCount} reviewers. AI pre-review completed.",
            escrowId, creatorId, reviewerIds.Length);
        return escrowId;
    }

    public Task<EscrowStatus> GetStatusAsync(string escrowId, CancellationToken ct = default)
    {
        if (!_escrows.TryGetValue(escrowId, out var entry))
        {
            throw new KeyNotFoundException($"Escrow {escrowId} not found");
        }

        return Task.FromResult(new EscrowStatus
        {
            EscrowId = entry.EscrowId,
            State = entry.State,
            CreatorId = entry.CreatorId,
            ReviewerIds = entry.ReviewerIds,
            Reviews = entry.Reviews,
            CreatedAt = entry.CreatedAt,
            ReleasedAt = entry.ReleasedAt
        });
    }

    public Task<bool> ApproveAsync(string escrowId, string reviewerId, string? comments = null, CancellationToken ct = default)
    {
        if (!_escrows.TryGetValue(escrowId, out var entry))
        {
            return Task.FromResult(false);
        }

        entry.Reviews[reviewerId] = "Approved" + (comments != null ? $": {comments}" : "");
        _logger.LogInformation("Escrow {EscrowId} approved by {ReviewerId}", escrowId, reviewerId);
        
        return Task.FromResult(true);
    }

    public Task<bool> RejectAsync(string escrowId, string reviewerId, string reason, CancellationToken ct = default)
    {
        if (!_escrows.TryGetValue(escrowId, out var entry))
        {
            return Task.FromResult(false);
        }

        entry.State = "Rejected";
        entry.Reviews[reviewerId] = $"Rejected: {reason}";
        _logger.LogInformation("Escrow {EscrowId} rejected by {ReviewerId}: {Reason}", escrowId, reviewerId, reason);
        
        return Task.FromResult(true);
    }

    public Task<string?> ReleaseAsync(string escrowId, CancellationToken ct = default)
    {
        if (!_escrows.TryGetValue(escrowId, out var entry))
        {
            return Task.FromResult<string?>(null);
        }

        var approvedCount = entry.ReviewerIds.Count(rid =>
            entry.Reviews.TryGetValue(rid, out var review) && review.StartsWith("Approved", StringComparison.OrdinalIgnoreCase));

        if (approvedCount < entry.ReviewerIds.Length)
        {
            _logger.LogWarning(
                "Escrow {EscrowId} cannot be released: {ApprovedCount}/{RequiredCount} reviewers approved",
                escrowId, approvedCount, entry.ReviewerIds.Length);
            return Task.FromResult<string?>(null);
        }

        entry.State = "Released";
        entry.ReleasedAt = DateTime.UtcNow;
        _logger.LogInformation("Escrow {EscrowId} released (unanimous approval)", escrowId);
        
        return Task.FromResult<string?>(entry.Code);
    }

    private class EscrowEntry
    {
        public string EscrowId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public string[] ReviewerIds { get; set; } = Array.Empty<string>();
        public string State { get; set; } = string.Empty;
        public Dictionary<string, string> Reviews { get; set; } = new();
        public string? AiPreReview { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }
}
