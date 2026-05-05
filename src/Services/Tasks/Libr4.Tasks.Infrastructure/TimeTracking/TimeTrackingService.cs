/*
using Libr4.Tasks.Domain.TimeTracking.FSharp;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.Extensions.Logging;

namespace Libr4.Tasks.Infrastructure.TimeTracking;

/// <summary>
/// C# Bridge for F# Time Tracking Domain Logic.
/// Follows Golden Stack: C# (Skeleton) calls F# (Brain) for domain logic.
/// </summary>
public interface ITimeTrackingService
{
    Task<TimeEntry> StartEntryAsync(Guid userId, Guid taskId, string description, Guid? projectId);
    Task<TimeEntry> StopEntryAsync(Guid entryId);
    Task<TimeEntry> PauseEntryAsync(Guid entryId);
    Task<TimeEntry> ResumeEntryAsync(Guid entryId);
    Task<FinancialReport> CalculateFinancialsAsync(Guid userId, DateTime startDate, DateTime endDate, decimal? hourlyRate);
    Task<DailySummary> GetDailySummaryAsync(Guid userId, DateTime date);
    Task<IReadOnlyList<TimeEntry>> GetEntriesByPeriodAsync(Guid userId, DateTime startDate, DateTime endDate);
}

/// <summary>
/// Bridge implementation that calls F# domain logic
/// </summary>
public class TimeTrackingService : ITimeTrackingService
{
    private readonly ITimeEntryRepository _repository;
    private readonly ILogger<TimeTrackingService> _logger;

    public TimeTrackingService(
        ITimeEntryRepository repository,
        ILogger<TimeTrackingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TimeEntry> StartEntryAsync(Guid userId, Guid taskId, string description, Guid? projectId)
    {
        // Call F# pure function for domain logic
        var fsharpEntry = TimeTrackingLogic.startEntry(
            userId, 
            taskId, 
            description, 
            OptionModule.OfObj(projectId));

        // Convert to C# type and persist
        var entry = MapToCSharp(fsharpEntry);
        await _repository.AddAsync(entry);

        _logger.LogInformation(
            "Started time entry {EntryId} for user {UserId} on task {TaskId}",
            entry.Id, userId, taskId);

        return entry;
    }

    public async Task<TimeEntry> StopEntryAsync(Guid entryId)
    {
        var existing = await _repository.GetByIdAsync(entryId)
            ?? throw new InvalidOperationException($"Time entry {entryId} not found");

        var fsharpEntry = MapToFSharp(existing);
        var stoppedEntry = TimeTrackingLogic.stopEntry(fsharpEntry);
        
        var updated = MapToCSharp(stoppedEntry);
        await _repository.UpdateAsync(updated);

        _logger.LogInformation("Stopped time entry {EntryId}, duration: {Duration}",
            entryId, updated.Duration);

        return updated;
    }

    public async Task<TimeEntry> PauseEntryAsync(Guid entryId)
    {
        var existing = await _repository.GetByIdAsync(entryId)
            ?? throw new InvalidOperationException($"Time entry {entryId} not found");

        var fsharpEntry = MapToFSharp(existing);
        var pausedEntry = TimeTrackingLogic.pauseEntry(fsharpEntry);
        
        var updated = MapToCSharp(pausedEntry);
        await _repository.UpdateAsync(updated);

        return updated;
    }

    public async Task<TimeEntry> ResumeEntryAsync(Guid entryId)
    {
        var existing = await _repository.GetByIdAsync(entryId)
            ?? throw new InvalidOperationException($"Time entry {entryId} not found");

        var fsharpEntry = MapToFSharp(existing);
        var resumedEntry = TimeTrackingLogic.resumeEntry(fsharpEntry);
        
        var updated = MapToCSharp(resumedEntry);
        await _repository.UpdateAsync(updated);

        return updated;
    }

    public async Task<FinancialReport> CalculateFinancialsAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate, 
        decimal? hourlyRate)
    {
        var entries = await _repository.GetByUserAndDateRangeAsync(userId, startDate, endDate);
        var fsharpEntries = ListModule.OfSeq(entries.Select(MapToFSharp));

        // Call F# pure function for calculation
        var financials = TimeTrackingLogic.calculateFinancials(
            OptionModule.OfObj(hourlyRate),
            fsharpEntries);

        return new FinancialReport
        {
            TotalHours = financials.TotalHours,
            BillableHours = financials.BillableHours,
            NonBillableHours = financials.NonBillableHours,
            HourlyRate = financials.HourlyRate,
            TotalAmount = financials.TotalAmount,
            Currency = financials.Currency
        };
    }

    public async Task<DailySummary> GetDailySummaryAsync(Guid userId, DateTime date)
    {
        var entries = await _repository.GetByUserAndDateAsync(userId, date);
        var fsharpEntries = ListModule.OfSeq(entries.Select(MapToFSharp));

        // Call F# pure function
        var summary = TimeTrackingLogic.dailySummary(date, fsharpEntries);

        return new DailySummary
        {
            Date = summary.Date,
            TotalEntries = summary.TotalEntries,
            TotalHours = (decimal)summary.TotalHours,
            BillableHours = summary.BillableHours,
            NonBillableHours = summary.NonBillableHours,
            Entries = entries.ToList()
        };
    }

    public async Task<IReadOnlyList<TimeEntry>> GetEntriesByPeriodAsync(
        Guid userId, 
        DateTime startDate, 
        DateTime endDate)
    {
        return await _repository.GetByUserAndDateRangeAsync(userId, startDate, endDate);
    }

    // Mapping helpers between C# and F#
    private static Libr4.Tasks.Domain.TimeTracking.FSharp.TimeEntry MapToFSharp(TimeEntry csharp)
    {
        return new Libr4.Tasks.Domain.TimeTracking.FSharp.TimeEntry(
            csharp.Id,
            csharp.UserId,
            csharp.TaskId,
            OptionModule.OfObj(csharp.ProjectId),
            csharp.Description,
            csharp.StartedAt,
            OptionModule.OfObj(csharp.EndedAt),
            csharp.Duration,
            MapStatusToFSharp(csharp.Status),
            MapBillingToFSharp(csharp.BillingStatus),
            ListModule.OfSeq(csharp.Tags),
            csharp.CreatedAt,
            OptionModule.OfObj(csharp.UpdatedAt)
        );
    }

    private static TimeEntry MapToCSharp(Libr4.Tasks.Domain.TimeTracking.FSharp.TimeEntry fsharp)
    {
        return new TimeEntry
        {
            Id = fsharp.Id,
            UserId = fsharp.UserId,
            TaskId = fsharp.TaskId,
            ProjectId = OptionModule.GetValueWithDefault(fsharp.ProjectId, null),
            Description = fsharp.Description,
            StartedAt = fsharp.StartedAt,
            EndedAt = OptionModule.GetValueWithDefault(fsharp.EndedAt, null),
            Duration = fsharp.Duration,
            Status = MapStatusToCSharp(fsharp.Status),
            BillingStatus = MapBillingToCSharp(fsharp.BillingStatus),
            Tags = fsharp.Tags.ToList(),
            CreatedAt = fsharp.CreatedAt,
            UpdatedAt = OptionModule.GetValueWithDefault(fsharp.UpdatedAt, null)
        };
    }

    private static Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus MapStatusToFSharp(TimeEntryStatus csharp) =>
        csharp switch
        {
            TimeEntryStatus.Active => Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Active,
            TimeEntryStatus.Paused => Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Paused,
            TimeEntryStatus.Completed => Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Completed,
            TimeEntryStatus.Billable => Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Active,
            TimeEntryStatus.NonBillable => Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Active,
            _ => throw new ArgumentOutOfRangeException(nameof(csharp))
        };

    private static TimeEntryStatus MapStatusToCSharp(Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus fsharp) =>
        fsharp switch
        {
            Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Active => TimeEntryStatus.Active,
            Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Paused => TimeEntryStatus.Paused,
            Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Completed => TimeEntryStatus.Completed,
            Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Abandoned => TimeEntryStatus.NonBillable,
            Libr4.Tasks.Domain.TimeTracking.FSharp.SessionStatus.Flagged => TimeEntryStatus.NonBillable,
            _ => throw new ArgumentOutOfRangeException(nameof(fsharp))
        };

    private static Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus MapBillingToFSharp(BillingStatus csharp) =>
        csharp switch
        {
            BillingStatus.Pending => Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Pending,
            BillingStatus.Approved a => Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Approved(a.ApproverId),
            BillingStatus.Inviced i => Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Invoiced(i.InvoiceId),
            BillingStatus.Paid p => Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Paid(p.PaymentDate),
            _ => throw new ArgumentOutOfRangeException(nameof(csharp))
        };

    private static BillingStatus MapBillingToCSharp(Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus fsharp) =>
        fsharp switch
        {
            Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Pending => BillingStatus.Pending,
            Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Approved approverId => new BillingStatus.Approved(approverId),
            Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Invoiced invoiceId => new BillingStatus.Inviced(invoiceId),
            Libr4.Tasks.Domain.TimeTracking.FSharp.BillingStatus.Paid paymentDate => new BillingStatus.Paid(paymentDate),
            _ => throw new ArgumentOutOfRangeException(nameof(fsharp))
        };
}

// C# Domain Types (EF Core compatible)
public class TimeEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeEntryStatus Status { get; set; }
    public BillingStatus BillingStatus { get; set; } = BillingStatus.Pending;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum TimeEntryStatus
{
    Active,
    Paused,
    Completed,
    Billable,
    NonBillable
}

public abstract record BillingStatus
{
    public record Pending : BillingStatus;
    public record Approved(string ApproverId) : BillingStatus;
    public record Invoiced(string InvoiceId) : BillingStatus;
    public record Paid(DateTime PaymentDate) : BillingStatus;
}

public class FinancialReport
{
    public decimal TotalHours { get; set; }
    public decimal BillableHours { get; set; }
    public decimal NonBillableHours { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
}

public class DailySummary
{
    public DateTime Date { get; set; }
    public int TotalEntries { get; set; }
    public decimal TotalHours { get; set; }
    public decimal BillableHours { get; set; }
    public decimal NonBillableHours { get; set; }
    public List<TimeEntry> Entries { get; set; } = new();
}

public interface ITimeEntryRepository
{
    Task AddAsync(TimeEntry entry);
    Task UpdateAsync(TimeEntry entry);
    Task<TimeEntry?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<TimeEntry>> GetByUserAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<TimeEntry>> GetByUserAndDateAsync(Guid userId, DateTime date);
}
*/
