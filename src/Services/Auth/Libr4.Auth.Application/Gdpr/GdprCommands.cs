using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Gdpr;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Gdpr;

public record GdprRequestDto(Guid Id, GdprRequestType Type, GdprRequestStatus Status,
    DateTimeOffset RequestedAt, DateTimeOffset? ProcessedAt, DateTimeOffset? ScheduledFor, string? ExportFileUrl);

public record SubmitGdprRequestCommand(Guid UserId, GdprRequestType Type, string? Reason) : IRequest<Result<Guid>>;

public sealed class SubmitGdprRequestHandler(IAuthDbContext db) : IRequestHandler<SubmitGdprRequestCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitGdprRequestCommand req, CancellationToken ct)
    {
        var existing = await db.GdprRequests.FirstOrDefaultAsync(
            x => x.UserId == req.UserId && x.Type == req.Type && x.Status == GdprRequestStatus.Pending, ct);
        if (existing is not null) return Result.Success(existing.Id);

        var r = GdprRequest.Submit(req.UserId, req.Type, req.Reason, DateTimeOffset.UtcNow);
        await db.GdprRequests.AddAsync(r, ct);
        await db.SaveChangesAsync(ct);
        return Result.Success(r.Id);
    }
}

public record CancelGdprRequestCommand(Guid UserId, Guid RequestId) : IRequest<Result>;

public sealed class CancelGdprRequestHandler(IAuthDbContext db) : IRequestHandler<CancelGdprRequestCommand, Result>
{
    public async Task<Result> Handle(CancelGdprRequestCommand req, CancellationToken ct)
    {
        var r = await db.GdprRequests.FirstOrDefaultAsync(x => x.Id == req.RequestId && x.UserId == req.UserId, ct);
        if (r is null) return Result.Failure(Error.NotFound("gdpr.not_found", "Request not found"));
        r.Cancel(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record GetMyGdprRequestsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<GdprRequestDto>>>;

public sealed class GetMyGdprRequestsHandler(IAuthDbContext db) : IRequestHandler<GetMyGdprRequestsQuery, Result<IReadOnlyList<GdprRequestDto>>>
{
    public async Task<Result<IReadOnlyList<GdprRequestDto>>> Handle(GetMyGdprRequestsQuery req, CancellationToken ct)
    {
        var items = await db.GdprRequests.AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .OrderByDescending(x => x.RequestedAt)
            .Select(x => new GdprRequestDto(x.Id, x.Type, x.Status, x.RequestedAt, x.ProcessedAt, x.ScheduledFor, x.ExportFileUrl))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<GdprRequestDto>>(items);
    }
}

public record RecordConsentCommand(Guid UserId, ConsentType Type, string Version, bool Granted,
    string? IpAddress, string? UserAgent) : IRequest<Result>;

public sealed class RecordConsentHandler(IAuthDbContext db) : IRequestHandler<RecordConsentCommand, Result>
{
    public async Task<Result> Handle(RecordConsentCommand req, CancellationToken ct)
    {
        var c = ConsentRecord.Record(req.UserId, req.Type, req.Version, req.Granted,
            req.IpAddress, req.UserAgent, DateTimeOffset.UtcNow);
        await db.Consents.AddAsync(c, ct);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
