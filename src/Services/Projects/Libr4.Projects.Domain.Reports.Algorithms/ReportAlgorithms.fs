namespace Libr4.Projects.Domain.Reports.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Shared types for Report algorithms
type ReportData = {
    MetricName: string
    Value: float
    Unit: string
    Timestamp: DateTime
}

type AggregatedMetric = {
    Name: string
    Total: float
    Average: float
    Min: float
    Max: float
    Count: int
}

// Report Data Aggregation
module ReportAggregator =

    // Aggregate metrics over time period
    let aggregateMetrics (data: ReportData list) (metricName: string) : AggregatedMetric =
        let filteredData = data |> List.filter (fun d -> d.MetricName = metricName)
        
        if filteredData.IsEmpty then
            {
                Name = metricName
                Total = 0.0
                Average = 0.0
                Min = 0.0
                Max = 0.0
                Count = 0
            }
        else
            let values = filteredData |> List.map (fun d -> d.Value)
            let total = List.sum values
            let average = total / float (List.length values)
            let min = List.min values
            let max = List.max values
            
            {
                Name = metricName
                Total = total
                Average = average
                Min = min
                Max = max
                Count = List.length values
            }

    // Group data by time period (daily, weekly, monthly)
    let groupByTimePeriod (data: ReportData list) (period: string) : Map<DateTime, ReportData list> =
        let keyFunc =
            match period with
            | "daily" -> fun (d: ReportData) -> d.Timestamp.Date
            | "weekly" -> fun (d: ReportData) ->
                let dayOfWeek = int d.Timestamp.DayOfWeek
                let startOfWeek = d.Timestamp.AddDays(-float dayOfWeek).Date
                startOfWeek
            | "monthly" -> fun (d: ReportData) ->
                DateTime(d.Timestamp.Year, d.Timestamp.Month, 1)
            | _ -> fun (d: ReportData) -> d.Timestamp.Date
        
        data
        |> List.groupBy keyFunc
        |> Map.ofList

    // Aggregate metrics using AI for intelligent data analysis
    let aggregateMetricsWithAI (aiService: IAIService) (data: ReportData list) (metricName: string) (aggregationContext: string) : Async<AggregatedMetric> =
        async {
            let filteredData = data |> List.filter (fun d -> d.MetricName = metricName)
            let valuesText = filteredData |> List.map (fun d -> sprintf "%.1f" d.Value) |> String.concat "; "
            
            let prompt = sprintf "Aggregate metrics: metric '%s', values [%s], context '%s'. Return JSON: {\"total\": number, \"average\": number, \"min\": number, \"max\": number}" metricName valuesText aggregationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcTotal() = 
                if filteredData.IsEmpty then 0.0 
                else filteredData |> List.sumBy (fun d -> d.Value)
            
            let total = 
                try root.GetProperty("total").GetDouble() 
                with _ -> calcTotal()
            
            let calcAverage() = 
                if filteredData.IsEmpty then 0.0 
                else total / float filteredData.Length
            
            let average = 
                try root.GetProperty("average").GetDouble() 
                with _ -> calcAverage()
            
            let calcMin() = 
                if filteredData.IsEmpty then 0.0 
                else filteredData |> List.map (fun d -> d.Value) |> List.min
            
            let minVal = 
                try root.GetProperty("min").GetDouble() 
                with _ -> calcMin()
            
            let calcMax() = 
                if filteredData.IsEmpty then 0.0 
                else filteredData |> List.map (fun d -> d.Value) |> List.max
            
            let maxVal = 
                try root.GetProperty("max").GetDouble() 
                with _ -> calcMax()
            
            return {
                Name = metricName
                Total = total
                Average = average
                Min = minVal
                Max = maxVal
                Count = filteredData.Length
            }
        }

