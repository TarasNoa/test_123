using Libr4.AI.Infrastructure.AI.Providers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.AI.Application.DocumentVerification;

public interface IDocumentVerificationService
{
    Task<IdentityVerificationResult> VerifyIdentityDocumentsAsync(
        IdentityVerificationRequest request,
        CancellationToken ct = default);
    
    Task<CVVerificationResult> VerifyCVAsync(
        CVVerificationRequest request,
        CancellationToken ct = default);
}

public sealed class DocumentVerificationService : IDocumentVerificationService
{
    private readonly DockerModelRunnerProvider _aiProvider;
    private readonly ILogger<DocumentVerificationService> _logger;

    public DocumentVerificationService(DockerModelRunnerProvider aiProvider, ILogger<DocumentVerificationService> logger)
    {
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<IdentityVerificationResult> VerifyIdentityDocumentsAsync(
        IdentityVerificationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting identity verification for user {UserId}", request.UserId);
        
        var checks = new List<VerificationCheck>();
        var analysisResults = new List<DocumentAnalysisResult>();
        
        // Analyze passport document
        if (!string.IsNullOrEmpty(request.PassportImageUrl))
        {
            var passportAnalysis = await AnalyzePassportAsync(request.PassportImageUrl, ct);
            analysisResults.Add(passportAnalysis);
            checks.Add(new VerificationCheck(
                "Passport Authenticity",
                passportAnalysis.IsAuthentic ? CheckResult.Pass : CheckResult.Fail,
                passportAnalysis.Details));
        }
        
        // Analyze selfie with passport
        if (!string.IsNullOrEmpty(request.SelfieWithPassportUrl) && !string.IsNullOrEmpty(request.PassportImageUrl))
        {
            var faceMatchResult = await VerifyFaceMatchAsync(
                request.SelfieWithPassportUrl, 
                request.PassportImageUrl, 
                ct);
            checks.Add(new VerificationCheck(
                "Face Match",
                faceMatchResult.IsMatch ? CheckResult.Pass : CheckResult.Fail,
                faceMatchResult.Details));
        }
        
        // Liveness check on selfie
        if (!string.IsNullOrEmpty(request.SelfieWithPassportUrl))
        {
            var livenessResult = await VerifyLivenessAsync(request.SelfieWithPassportUrl, ct);
            checks.Add(new VerificationCheck(
                "Liveness Detection",
                livenessResult.IsLive ? CheckResult.Pass : CheckResult.Fail,
                livenessResult.Details));
        }
        
        // Cross-reference with provided data
        if (!string.IsNullOrEmpty(request.ProvidedFullName) && analysisResults.Any())
        {
            var nameMatch = analysisResults
                .Where(r => !string.IsNullOrEmpty(r.ExtractedName))
                .Any(r => NormalizeName(r.ExtractedName).Contains(NormalizeName(request.ProvidedFullName)) ||
                         NormalizeName(request.ProvidedFullName).Contains(NormalizeName(r.ExtractedName)));
            
            checks.Add(new VerificationCheck(
                "Name Match",
                nameMatch ? CheckResult.Pass : CheckResult.Warn,
                nameMatch ? "Name matches document" : "Name mismatch detected"));
        }
        
        var overallResult = DetermineOverallResult(checks);
        
        return new IdentityVerificationResult(
            request.UserId,
            overallResult,
            checks,
            analysisResults.Select(r => r.ExtractedName).FirstOrDefault(),
            DateTimeOffset.UtcNow);
    }

    public async Task<CVVerificationResult> VerifyCVAsync(
        CVVerificationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting CV verification for user {UserId}", request.UserId);
        
        var checks = new List<VerificationCheck>();
        
        // LinkedIn profile verification
        if (!string.IsNullOrEmpty(request.LinkedInUrl))
        {
            var linkedInCheck = await VerifyLinkedInProfileAsync(request.LinkedInUrl, ct);
            checks.Add(new VerificationCheck(
                "LinkedIn Profile",
                linkedInCheck.IsValid ? CheckResult.Pass : CheckResult.Warn,
                linkedInCheck.Details));
        }
        
        // CV content analysis
        if (!string.IsNullOrEmpty(request.CVText))
        {
            var cvAnalysis = await AnalyzeCVContentAsync(request.CVText, ct);
            checks.Add(new VerificationCheck(
                "CV Authenticity",
                cvAnalysis.IsAuthentic ? CheckResult.Pass : CheckResult.Warn,
                cvAnalysis.Details));
            
            checks.Add(new VerificationCheck(
                "Content Quality",
                cvAnalysis.QualityScore > 0.6 ? CheckResult.Pass : CheckResult.Warn,
                $"Quality score: {cvAnalysis.QualityScore:P0}"));
        }
        
        var overallResult = DetermineOverallResult(checks);
        
        return new CVVerificationResult(
            request.UserId,
            overallResult,
            checks,
            DateTimeOffset.UtcNow);
    }

    private async Task<DocumentAnalysisResult> AnalyzePassportAsync(string imageUrl, CancellationToken ct)
    {
        // In real implementation, this would use computer vision API
        // For now, simulating with LLM-based analysis description
        var prompt = $"""
            Analyze this passport image: {imageUrl}
            Determine:
            1. Is it a valid, authentic passport document?
            2. What is the full name shown?
            3. Is the photo clear and matches standards?
            4. Are security features visible?
            
            Respond in JSON format:
            {{
                "isAuthentic": true/false,
                "extractedName": "Full Name",
                "confidence": 0.95,
                "details": "Analysis details"
            }}
            """;
        
        try
        {
            var systemPrompt = "You are a document verification expert. Analyze the passport image and respond in JSON format.";
            var response = await _aiProvider.GenerateCompletionAsync(prompt, systemPrompt, null);
            var result = JsonSerializer.Deserialize<PassportAnalysisResponse>(
                ExtractJson(response),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return new DocumentAnalysisResult(
                "Passport",
                result?.IsAuthentic ?? false,
                result?.ExtractedName,
                result?.Details ?? "Analysis completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passport analysis failed");
            return new DocumentAnalysisResult("Passport", false, null, "Analysis failed");
        }
    }

    private async Task<FaceMatchResult> VerifyFaceMatchAsync(string selfieUrl, string passportUrl, CancellationToken ct)
    {
        var prompt = $"""
            Compare faces in these two images:
            Selfie: {selfieUrl}
            Passport: {passportUrl}
            
            Determine if they are the same person.
            Respond in JSON: {{"isMatch": true/false, "confidence": 0.95, "details": "reasoning"}}
            """;
        
        try
        {
            var systemPrompt = "You are a facial recognition expert. Compare faces and respond in JSON format.";
            var response = await _aiProvider.GenerateCompletionAsync(prompt, systemPrompt, null);
            var result = JsonSerializer.Deserialize<FaceMatchResult>(
                ExtractJson(response),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new FaceMatchResult(false, 0, "Analysis failed");
        }
        catch
        {
            return new FaceMatchResult(false, 0, "Face match verification failed");
        }
    }

    private async Task<LivenessResult> VerifyLivenessAsync(string selfieUrl, CancellationToken ct)
    {
        var prompt = $"""
            Analyze this selfie image: {selfieUrl}
            Determine if this is a real, live person (not a photo of a photo, mask, or screen).
            Look for: natural lighting, depth, texture, eye reflection.
            Respond in JSON: {{"isLive": true/false, "confidence": 0.95, "details": "reasoning"}}
            """;
        
        try
        {
            var systemPrompt = "You are a liveness detection expert. Analyze the selfie and respond in JSON format.";
            var response = await _aiProvider.GenerateCompletionAsync(prompt, systemPrompt, null);
            var result = JsonSerializer.Deserialize<LivenessResult>(
                ExtractJson(response),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new LivenessResult(false, 0, "Liveness check failed");
        }
        catch
        {
            return new LivenessResult(false, 0, "Liveness verification failed");
        }
    }

    private async Task<LinkedInCheckResult> VerifyLinkedInProfileAsync(string linkedInUrl, CancellationToken ct)
    {
        // Simulated LinkedIn verification
        await Task.Delay(500, ct);
        
        var isValid = linkedInUrl.Contains("linkedin.com/in/");
        return new LinkedInCheckResult(
            isValid,
            isValid ? "LinkedIn profile URL is valid format" : "Invalid LinkedIn URL format");
    }

    private async Task<CVContentAnalysis> AnalyzeCVContentAsync(string cvText, CancellationToken ct)
    {
        var prompt = $"""
            Analyze this CV content for authenticity and quality:
            {cvText.Substring(0, Math.Min(cvText.Length, 2000))}
            
            Check for:
            1. Generic templates (signs of copy-paste)
            2. Unrealistic claims
            3. Consistency and coherence
            4. Professional formatting
            
            Respond in JSON:
            {{
                "isAuthentic": true/false,
                "qualityScore": 0.85,
                "details": "analysis summary"
            }}
            """;
        
        try
        {
            var systemPrompt = "You are a CV analysis expert. Analyze the CV content and respond in JSON format.";
            var response = await _aiProvider.GenerateCompletionAsync(prompt, systemPrompt, null);
            var result = JsonSerializer.Deserialize<CVContentAnalysis>(
                ExtractJson(response),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new CVContentAnalysis(true, 0.5, "Analysis inconclusive");
        }
        catch
        {
            return new CVContentAnalysis(true, 0.5, "Could not analyze CV content");
        }
    }

    private static VerificationOutcome DetermineOverallResult(List<VerificationCheck> checks)
    {
        if (checks.Any(c => c.Result == CheckResult.Fail))
            return VerificationOutcome.Rejected;
        if (checks.Any(c => c.Result == CheckResult.Warn))
            return VerificationOutcome.ManualReview;
        if (checks.All(c => c.Result == CheckResult.Pass))
            return VerificationOutcome.Approved;
        return VerificationOutcome.Pending;
    }

    private static string NormalizeName(string? name) => 
        (name ?? "").ToLower().Replace(" ", "").Replace("-", "");

    private static string ExtractJson(string response)
    {
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response.Substring(start, end - start + 1);
        return response;
    }
}

// Request/Result DTOs
public sealed record IdentityVerificationRequest(
    Guid UserId,
    string? PassportImageUrl,
    string? SelfieWithPassportUrl,
    string? AdditionalDocumentUrl,
    string? ProvidedFullName,
    DateOnly? ProvidedDateOfBirth);

public sealed record CVVerificationRequest(
    Guid UserId,
    string? CVText,
    string? LinkedInUrl);

public sealed record IdentityVerificationResult(
    Guid UserId,
    VerificationOutcome Outcome,
    List<VerificationCheck> Checks,
    string? ExtractedName,
    DateTimeOffset CompletedAt);

public sealed record CVVerificationResult(
    Guid UserId,
    VerificationOutcome Outcome,
    List<VerificationCheck> Checks,
    DateTimeOffset CompletedAt);

public sealed record VerificationCheck(string Name, CheckResult Result, string Details);

public enum VerificationOutcome { Pending, Approved, Rejected, ManualReview }
public enum CheckResult { Pass, Warn, Fail }

// Internal types
internal sealed record DocumentAnalysisResult(string DocumentType, bool IsAuthentic, string? ExtractedName, string Details);
internal sealed record FaceMatchResult(bool IsMatch, double Confidence, string Details);
internal sealed record LivenessResult(bool IsLive, double Confidence, string Details);
internal sealed record LinkedInCheckResult(bool IsValid, string Details);
internal sealed record CVContentAnalysis(bool IsAuthentic, double QualityScore, string Details);
internal sealed record PassportAnalysisResponse(bool IsAuthentic, string? ExtractedName, double Confidence, string Details);
