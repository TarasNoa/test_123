using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Escrow;

/// <summary>
/// Stub implementation of escrow code service
/// </summary>
public class EscrowCodeService : IEscrowCodeService
{
    private readonly ILogger<EscrowCodeService> _logger;
    private readonly Dictionary<string, EscrowEntry> _escrows = new();

    public EscrowCodeService(ILogger<EscrowCodeService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateEscrowAsync(string code, string creatorId, string[] reviewerIds, CancellationToken ct = default)
    {
        var escrowId = Guid.NewGuid().ToString("N");
        _escrows[escrowId] = new EscrowEntry
        {
            EscrowId = escrowId,
            Code = code,
            CreatorId = creatorId,
            ReviewerIds = reviewerIds,
            State = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("Created escrow {EscrowId} for creator {CreatorId}", escrowId, creatorId);
        return Task.FromResult(escrowId);
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

        entry.State = "Released";
        entry.ReleasedAt = DateTime.UtcNow;
        _logger.LogInformation("Escrow {EscrowId} released", escrowId);
        
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
        public DateTime CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }
}