// Report Generator
module ReportGenerator =

    type ReportConfig = {
        ReportType: string
        ProjectId: Guid
        StartDate: DateTime
        EndDate: DateTime
        IncludeMetrics: string list
        Format: string  // "PDF", "Excel", "CSV"
    }

    type GeneratedReport = {
        ReportId: Guid
        FilePath: string
        FileSize: int64
        GeneratedAt: DateTime
        Status: string
    }

    // Generate report based on configuration
    let generateReport (config: ReportConfig) (data: ReportData list) : GeneratedReport =
        // Simplified report generation - in production would use proper PDF/Excel libraries
        let reportId = Guid.NewGuid()
        let filePath = sprintf "reports/%O_%s.%s" reportId config.ReportType (config.Format.ToLower())
        
        // Simulate file size based on data
        let fileSize = int64 (data.Length * 1024)
        
        {
            ReportId = reportId
            FilePath = filePath
            FileSize = fileSize
            GeneratedAt = DateTime.UtcNow
            Status = "Completed"
        }

    // Validate report configuration
    let validateConfig (config: ReportConfig) : bool =
        config.StartDate < config.EndDate &&
        not (config.ProjectId.Equals(Guid.Empty)) &&
        not (String.IsNullOrEmpty config.ReportType) &&
        not config.IncludeMetrics.IsEmpty

    // Generate report using AI for intelligent report optimization
    let generateReportWithAI (aiService: IAIService) (config: ReportConfig) (data: ReportData list) (reportContext: string) : Async<GeneratedReport> =
        async {
            let metricsText = config.IncludeMetrics |> String.concat "; "
            let prompt = sprintf "Generate report: type '%s', metrics [%s], format '%s', context '%s'. Return JSON: {\"fileSize\": number, \"status\": string}" config.ReportType metricsText config.Format reportContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let reportId = Guid.NewGuid()
            let filePath = sprintf "reports/%O_%s.%s" reportId config.ReportType (config.Format.ToLower())
            
            let fileSize = try root.GetProperty("fileSize").GetInt64() with _ -> int64 (data.Length * 1024)
            let status = try root.GetProperty("status").GetString() with _ -> "Completed"
            
            return {
                ReportId = reportId
                FilePath = filePath
                FileSize = fileSize
                GeneratedAt = DateTime.UtcNow
                Status = status
            }
        }

// Report Scheduler
module ReportScheduler =

    type ScheduledReport = {
        ReportId: Guid
        Name: string
        Schedule: string  // "daily", "weekly", "monthly"
        NextRunDate: DateTime
        LastRunDate: DateTime option
        IsActive: bool
    }

    // Calculate next run date based on schedule
    let calculateNextRunDate (schedule: string) (lastRunDate: DateTime option) : DateTime =
        let now = DateTime.UtcNow
        let baseDate = lastRunDate |> Option.defaultValue now
        
        match schedule with
        | "daily" -> baseDate.AddDays(1.0)
        | "weekly" -> baseDate.AddDays(7.0)
        | "monthly" -> baseDate.AddMonths(1)
        | _ -> now.AddDays(1.0)

    // Check if report is due to run
    let isDueToRun (scheduledReport: ScheduledReport) : bool =
        if not scheduledReport.IsActive then false
        else
            DateTime.UtcNow >= scheduledReport.NextRunDate

    // Update scheduled report after execution
    let updateAfterExecution (scheduledReport: ScheduledReport) : ScheduledReport =
        let nextRunDate = calculateNextRunDate scheduledReport.Schedule (Some DateTime.UtcNow)
        {
            scheduledReport with
                NextRunDate = nextRunDate
                LastRunDate = Some DateTime.UtcNow
        }

    // Calculate next run date using AI for intelligent scheduling
    let calculateNextRunDateWithAI (aiService: IAIService) (schedule: string) (lastRunDate: DateTime option) (schedulingContext: string) : Async<DateTime> =
        async {
            let now = DateTime.UtcNow
            let baseDate = lastRunDate |> Option.defaultValue now
            let lastRunText = match lastRunDate with | None -> "none" | Some d -> d.ToString("o")
            
            let prompt = sprintf "Calculate next run: schedule '%s', last run %s, context '%s'. Return JSON: {\"daysToAdd\": number}" schedule lastRunText schedulingContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcDaysToAdd() = 
                match schedule with
                | "daily" -> 1.0
                | "weekly" -> 7.0
                | "monthly" -> 30.0
                | _ -> 1.0
            
            let daysToAdd = try root.GetProperty("daysToAdd").GetDouble() with _ -> calcDaysToAdd()
            
            return baseDate.AddDays(daysToAdd)
        }

