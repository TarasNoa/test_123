using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Application.Dtos;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record RegisterUserCommand(Dtos.RegisterRequest Payload) : IRequest<Result<UserDto>>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Payload.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Payload.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(64);
        RuleFor(x => x.Payload.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Za-z]").WithMessage("Password must contain letters")
            .Matches(@"\d").WithMessage("Password must contain digits");
    }
}

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IAuthDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IPublishEndpoint _bus;

    public RegisterUserHandler(IAuthDbContext db, IPasswordHasher hasher, IClock clock, IPublishEndpoint bus)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _bus = bus;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var email = request.Payload.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
            return Result.Failure<UserDto>(AuthErrors.EmailAlreadyExists);

        var user = User.Register(email, request.Payload.DisplayName, _hasher.Hash(request.Payload.Password), _clock.UtcNow);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new UserRegisteredIntegrationEvent(user.Id, user.Email, user.DisplayName, _clock.UtcNow), ct);

        return new UserDto(
            user.Id, user.Email, user.DisplayName,
            user.Roles.Select(r => r.Name).ToList(),
            user.EmailConfirmed, user.TwoFactorEnabled, user.CreatedAt);
    }
}
