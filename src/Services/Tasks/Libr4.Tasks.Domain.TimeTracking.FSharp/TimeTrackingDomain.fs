namespace Libr4.Tasks.Domain.TimeTracking.FSharp

open System
open Microsoft.FSharp.Collections

/// Time tracking domain types using F# Discriminated Unions
type TimeEntryDomainStatus =
    | Active
    | Paused
    | Completed
    | Billable
    | NonBillable

type TimeSessionState =
    | Idle
    | Running of startTime: DateTime * description: string
    | PausedSession of elapsed: TimeSpan * description: string
    | Stopped of elapsed: TimeSpan * description: string

type BillingStatus =
    | Pending
    | Approved of approverId: string
    | Invoiced of invoiceId: string
    | Paid of paymentDate: DateTime

type TimeEntryDomain = {
    Id: Guid
    UserId: Guid
    TaskId: Guid
    ProjectId: Guid option
    Description: string
    StartedAt: DateTime
    EndedAt: DateTime option
    Duration: TimeSpan
    Status: TimeEntryDomainStatus
    BillingStatus: BillingStatus
    Tags: string list
    CreatedAt: DateTime
    UpdatedAt: DateTime option
}

type TimeReportPeriod =
    | Daily of DateTime
    | Weekly of DateTime
    | Monthly of int * int // month, year
    | Custom of DateTime * DateTime

type FinancialCalculation = {
    TotalHours: decimal
    BillableHours: decimal
    NonBillableHours: decimal
    HourlyRate: decimal option
    TotalAmount: decimal option
    Currency: string
}