// Report Performance Analyzer
module PerformanceAnalyzer =

    type PerformanceMetrics = {
        ReportId: Guid
        GenerationTime: float  // in seconds
        DataSize: int
        FileSize: int64
        SuccessRate: float
    }

    // Analyze report generation performance
    let analyzePerformance (metrics: PerformanceMetrics list) : Map<string, float> =
        if metrics.IsEmpty then Map.empty
        else
            let avgGenerationTime = 
                metrics |> List.averageBy (fun m -> m.GenerationTime)
            
            let avgDataSize = 
                metrics |> List.averageBy (fun m -> float m.DataSize)
            
            let avgFileSize = 
                metrics |> List.averageBy (fun m -> float m.FileSize)
            
            let avgSuccessRate = 
                metrics |> List.averageBy (fun m -> m.SuccessRate)
            
            [
                ("AverageGenerationTime", avgGenerationTime)
                ("AverageDataSize", avgDataSize)
                ("AverageFileSize", avgFileSize)
                ("AverageSuccessRate", avgSuccessRate)
            ] |> Map.ofList

    // Identify performance issues
    let identifyIssues (metrics: PerformanceMetrics list) : string list =
        let issues = ResizeArray<string>()
        
        let slowReports = 
            metrics 
            |> List.filter (fun m -> m.GenerationTime > 30.0)
        
        if not slowReports.IsEmpty then
            issues.Add(sprintf "%d reports took longer than 30 seconds to generate" slowReports.Length)
        
        let lowSuccessRate = 
            metrics
            |> List.filter (fun m -> m.SuccessRate < 0.9)
        
        if not lowSuccessRate.IsEmpty then
            issues.Add(sprintf "%d reports had success rate below 90%%" lowSuccessRate.Length)
        
        let largeFiles = 
            metrics
            |> List.filter (fun m -> m.FileSize > 10_000_000L)  // > 10MB
        
        if not largeFiles.IsEmpty then
            issues.Add(sprintf "%d reports generated files larger than 10MB" largeFiles.Length)
        
        List.ofSeq issues

    // Identify performance issues using AI for intelligent performance monitoring
    let identifyIssuesWithAI (aiService: IAIService) (metrics: PerformanceMetrics list) (performanceContext: string) : Async<string list> =
        async {
            let metricsText = metrics |> List.map (fun m -> sprintf "gen %.1fs, data %d, file %d, success %.1f%%" m.GenerationTime m.DataSize m.FileSize m.SuccessRate) |> String.concat "; "
            
            let prompt = sprintf "Identify performance issues: metrics [%s], context '%s'. Return JSON: {\"issues\": [string]}" metricsText performanceContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcFallbackIssues() = 
                let fallbackIssues = ResizeArray<string>()
                let slowReports = metrics |> List.filter (fun m -> m.GenerationTime > 30.0)
                if not slowReports.IsEmpty then fallbackIssues.Add(sprintf "%d reports took longer than 30 seconds" slowReports.Length)
                let lowSuccessRate = metrics |> List.filter (fun m -> m.SuccessRate < 0.9)
                if not lowSuccessRate.IsEmpty then fallbackIssues.Add(sprintf "%d reports had success rate below 90%%" lowSuccessRate.Length)
                List.ofSeq fallbackIssues
            
            let issues = try root.GetProperty("issues").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> calcFallbackIssues()
            
            return issues
        }
