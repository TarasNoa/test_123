using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Application.Dtos;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record SetupTwoFactorCommand(Guid UserId) : IRequest<Result<TwoFactorSetupResponse>>;

public sealed class SetupTwoFactorHandler : IRequestHandler<SetupTwoFactorCommand, Result<TwoFactorSetupResponse>>
{
    private readonly IAuthDbContext _db;
    private readonly ITotpService _totp;

    public SetupTwoFactorHandler(IAuthDbContext db, ITotpService totp)
    {
        _db = db;
        _totp = totp;
    }

    public async Task<Result<TwoFactorSetupResponse>> Handle(SetupTwoFactorCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null) return Result.Failure<TwoFactorSetupResponse>(AuthErrors.UserNotFound);
        if (user.TwoFactorEnabled) return Result.Failure<TwoFactorSetupResponse>(AuthErrors.TwoFactorAlreadyEnabled);

        var setup = _totp.GenerateSetup(user.Email);
        user.EnableTwoFactor(setup.EncryptedSecret);
        await _db.SaveChangesAsync(ct);

        return new TwoFactorSetupResponse(setup.OtpAuthUri, Convert.ToBase64String(setup.QrPng));
    }
}

public sealed record VerifyTwoFactorCommand(Guid UserId, string Code) : IRequest<Result>;

public sealed class VerifyTwoFactorHandler : IRequestHandler<VerifyTwoFactorCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ITotpService _totp;

    public VerifyTwoFactorHandler(IAuthDbContext db, ITotpService totp)
    {
        _db = db;
        _totp = totp;
    }

    public async Task<Result> Handle(VerifyTwoFactorCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null || string.IsNullOrEmpty(user.TwoFactorSecretEncrypted))
            return Result.Failure(AuthErrors.UserNotFound);
        if (!_totp.VerifyCode(user.TwoFactorSecretEncrypted, request.Code))
            return Result.Failure(AuthErrors.TwoFactorInvalid);
        return Result.Success();
    }
}

public sealed record DisableTwoFactorCommand(Guid UserId, string Password) : IRequest<Result>;

public sealed class DisableTwoFactorHandler : IRequestHandler<DisableTwoFactorCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DisableTwoFactorHandler(IAuthDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null) return Result.Failure(AuthErrors.UserNotFound);
        if (!_hasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure(AuthErrors.InvalidCredentials);

        user.DisableTwoFactor();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
