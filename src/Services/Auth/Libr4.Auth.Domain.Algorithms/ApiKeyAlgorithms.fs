namespace Libr4.Auth.Domain.Algorithms

open System
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Libr4.Auth.Domain.ApiKeys
open Libr4.AI.Infrastructure.AI

// API Key Generator
module ApiKeyGenerator =

    type GeneratedKey = {
        PlainKey: string
        KeyHash: string
        KeyPrefix: string
    }

    // Generate secure API key
    let generateApiKey (prefix: string option) : GeneratedKey =
        let randomBytes = Array.zeroCreate<byte> 32
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes(randomBytes)
        
        let plainKey = Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "")
        let keyPrefix = prefix |> Option.defaultValue "libr4"
        let fullKey = sprintf "%s_%s" keyPrefix plainKey
        
        use sha256 = SHA256.Create()
        let hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fullKey))
        let keyHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower()
        
        {
            PlainKey = fullKey
            KeyHash = keyHash
            KeyPrefix = sprintf "%s_%s" keyPrefix (plainKey.[0..7])
        }

    // Generate API key with AI-suggested prefix
    let generateApiKeyWithAI (aiService: IAIService) (userContext: string) (apiKeyPurpose: string) : Async<GeneratedKey> =
        async {
            let prompt = sprintf "Suggest a secure API key prefix for user context '%s' and purpose '%s'. Return JSON: {\"prefix\": string (3-8 chars, lowercase, no special chars)}" userContext apiKeyPurpose
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "auth") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let suggestedPrefix = 
                try root.GetProperty("prefix").GetString()
                with _ -> "libr4"
            
            let cleanPrefix = 
                suggestedPrefix.ToLower()
                              |> Seq.filter (fun c -> System.Char.IsLetterOrDigit c)
                              |> Seq.toArray
                              |> fun arr -> if arr.Length > 8 then arr.[0..7] else arr
                              |> System.String
            
            return generateApiKey (Some cleanPrefix)
        }

// Security Analyzer
module SecurityAnalyzer =

    type SecurityRisk = {
        Level: string
        Score: float
        Factors: string list
    }

    // Analyze API key security
    let analyzeSecurity (lastUsed: DateTimeOffset option) (createdAt: DateTimeOffset) (expiresAt: DateTimeOffset option) (usageCount: int) : SecurityRisk =
        let now = DateTimeOffset.UtcNow
        let factors = ResizeArray<string>()
        let mutable score = 100.0
        
        // Check if key is expired
        match expiresAt with
        | Some expiry when expiry < now ->
            score <- score - 50.0
            factors.Add("Key is expired")
        | _ -> ()
        
        // Check last usage
        match lastUsed with
        | None when (now - createdAt).TotalDays > 30.0 ->
            score <- score - 20.0
            factors.Add("Key never used and is old")
        | Some last when (now - last).TotalDays > 90.0 ->
            score <- score - 15.0
            factors.Add("Key not used in 90 days")
        | _ -> ()
        
        // Check usage patterns
        if usageCount > 10000 then
            score <- score - 10.0
            factors.Add("High usage count - potential compromise")
        
        let riskLevel = 
            match score with
            | _ when score >= 80.0 -> "Low"
            | _ when score >= 50.0 -> "Medium"
            | _ -> "High"
        
        {
            Level = riskLevel
            Score = score
            Factors = List.ofSeq factors
        }

    // Analyze security using AI for anomaly detection
    let analyzeSecurityWithAI (aiService: IAIService) (lastUsed: DateTimeOffset option) (createdAt: DateTimeOffset) (expiresAt: DateTimeOffset option) (usageCount: int) (usagePattern: string list) : Async<SecurityRisk> =
        async {
            let now = DateTimeOffset.UtcNow
            let lastUsedText = match lastUsed with Some d -> d.ToString("o") | None -> "never"
            let createdAtText = createdAt.ToString("o")
            let expiresAtText = match expiresAt with Some d -> d.ToString("o") | None -> "never"
            let patternText = String.concat ", " usagePattern
            
            let prompt = sprintf "Analyze API key security: last used %s, created %s, expires %s, usage count %d, pattern [%s]. Return JSON: {\"riskLevel\": \"Low/Medium/High\", \"score\": number (0-100), \"factors\": [string]}" lastUsedText createdAtText expiresAtText usageCount patternText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "auth") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let riskLevel = 
                try root.GetProperty("riskLevel").GetString()
                with _ -> "Medium"
            
            let score = 
                try root.GetProperty("score").GetDouble()
                with _ -> 50.0
            
            let factors = 
                try
                    root.GetProperty("factors").EnumerateArray()
                    |> Seq.map (fun f -> f.GetString())
                    |> List.ofSeq
                with _ ->
                    analyzeSecurity lastUsed createdAt expiresAt usageCount |> fun r -> r.Factors
            
            return {
                Level = riskLevel
                Score = score
                Factors = factors
            }
        }

