using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Kyc;
using Libr4.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Kyc;

public static class AmlScreeningCommands
{
    public sealed record PerformAmlScreeningCommand(Guid KycVerificationId) : IRequest<AmlScreeningResponse>;

    public sealed record AmlScreeningResponse(
        bool IsPep,
        bool SanctionsHit,
        string RiskRating,
        string? Details,
        string? ExternalRefId
    );

    public sealed class PerformAmlScreeningValidator : AbstractValidator<PerformAmlScreeningCommand>
    {
        public PerformAmlScreeningValidator()
        {
            RuleFor(x => x.KycVerificationId).NotEmpty();
        }
    }

    public sealed class PerformAmlScreeningHandler : IRequestHandler<PerformAmlScreeningCommand, AmlScreeningResponse>
    {
        private readonly IAuthDbContext _db;
        private readonly IAmlScreeningService _amlService;

        public PerformAmlScreeningHandler(IAuthDbContext db, IAmlScreeningService amlService)
        {
            _db = db;
            _amlService = amlService;
        }

        public async Task<AmlScreeningResponse> Handle(PerformAmlScreeningCommand cmd, CancellationToken ct)
        {
            var kyc = await _db.KycVerifications
                .FirstOrDefaultAsync(x => x.Id == cmd.KycVerificationId, ct)
                ?? throw new DomainException("KYC verification not found");

            if (kyc.FullName == null || kyc.DateOfBirth == null || kyc.Nationality == null || kyc.CountryOfResidence == null)
                throw new DomainException("Personal data is incomplete");

            var result = await _amlService.ScreenAsync(
                kyc.FullName,
                kyc.DateOfBirth.Value,
                kyc.Nationality,
                kyc.CountryOfResidence,
                ct
            );

            kyc.RecordCheck(
                KycCheckType.AmlRisk,
                result.SanctionsHit ? KycCheckResult.SanctionsHit : KycCheckResult.Pass,
                result.Details,
                DateTimeOffset.UtcNow
            );

            _db.KycVerifications.Update(kyc);
            await _db.SaveChangesAsync(ct);

            return new AmlScreeningResponse(
                result.IsPep,
                result.SanctionsHit,
                result.RiskRating.ToString(),
                result.Details,
                result.ExternalRefId
            );
        }
    }
}