/// Module for pure functional time tracking logic
module TimeTrackingLogic =
    /// Start a new time entry
    let startEntry (userId: Guid) (taskId: Guid) (description: string) (projectId: Guid option) =
        {
            Id = Guid.NewGuid()
            UserId = userId
            TaskId = taskId
            ProjectId = projectId
            Description = description
            StartedAt = DateTime.UtcNow
            EndedAt = None
            Duration = TimeSpan.Zero
            Status = Active
            BillingStatus = Pending
            Tags = []
            CreatedAt = DateTime.UtcNow
            UpdatedAt = None
        }
    
    /// Stop an active time entry
    let stopEntry (entry: TimeEntryDomain) =
        let endTime = DateTime.UtcNow
        let duration = endTime - entry.StartedAt
        { entry with
            EndedAt = Some endTime
            Duration = duration
            Status = Completed
            UpdatedAt = Some DateTime.UtcNow }
    
    /// Pause an active time entry (creates completed entry and new paused state)
    let pauseEntry (entry: TimeEntryDomain) =
        let now = DateTime.UtcNow
        let elapsed = now - entry.StartedAt
        { entry with
            Duration = elapsed
            Status = Paused
            UpdatedAt = Some now }
    
    /// Resume a paused entry
    let resumeEntry (entry: TimeEntryDomain) =
        { entry with
            StartedAt = DateTime.UtcNow
            Status = Active
            UpdatedAt = Some DateTime.UtcNow }
    
    /// Mark entry as billable
    let markBillable (hourlyRate: decimal) (entry: TimeEntryDomain) =
        { entry with
            Status = Billable
            UpdatedAt = Some DateTime.UtcNow }
    
    /// Mark entry as non-billable
    let markNonBillable (entry: TimeEntryDomain) =
        { entry with
            Status = NonBillable
            UpdatedAt = Some DateTime.UtcNow }
    
    /// Approve entry for billing
    let approveEntry (approverId: string) (entry: TimeEntryDomain) =
        { entry with
            BillingStatus = Approved approverId
            UpdatedAt = Some DateTime.UtcNow }
    
    /// Calculate financial summary for a list of entries
    let calculateFinancials (hourlyRate: decimal option) (entries: TimeEntryDomain list) =
        let totalHours = 
            entries 
            |> List.sumBy (fun e -> decimal e.Duration.TotalHours)
        
        let billableHours =
            entries
            |> List.filter (fun e -> e.Status = Billable)
            |> List.sumBy (fun e -> decimal e.Duration.TotalHours)
        
        let nonBillableHours = totalHours - billableHours
        
        let totalAmount =
            hourlyRate
            |> Option.map (fun rate -> billableHours * rate)
        
        {
            TotalHours = totalHours
            BillableHours = billableHours
            NonBillableHours = nonBillableHours
            HourlyRate = hourlyRate
            TotalAmount = totalAmount
            Currency = "USD"
        }
    
    /// Filter entries by period
    let filterByPeriod (period: TimeReportPeriod) (entries: TimeEntryDomain list) =
        let isInPeriod (entry: TimeEntryDomain) =
            match period with
            | Daily date -> 
                entry.StartedAt.Date = date.Date
            | Weekly date ->
                let weekStart = date.AddDays(-(float date.DayOfWeek))
                entry.StartedAt >= weekStart && entry.StartedAt < weekStart.AddDays(7.0)
            | Monthly (month, year) ->
                entry.StartedAt.Month = month && entry.StartedAt.Year = year
            | Custom (startDate, endDate) ->
                entry.StartedAt >= startDate && entry.StartedAt <= endDate
        
        entries |> List.filter isInPeriod
    
    /// Group entries by task
    let groupByTask (entries: TimeEntryDomain list) =
        entries
        |> List.groupBy (fun e -> e.TaskId)
        |> Map.ofList
    
    /// Group entries by project
    let groupByProject (entries: TimeEntryDomain list) =
        entries
        |> List.filter (fun e -> e.ProjectId.IsSome)
        |> List.groupBy (fun e -> e.ProjectId.Value)
        |> Map.ofList
    
    /// Calculate total duration for entries
    let totalDuration (entries: TimeEntryDomain list) =
        entries
        |> List.fold (fun acc e -> acc + e.Duration) TimeSpan.Zero
    
    /// Validate entry (pure function)
    let validateEntry (entry: TimeEntryDomain) =
        if String.IsNullOrWhiteSpace(entry.Description) then
            Error "Description is required"
        elif entry.StartedAt > DateTime.UtcNow then
            Error "Start time cannot be in the future"
        elif entry.EndedAt.IsSome && entry.EndedAt.Value < entry.StartedAt then
            Error "End time must be after start time"
        else
            Ok entry
    
    /// Merge overlapping entries
    let mergeOverlappingEntries (entries: TimeEntryDomain list) =
        let sorted = entries |> List.sortBy (fun e -> e.StartedAt)
        
        let rec merge (acc: TimeEntryDomain list) (remaining: TimeEntryDomain list) =
            match remaining with
            | [] -> acc |> List.rev
            | [x] -> merge (x :: acc) []
            | x :: y :: rest ->
                let xEnd = x.EndedAt |> Option.defaultValue DateTime.MaxValue
                let yStart = y.StartedAt
                
                if xEnd > yStart then
                    // Overlapping - merge into single entry
                    let merged = {
                        x with
                            EndedAt = y.EndedAt
                            Duration = (y.EndedAt |> Option.defaultValue DateTime.UtcNow) - x.StartedAt
                            Description = $"{x.Description}; {y.Description}"
                    }
                    merge (merged :: acc) rest
                else
                    merge (x :: acc) (y :: rest)
        
        merge [] sorted
    
    /// Generate daily summary
    let dailySummary (date: DateTime) (entries: TimeEntryDomain list) =
        let dayEntries = entries |> filterByPeriod (Daily date)
        let totalHours = dayEntries |> totalDuration
        let financials = calculateFinancials None dayEntries
        
        {| 
            Date = date.Date
            TotalEntries = dayEntries.Length
            TotalHours = totalHours.TotalHours
            BillableHours = financials.BillableHours
            NonBillableHours = financials.NonBillableHours
            Entries = dayEntries
        |}