// Rate Limiter
module RateLimiter =

    type RateLimitCheck = {
        Allowed: bool
        RemainingRequests: int
        ResetAt: DateTimeOffset
    }

    // Check rate limit for API key
    let checkRateLimit (requestCount: int) (limit: int) (windowStart: DateTimeOffset) (windowDuration: TimeSpan) : RateLimitCheck =
        let now = DateTimeOffset.UtcNow
        let windowEnd = windowStart.Add(windowDuration)
        
        if now >= windowEnd then
            // Window reset
            {
                Allowed = true
                RemainingRequests = limit - 1
                ResetAt = now.Add(windowDuration)
            }
        else if requestCount >= limit then
            // Limit exceeded
            {
                Allowed = false
                RemainingRequests = 0
                ResetAt = windowEnd
            }
        else
            // Within limit
            {
                Allowed = true
                RemainingRequests = limit - requestCount - 1
                ResetAt = windowEnd
            }

    // Predict rate limit breach using AI
    let predictRateLimitBreach (aiService: IAIService) (currentRequestCount: int) (limit: int) (requestHistory: int list) (userBehavior: string) : Async<RateLimitCheck> =
        async {
            let historyText = String.concat ", " (requestHistory |> List.map string)
            let prompt = sprintf "Predict rate limit breach: current %d, limit %d, history [%s], behavior '%s'. Return JSON: {\"willBreach\": bool, \"probability\": number (0-1), \"suggestedLimit\": number}" currentRequestCount limit historyText userBehavior
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "auth") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let willBreach = 
                try root.GetProperty("willBreach").GetBoolean()
                with _ -> currentRequestCount >= limit
            
            let allowed = not willBreach
            let remaining = if allowed then limit - currentRequestCount - 1 else 0
            let resetAt = DateTimeOffset.UtcNow.AddHours(1.0)
            
            return {
                Allowed = allowed
                RemainingRequests = remaining
                ResetAt = resetAt
            }
        }

// Scope Validator
module ScopeValidator =

    // Validate if required scope is granted
    let hasScope (grantedScopes: ApiKeyScope) (requiredScope: ApiKeyScope) : bool =
        (grantedScopes &&& requiredScope) = requiredScope
    
    // Check if admin scope is granted
    let isAdmin (scopes: ApiKeyScope) : bool =
        (scopes &&& ApiKeyScope.Admin) = ApiKeyScope.Admin
    
    // Check if has write permissions
    let hasWritePermission (scopes: ApiKeyScope) : bool =
        (scopes &&& (ApiKeyScope.WriteProfile ||| ApiKeyScope.WriteTasks ||| ApiKeyScope.WritePayments ||| ApiKeyScope.WriteChat)) <> ApiKeyScope.None

    // Suggest appropriate scopes using AI
    let suggestScopesWithAI (aiService: IAIService) (requestedOperation: string) (userRole: string) (resourceType: string) : Async<ApiKeyScope list> =
        async {
            let prompt = sprintf "Suggest appropriate API key scopes for operation '%s', user role '%s', resource type '%s'. Available scopes: ReadProfile, WriteProfile, ReadTasks, WriteTasks, ReadPayments, WritePayments, ReadChat, WriteChat, Admin. Return JSON: {\"scopes\": [string]}" requestedOperation userRole resourceType
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "auth") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let scopeStrings = 
                try
                    root.GetProperty("scopes").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ ->
                    match userRole.ToLower() with
                    | "admin" -> ["Admin"]
                    | _ -> ["ReadProfile"; "ReadTasks"]
            
            let scopes = 
                scopeStrings
                |> List.choose (fun s ->
                    match s with
                    | "ReadProfile" -> Some ApiKeyScope.ReadProfile
                    | "WriteProfile" -> Some ApiKeyScope.WriteProfile
                    | "ReadTasks" -> Some ApiKeyScope.ReadTasks
                    | "WriteTasks" -> Some ApiKeyScope.WriteTasks
                    | "ReadPayments" -> Some ApiKeyScope.ReadPayments
                    | "WritePayments" -> Some ApiKeyScope.WritePayments
                    | "ReadChat" -> Some ApiKeyScope.ReadChat
                    | "WriteChat" -> Some ApiKeyScope.WriteChat
                    | "Admin" -> Some ApiKeyScope.Admin
                    | _ -> None)
            
            return scopes
        }
