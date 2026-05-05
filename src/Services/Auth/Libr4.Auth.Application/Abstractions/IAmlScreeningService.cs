using Libr4.Auth.Domain.Kyc;

namespace Libr4.Auth.Application.Abstractions;

public interface IAmlScreeningService
{
    Task<AmlScreeningResult> ScreenAsync(string fullName, DateOnly dateOfBirth, string nationality, string country, CancellationToken ct);
}

public sealed record AmlScreeningResult(
    bool IsPep,
    bool SanctionsHit,
    RiskRating RiskRating,
    string? Details,
    string? ExternalRefId
);
