using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Kyc;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Kyc;

public record KycVerificationDto(
    Guid Id, Guid UserId, KycLevel Level, KycStatus Status, string? Provider,
    string? FullName, string? Nationality, RiskRating RiskRating, bool IsPep, bool SanctionsHit,
    string? RejectionReason, DateTimeOffset CreatedAt, DateTimeOffset? VerifiedAt, DateTimeOffset? ExpiresAt);

// === Initiate verification ===
public record InitiateKycCommand(Guid UserId, KycLevel Level, string Provider) : IRequest<Result<Guid>>;

public sealed class InitiateKycHandler(IAuthDbContext db) : IRequestHandler<InitiateKycCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(InitiateKycCommand req, CancellationToken ct)
    {
        var existing = await db.KycVerifications.FirstOrDefaultAsync(x =>
            x.UserId == req.UserId && (x.Status == KycStatus.Pending || x.Status == KycStatus.UnderReview), ct);
        if (existing is not null) return Result.Success(existing.Id);

        var v = KycVerification.Initiate(req.UserId, req.Level, req.Provider, DateTimeOffset.UtcNow);
        await db.KycVerifications.AddAsync(v, ct);
        await db.SaveChangesAsync(ct);
        return Result.Success(v.Id);
    }
}

// === Submit personal data ===
public record SubmitKycPersonalDataCommand(
    Guid VerificationId,
    string FullName,
    DateOnly DateOfBirth,
    string Nationality,
    string CountryOfResidence,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string PostalCode) : IRequest<Result>;

public sealed class SubmitKycPersonalDataValidator : AbstractValidator<SubmitKycPersonalDataCommand>
{
    public SubmitKycPersonalDataValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Nationality).NotEmpty().Length(2, 3);
        RuleFor(x => x.CountryOfResidence).NotEmpty().Length(2, 3);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            .WithMessage("Must be 18 years or older");
    }
}

public sealed class SubmitKycPersonalDataHandler(IAuthDbContext db) : IRequestHandler<SubmitKycPersonalDataCommand, Result>
{
    public async Task<Result> Handle(SubmitKycPersonalDataCommand req, CancellationToken ct)
    {
        var v = await db.KycVerifications.FirstOrDefaultAsync(x => x.Id == req.VerificationId, ct);
        if (v is null) return Result.Failure(Error.NotFound("kyc.not_found", "Verification not found"));
        v.SubmitPersonalData(req.FullName, req.DateOfBirth, req.Nationality, req.CountryOfResidence,
            req.AddressLine1, req.AddressLine2, req.City, req.PostalCode, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// === Upload document ===
public record UploadKycDocumentCommand(Guid VerificationId, KycDocumentType Type, string FileUrl, string? Country)
    : IRequest<Result<Guid>>;

public sealed class UploadKycDocumentHandler(IAuthDbContext db) : IRequestHandler<UploadKycDocumentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadKycDocumentCommand req, CancellationToken ct)
    {
        var v = await db.KycVerifications.Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == req.VerificationId, ct);
        if (v is null) return Result.Failure<Guid>(Error.NotFound("kyc.not_found", "Verification not found"));
        var doc = v.AddDocument(req.Type, req.FileUrl, req.Country, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success(doc.Id);
    }
}

// === Approve / Reject (admin) ===
public record ApproveKycCommand(Guid VerificationId, RiskRating Risk, bool IsPep) : IRequest<Result>;

public sealed class ApproveKycHandler(IAuthDbContext db) : IRequestHandler<ApproveKycCommand, Result>
{
    public async Task<Result> Handle(ApproveKycCommand req, CancellationToken ct)
    {
        var v = await db.KycVerifications.FirstOrDefaultAsync(x => x.Id == req.VerificationId, ct);
        if (v is null) return Result.Failure(Error.NotFound("kyc.not_found", "Verification not found"));
        v.Approve(req.Risk, req.IsPep, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record RejectKycCommand(Guid VerificationId, string Reason) : IRequest<Result>;

public sealed class RejectKycHandler(IAuthDbContext db) : IRequestHandler<RejectKycCommand, Result>
{
    public async Task<Result> Handle(RejectKycCommand req, CancellationToken ct)
    {
        var v = await db.KycVerifications.FirstOrDefaultAsync(x => x.Id == req.VerificationId, ct);
        if (v is null) return Result.Failure(Error.NotFound("kyc.not_found", "Verification not found"));
        v.Reject(req.Reason, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// === Get my verification ===
public record GetMyKycQuery(Guid UserId) : IRequest<Result<KycVerificationDto?>>;

public sealed class GetMyKycHandler(IAuthDbContext db) : IRequestHandler<GetMyKycQuery, Result<KycVerificationDto?>>
{
    public async Task<Result<KycVerificationDto?>> Handle(GetMyKycQuery req, CancellationToken ct)
    {
        var v = await db.KycVerifications.AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (v is null) return Result.Success<KycVerificationDto?>(null);
        return Result.Success<KycVerificationDto?>(new KycVerificationDto(
            v.Id, v.UserId, v.Level, v.Status, v.Provider, v.FullName, v.Nationality,
            v.RiskRating, v.IsPep, v.SanctionsHit, v.RejectionReason, v.CreatedAt, v.VerifiedAt, v.ExpiresAt));
    }
}
