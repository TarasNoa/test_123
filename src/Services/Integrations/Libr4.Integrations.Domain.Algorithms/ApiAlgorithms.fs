namespace Libr4.Integrations.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Rate Limiting Algorithms
module RateLimiter =

    type RateLimitState = {
        Requests: int
        WindowStart: DateTime
    }

    // Check if request is allowed based on rate limit
    let checkRateLimit (state: RateLimitState) (limit: int) (windowSeconds: int) (now: DateTime) : (bool * RateLimitState) =
        let windowEnd = state.WindowStart.AddSeconds(float windowSeconds)
        
        if now > windowEnd then
            // Window expired, reset
            let newState = { Requests = 1; WindowStart = now }
            (true, newState)
        elif state.Requests < limit then
            // Within limit
            let newState = { state with Requests = state.Requests + 1 }
            (true, newState)
        else
            // Rate limit exceeded
            (false, state)

    // Calculate time until rate limit reset
    let calculateResetTime (state: RateLimitState) (windowSeconds: int) (now: DateTime) : TimeSpan =
        let windowEnd = state.WindowStart.AddSeconds(float windowSeconds)
        if windowEnd <= now then TimeSpan.Zero
        else windowEnd - now

    // Check rate limit using AI for intelligent rate limiting
    let checkRateLimitWithAI (aiService: IAIService) (state: RateLimitState) (limit: int) (windowSeconds: int) (now: DateTime) (rateContext: string) : Async<bool * RateLimitState> =
        async {
            let prompt = sprintf "Check rate limit: requests %d, window start %s, limit %d, window %d sec, context '%s'. Return JSON: {\"allowed\": bool, \"resetWindow\": bool}" state.Requests (state.WindowStart.ToString("o")) limit windowSeconds rateContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "integrations") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let windowEnd = state.WindowStart.AddSeconds(float windowSeconds)
            
            let calcAllowed() = 
                if now > windowEnd then true
                elif state.Requests < limit then true
                else false
            let allowed = try root.GetProperty("allowed").GetBoolean() with _ -> calcAllowed()
            
            let resetWindow = try root.GetProperty("resetWindow").GetBoolean() with _ -> now > windowEnd
            
            let newState = 
                if resetWindow then { Requests = 1; WindowStart = now }
                elif allowed then { state with Requests = state.Requests + 1 }
                else state
            
            return (allowed, newState)
        }

// Retry Logic Algorithms
module RetryHandler =

    type RetryPolicy = {
        MaxRetries: int
        InitialDelayMs: int
        MaxDelayMs: int
        BackoffMultiplier: float
    }

    type RetryResult = {
        ShouldRetry: bool
        DelayMs: int
        AttemptNumber: int
    }

    // Default retry policy
    let defaultRetryPolicy = {
        MaxRetries = 3
        InitialDelayMs = 1000
        MaxDelayMs = 30000
        BackoffMultiplier = 2.0
    }

    // Calculate next retry delay with exponential backoff
    let calculateRetryDelay (policy: RetryPolicy) (attemptNumber: int) (isTransient: bool) : RetryResult =
        if attemptNumber >= policy.MaxRetries || not isTransient then
            { ShouldRetry = false; DelayMs = 0; AttemptNumber = attemptNumber }
        else
            let delay = 
                let exponentialDelay = float policy.InitialDelayMs * (policy.BackoffMultiplier ** float attemptNumber)
                min policy.MaxDelayMs (int exponentialDelay)
            { ShouldRetry = true; DelayMs = delay; AttemptNumber = attemptNumber + 1 }

    // Determine if error is transient (retryable)
    let isTransientError (statusCode: int) (errorMessage: string) : bool =
        match statusCode with
        | 408 -> true // Request Timeout
        | 429 -> true // Too Many Requests
        | 500 -> true // Internal Server Error
        | 502 -> true // Bad Gateway
        | 503 -> true // Service Unavailable
        | 504 -> true // Gateway Timeout
        | _ ->
            // Check for specific error messages
            errorMessage.ToLowerInvariant().Contains("timeout") ||
            errorMessage.ToLowerInvariant().Contains("temporary") ||
            errorMessage.ToLowerInvariant().Contains("retry")

    // Calculate retry delay using AI for intelligent retry logic
    let calculateRetryDelayWithAI (aiService: IAIService) (policy: RetryPolicy) (attemptNumber: int) (isTransient: bool) (statusCode: int) (retryContext: string) : Async<RetryResult> =
        async {
            let prompt = sprintf "Calculate retry: attempt %d, transient %b, status %d, max retries %d, context '%s'. Return JSON: {\"shouldRetry\": bool, \"delayMs\": number}" attemptNumber isTransient statusCode policy.MaxRetries retryContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "integrations") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcShouldRetry() = attemptNumber < policy.MaxRetries && isTransient
            let shouldRetry = try root.GetProperty("shouldRetry").GetBoolean() with _ -> calcShouldRetry()
            
            let calcDelayMs() = 
                if shouldRetry then
                    let exponentialDelay = float policy.InitialDelayMs * (policy.BackoffMultiplier ** float attemptNumber)
                    min policy.MaxDelayMs (int exponentialDelay)
                else 0
            let delayMs = try root.GetProperty("delayMs").GetInt32() with _ -> calcDelayMs()
            
            return {
                ShouldRetry = shouldRetry
                DelayMs = delayMs
                AttemptNumber = attemptNumber + 1
            }
        }

