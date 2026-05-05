namespace Libr4.Payments.Application.Abstractions;

/// <summary>
/// Repository for accessing fraud detection history.
/// </summary>
public interface IFraudHistoryRepository
{
    /// <summary>
    /// Get count of previous fraud incidents for a user.
    /// </summary>
    Task<int> GetFraudCountAsync(Guid userId, CancellationToken ct = default);
    
    /// <summary>
    /// Record a new fraud incident.
    /// </summary>
    Task RecordFraudAsync(Guid userId, string reason, CancellationToken ct = default);
}
