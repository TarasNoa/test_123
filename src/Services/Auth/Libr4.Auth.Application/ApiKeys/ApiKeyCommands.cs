using System.Security.Cryptography;
using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.ApiKeys;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.ApiKeys;

public record ApiKeyDto(Guid Id, string Name, string KeyPrefix, ApiKeyScope Scopes,
    DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? RevokedAt);

public record IssueApiKeyCommand(Guid UserId, string Name, ApiKeyScope Scopes, TimeSpan? Lifetime)
    : IRequest<Result<IssueApiKeyResponse>>;

public record IssueApiKeyResponse(Guid Id, string Name, string Secret, ApiKeyScope Scopes, DateTimeOffset? ExpiresAt);

public sealed class IssueApiKeyValidator : AbstractValidator<IssueApiKeyCommand>
{
    public IssueApiKeyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Scopes).NotEqual(ApiKeyScope.None).WithMessage("At least one scope is required");
    }
}

public sealed class IssueApiKeyHandler(IAuthDbContext db) : IRequestHandler<IssueApiKeyCommand, Result<IssueApiKeyResponse>>
{
    public async Task<Result<IssueApiKeyResponse>> Handle(IssueApiKeyCommand req, CancellationToken ct)
    {
        // Generate secret
        var bytes = RandomNumberGenerator.GetBytes(32);
        var secret = "l4_" + Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var prefix = secret[..12];
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret)));

        var key = ApiKey.Issue(req.UserId, req.Name, hash, prefix, req.Scopes, DateTimeOffset.UtcNow, req.Lifetime);
        await db.ApiKeys.AddAsync(key, ct);
        await db.SaveChangesAsync(ct);

        return Result.Success(new IssueApiKeyResponse(key.Id, key.Name, secret, key.Scopes, key.ExpiresAt));
    }
}

public record RevokeApiKeyCommand(Guid UserId, Guid KeyId, string? Reason) : IRequest<Result>;

public sealed class RevokeApiKeyHandler(IAuthDbContext db) : IRequestHandler<RevokeApiKeyCommand, Result>
{
    public async Task<Result> Handle(RevokeApiKeyCommand req, CancellationToken ct)
    {
        var key = await db.ApiKeys.FirstOrDefaultAsync(x => x.Id == req.KeyId && x.UserId == req.UserId, ct);
        if (key is null) return Result.Failure(Error.NotFound("apikey.not_found", "API key not found"));
        key.Revoke(req.Reason, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record ListApiKeysQuery(Guid UserId) : IRequest<Result<IReadOnlyList<ApiKeyDto>>>;

public sealed class ListApiKeysHandler(IAuthDbContext db) : IRequestHandler<ListApiKeysQuery, Result<IReadOnlyList<ApiKeyDto>>>
{
    public async Task<Result<IReadOnlyList<ApiKeyDto>>> Handle(ListApiKeysQuery req, CancellationToken ct)
    {
        var items = await db.ApiKeys.AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ApiKeyDto(x.Id, x.Name, x.KeyPrefix, x.Scopes, x.CreatedAt, x.LastUsedAt, x.ExpiresAt, x.RevokedAt))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<ApiKeyDto>>(items);
    }
}