// Data Synchronization Algorithms
module DataSync =

    type SyncStatus = Synced | Pending | Conflict | Error

    type SyncItem = {
        Id: Guid
        EntityType: string
        ExternalId: string
        LastSyncedAt: DateTime option
        Status: SyncStatus
        ConflictCount: int
    }

    // Determine if sync is needed based on last sync time
    let needsSync (item: SyncItem) (syncIntervalHours: float) (now: DateTime) : bool =
        match item.LastSyncedAt with
        | None -> true
        | Some lastSync ->
            let hoursSinceSync = (now - lastSync).TotalHours
            hoursSinceSync >= syncIntervalHours

    // Calculate sync priority based on age and conflict count
    let calculateSyncPriority (item: SyncItem) (now: DateTime) : int =
        let ageHours = 
            match item.LastSyncedAt with
            | None -> 999.0
            | Some lastSync -> (now - lastSync).TotalHours
        
        let ageScore = int (ageHours / 24.0) // Days since last sync
        let conflictScore = item.ConflictCount * 10
        ageScore + conflictScore

    // Detect conflicts between local and external data
    let detectConflict (localVersion: int) (externalVersion: int) (localData: string) (externalData: string) : bool =
        localVersion <> externalVersion && localData <> externalData

    // Calculate sync priority using AI for intelligent sync scheduling
    let calculateSyncPriorityWithAI (aiService: IAIService) (item: SyncItem) (now: DateTime) (syncContext: string) : Async<int> =
        async {
            let ageHours = match item.LastSyncedAt with | None -> 999.0 | Some lastSync -> (now - lastSync).TotalHours
            
            let prompt = sprintf "Calculate sync priority: entity '%s', age %.1f hours, conflicts %d, context '%s'. Return JSON: {\"priority\": number (1-100)}" item.EntityType ageHours item.ConflictCount syncContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "integrations") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let ageScore = int (ageHours / 24.0)
            let conflictScore = item.ConflictCount * 10
            let calcPriority() = ageScore + conflictScore
            let priority = try root.GetProperty("priority").GetInt32() with _ -> calcPriority()
            
            return priority
        }

