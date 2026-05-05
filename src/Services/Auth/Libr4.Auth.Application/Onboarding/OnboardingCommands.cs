using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Onboarding;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Onboarding;

public record OnboardingProgressDto(
    Guid Id, OnboardingFlow Flow, OnboardingStatus Status, int CurrentStepIndex,
    IReadOnlyList<OnboardingStepDto> Steps);
public record OnboardingStepDto(string Key, int Order, bool IsOptional, bool IsCompleted, bool IsSkipped);

public static class OnboardingFlowDefinitions
{
    public static IReadOnlyList<string> StepsFor(OnboardingFlow flow) => flow switch
    {
        OnboardingFlow.Freelancer => new[] { "welcome", "verify_email", "profile_basics", "skills", "portfolio", "hourly_rate", "kyc", "tour" },
        OnboardingFlow.Client => new[] { "welcome", "verify_email", "company_info", "billing", "first_task" },
        OnboardingFlow.Team => new[] { "welcome", "create_org", "invite_members", "billing" },
        _ => new[] { "welcome" }
    };
}

public record StartOnboardingCommand(Guid UserId, OnboardingFlow Flow) : IRequest<Result<Guid>>;

public sealed class StartOnboardingHandler(IAuthDbContext db) : IRequestHandler<StartOnboardingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartOnboardingCommand req, CancellationToken ct)
    {
        var existing = await db.OnboardingProgresses.FirstOrDefaultAsync(
            x => x.UserId == req.UserId && x.Flow == req.Flow, ct);
        if (existing is not null) return Result.Success(existing.Id);

        var p = OnboardingProgress.Start(req.UserId, req.Flow,
            OnboardingFlowDefinitions.StepsFor(req.Flow), DateTimeOffset.UtcNow);
        await db.OnboardingProgresses.AddAsync(p, ct);
        await db.SaveChangesAsync(ct);
        return Result.Success(p.Id);
    }
}

public record CompleteOnboardingStepCommand(Guid UserId, OnboardingFlow Flow, string StepKey, string? PayloadJson)
    : IRequest<Result>;

public sealed class CompleteOnboardingStepHandler(IAuthDbContext db) : IRequestHandler<CompleteOnboardingStepCommand, Result>
{
    public async Task<Result> Handle(CompleteOnboardingStepCommand req, CancellationToken ct)
    {
        var p = await db.OnboardingProgresses.Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.UserId == req.UserId && x.Flow == req.Flow, ct);
        if (p is null) return Result.Failure(Error.NotFound("onboarding.not_found", "Onboarding not started"));
        if (!p.CompleteStep(req.StepKey, req.PayloadJson, DateTimeOffset.UtcNow))
            return Result.Failure(Error.NotFound("onboarding.step_not_found", "Step not found"));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record GetMyOnboardingQuery(Guid UserId, OnboardingFlow Flow) : IRequest<Result<OnboardingProgressDto?>>;

public sealed class GetMyOnboardingHandler(IAuthDbContext db) : IRequestHandler<GetMyOnboardingQuery, Result<OnboardingProgressDto?>>
{
    public async Task<Result<OnboardingProgressDto?>> Handle(GetMyOnboardingQuery req, CancellationToken ct)
    {
        var p = await db.OnboardingProgresses.AsNoTracking().Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.UserId == req.UserId && x.Flow == req.Flow, ct);
        if (p is null) return Result.Success<OnboardingProgressDto?>(null);
        return Result.Success<OnboardingProgressDto?>(new OnboardingProgressDto(
            p.Id, p.Flow, p.Status, p.CurrentStepIndex,
            p.Steps.OrderBy(s => s.Order).Select(s => new OnboardingStepDto(s.Key, s.Order, s.IsOptional, s.IsCompleted, s.IsSkipped)).ToList()));
    }
}
