using System;
using System.Collections.Generic;

namespace Libr4.Payments.Domain.Budgets;

public enum BudgetPeriod { Daily, Weekly, Monthly, Yearly }
public enum AlertLevel { Warning, Critical }

public class BudgetCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public decimal AlertThreshold { get; set; } = 0.8m;
}

public class Budget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BudgetPeriod Period { get; set; }
    public decimal TotalLimit { get; set; }
    public decimal TotalSpent { get; set; }
    public List<BudgetCategory> Categories { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public decimal RemainingAmount => TotalLimit - TotalSpent;
    public decimal SpentPercentage => TotalLimit > 0 ? (TotalSpent / TotalLimit) * 100 : 0;
    public bool IsExceeded => TotalSpent > TotalLimit;

    public void RecordSpending(Guid categoryId, decimal amount, DateTimeOffset now)
    {
        var category = Categories.Find(c => c.Id == categoryId);
        if (category != null)
        {
            category.Spent += amount;
            TotalSpent += amount;
        }
    }

    public List<(Guid CategoryId, AlertLevel Level)> GetAlerts()
    {
        var alerts = new List<(Guid, AlertLevel)>();
        foreach (var cat in Categories)
        {
            var ratio = cat.Spent / cat.Limit;
            if (ratio >= 1) alerts.Add((cat.Id, AlertLevel.Critical));
            else if (ratio >= cat.AlertThreshold) alerts.Add((cat.Id, AlertLevel.Warning));
        }
        return alerts;
    }
}