// API Caching Algorithms
module ApiCache =

    type CacheEntry = {
        Key: string
        Value: string
        CachedAt: DateTime
        ExpiresAt: DateTime
        HitCount: int
    }

    // Check if cache entry is valid
    let isCacheValid (entry: CacheEntry) (now: DateTime) : bool =
        now < entry.ExpiresAt

    // Calculate cache hit rate
    let calculateHitRate (hits: int) (totalRequests: int) : float =
        if totalRequests = 0 then 0.0
        else float hits / float totalRequests * 100.0

    // Determine optimal cache TTL based on data volatility
    let calculateOptimalTTL (updateFrequencyMinutes: int) (dataImportance: int) : TimeSpan =
        // Higher importance = shorter TTL
        // Higher update frequency = shorter TTL
        let baseTTL = updateFrequencyMinutes * 2
        let adjustedTTL = max 1 (baseTTL / dataImportance)
        TimeSpan.FromMinutes(float adjustedTTL)

    // Calculate optimal TTL using AI for intelligent cache management
    let calculateOptimalTTLWithAI (aiService: IAIService) (updateFrequencyMinutes: int) (dataImportance: int) (cacheContext: string) : Async<TimeSpan> =
        async {
            let prompt = sprintf "Calculate cache TTL: update freq %d min, importance %d, context '%s'. Return JSON: {\"ttlMinutes\": number}" updateFrequencyMinutes dataImportance cacheContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "integrations") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let baseTTL = updateFrequencyMinutes * 2
            let calcTtlMinutes() = max 1 (baseTTL / dataImportance)
            let ttlMinutes = try root.GetProperty("ttlMinutes").GetInt32() with _ -> calcTtlMinutes()
            
            return TimeSpan.FromMinutes(float ttlMinutes)
        }

// API Health Monitoring Algorithms
module HealthMonitor =

    type HealthStatus = Healthy | Degraded | Unhealthy

    type HealthMetrics = {
        SuccessRate: float
        AverageResponseTimeMs: float
        ErrorRate: float
        UptimePercentage: float
    }

    // Determine overall health status
    let determineHealthStatus (metrics: HealthMetrics) : HealthStatus =
        match metrics.SuccessRate, metrics.AverageResponseTimeMs, metrics.ErrorRate with
        | success, responseTime, errorRate when success >= 95.0 && responseTime < 500.0 && errorRate < 1.0 ->
            Healthy
        | success, responseTime, errorRate when success >= 80.0 && responseTime < 2000.0 && errorRate < 5.0 ->
            Degraded
        | _ ->
            Unhealthy

    // Determine health status using AI for intelligent health monitoring
    let determineHealthStatusWithAI (aiService: IAIService) (metrics: HealthMetrics) (healthContext: string) : Async<HealthStatus> =
        async {
            let prompt = sprintf "Determine health: success %.1f%%, response %.1f ms, error %.1f%%, uptime %.1f%%, context '%s'. Return JSON: {\"status\": \"Healthy/Degraded/Unhealthy\"}" metrics.SuccessRate metrics.AverageResponseTimeMs metrics.ErrorRate metrics.UptimePercentage healthContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "integrations") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcStatusStr() = 
                match metrics.SuccessRate, metrics.AverageResponseTimeMs, metrics.ErrorRate with
                | success, responseTime, errorRate when success >= 95.0 && responseTime < 500.0 && errorRate < 1.0 -> "Healthy"
                | success, responseTime, errorRate when success >= 80.0 && responseTime < 2000.0 && errorRate < 5.0 -> "Degraded"
                | _ -> "Unhealthy"
            let statusStr = try root.GetProperty("status").GetString() with _ -> calcStatusStr()
            
            let status = 
                match statusStr with
                | "Degraded" -> Degraded
                | "Unhealthy" -> Unhealthy
                | _ -> Healthy
            
            return status
        }

    // Calculate rolling average response time
    let calculateRollingAverage (recentResponseTimes: float list) (windowSize: int) : float =
        match recentResponseTimes with
        | [] -> 0.0
        | times ->
            let windowed = 
                if List.length times <= windowSize then times
                else times |> List.skip (List.length times - windowSize)
            List.average windowed

    // Detect anomalies in response time
    let detectResponseTimeAnomaly (currentResponseTime: float) (historicalAverage: float) (thresholdMultiplier: float) : bool =
        currentResponseTime > historicalAverage * thresholdMultiplier
