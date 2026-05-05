using System;

namespace Libr4.Payments.Domain.FinancialGoals;

public enum GoalStatus { Active, Completed, Abandoned, Paused }
public enum GoalFrequency { Weekly, Monthly, Quarterly, Yearly }

public class FinancialGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTimeOffset TargetDate { get; set; }
    public GoalFrequency ContributionFrequency { get; set; }
    public decimal? SuggestedContribution { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public decimal ProgressPercentage => TargetAmount > 0 ? (CurrentAmount / TargetAmount) * 100 : 0;
    public bool IsCompleted => CurrentAmount >= TargetAmount;
    public int DaysRemaining => (int)(TargetDate - DateTimeOffset.UtcNow).TotalDays;

    public void AddContribution(decimal amount, DateTimeOffset now)
    {
        CurrentAmount += amount;
        if (IsCompleted) Status = GoalStatus.Completed;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now) { Status = GoalStatus.Completed; UpdatedAt = now; }
    public void Pause(DateTimeOffset now) { Status = GoalStatus.Paused; UpdatedAt = now; }
    public void Abandon(DateTimeOffset now) { Status = GoalStatus.Abandoned; UpdatedAt = now; }
}
