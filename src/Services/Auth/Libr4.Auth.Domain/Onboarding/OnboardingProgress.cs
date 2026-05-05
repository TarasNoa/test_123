using Libr4.Shared.Kernel.Domain;

namespace Libr4.Auth.Domain.Onboarding;

public sealed class OnboardingProgress : AggregateRoot<Guid>
{
    private readonly List<OnboardingStep> _steps = new();

    public Guid UserId { get; private set; }
    public OnboardingFlow Flow { get; private set; }
    public OnboardingStatus Status { get; private set; }
    public int CurrentStepIndex { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<OnboardingStep> Steps => _steps.AsReadOnly();

    private OnboardingProgress() { }

    public static OnboardingProgress Start(Guid userId, OnboardingFlow flow, IEnumerable<string> stepKeys, DateTimeOffset now)
    {
        var p = new OnboardingProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Flow = flow,
            Status = OnboardingStatus.InProgress,
            StartedAt = now,
            UpdatedAt = now,
        };
        var idx = 0;
        foreach (var key in stepKeys)
            p._steps.Add(new OnboardingStep(p.Id, key, idx++));
        return p;
    }

    public bool CompleteStep(string stepKey, string? payloadJson, DateTimeOffset now)
    {
        var step = _steps.FirstOrDefault(s => s.Key == stepKey);
        if (step is null) return false;
        step.Complete(payloadJson, now);
        UpdatedAt = now;

        var next = _steps.FirstOrDefault(s => !s.IsCompleted);
        if (next is null)
        {
            Status = OnboardingStatus.Completed;
            CompletedAt = now;
        }
        else
        {
            CurrentStepIndex = next.Order;
        }
        return true;
    }

    public bool SkipStep(string stepKey, DateTimeOffset now)
    {
        var step = _steps.FirstOrDefault(s => s.Key == stepKey);
        if (step is null || !step.IsOptional) return false;
        step.Skip(now);
        UpdatedAt = now;
        return true;
    }

    public void Abandon(DateTimeOffset now)
    {
        Status = OnboardingStatus.Abandoned;
        UpdatedAt = now;
    }
}

public sealed class OnboardingStep
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProgressId { get; private set; }
    public string Key { get; private set; } = "";
    public int Order { get; private set; }
    public bool IsOptional { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsSkipped { get; private set; }
    public string? PayloadJson { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private OnboardingStep() { }
    internal OnboardingStep(Guid progressId, string key, int order, bool optional = false)
    {
        ProgressId = progressId;
        Key = key;
        Order = order;
        IsOptional = optional;
    }

    internal void Complete(string? payload, DateTimeOffset now)
    {
        IsCompleted = true;
        PayloadJson = payload;
        CompletedAt = now;
    }

    internal void Skip(DateTimeOffset now)
    {
        IsSkipped = true;
        CompletedAt = now;
    }
}

public enum OnboardingFlow { Freelancer = 0, Client = 1, Team = 2, Admin = 3 }
public enum OnboardingStatus { InProgress = 0, Completed = 1, Abandoned = 2 }
