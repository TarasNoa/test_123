namespace Libr4.IDE.Application.Escrow;

/// <summary>
/// Interface for escrow code service
/// </summary>
public interface IEscrowCodeService
{
    Task<string> CreateEscrowAsync(string code, string creatorId, string[] reviewerIds, CancellationToken ct = default);
    Task<EscrowStatus> GetStatusAsync(string escrowId, CancellationToken ct = default);
    Task<bool> ApproveAsync(string escrowId, string reviewerId, string? comments = null, CancellationToken ct = default);
    Task<bool> RejectAsync(string escrowId, string reviewerId, string reason, CancellationToken ct = default);
    Task<string?> ReleaseAsync(string escrowId, CancellationToken ct = default);
}

public class EscrowStatus
{
    public string EscrowId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // "Pending", "Approved", "Rejected", "Released"
    public string CreatorId { get; set; } = string.Empty;
    public string[] ReviewerIds { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Reviews { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}
