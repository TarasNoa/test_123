using System.Security.Claims;
using Libr4.Auth.Domain.Kyc;
using Libr4.Shared.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Auth.Api.Endpoints;

public static class RegistrationVerificationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/verification")
            .WithTags("Registration Verification")
            .RequireAuthorization();

        // Get current verification status
        group.MapGet("/status", async (
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            var verification = db.KycVerifications
                .Where(v => v.UserId == user.Id)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault();

            if (verification == null)
            {
                return Results.Ok(new VerificationStatusDto(
                    user.Id,
                    "NotStarted",
                    false,
                    null,
                    null,
                    null));
            }

            var documents = db.KycDocuments
                .Where(d => d.VerificationId == verification.Id)
                .Select(d => new VerificationDocumentDto(
                    d.Id,
                    d.Type.ToString(),
                    d.FileUrl,
                    d.VerificationResult?.ToString(),
                    d.UploadedAt))
                .ToList();

            return Results.Ok(new VerificationStatusDto(
                user.Id,
                verification.Status.ToString(),
                verification.Status == KycStatus.Approved,
                verification.RejectionReason,
                documents,
                verification.UpdatedAt));
        });

        // Upload CV for verification
        group.MapPost("/cv", async (
            IFormFile file,
            [FromForm] string? linkedInUrl,
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".doc" && ext != ".docx")
                return Results.BadRequest(new { error = "Only PDF, DOC, DOCX allowed" });

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { error = "File size exceeds 10MB" });

            var uploadsDir = "/app/uploads/cv";
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            // Update user CV URL
            var u = await db.Users.FindAsync(user.Id);
            if (u != null)
            {
                db.Users.Entry(u).Property("CvUrl").CurrentValue = $"/uploads/cv/{fileName}";
                db.Users.Entry(u).Property("LinkedInUrl").CurrentValue = linkedInUrl;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { 
                cvUrl = $"/uploads/cv/{fileName}",
                linkedInUrl,
                message = "CV uploaded successfully. Verification pending."
            });
        }).DisableAntiforgery();

        // Upload identity documents
        group.MapPost("/documents", async (
            [FromForm] DocumentUploadRequest request,
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            var uploadedDocs = new List<object>();

            // Get or create verification
            var verification = db.KycVerifications
                .Where(v => v.UserId == user.Id && v.Status != KycStatus.Approved)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault();

            if (verification == null)
            {
                verification = KycVerification.Initiate(user.Id, KycLevel.Standard, "Internal", DateTimeOffset.UtcNow);
                db.KycVerifications.Add(verification);
                await db.SaveChangesAsync(ct);
            }

            // Process passport
            if (request.Passport != null)
            {
                var doc = await SaveDocumentAsync(request.Passport, user.Id, "passport");
                verification.AddDocument(KycDocumentType.Passport, doc.url, null, DateTimeOffset.UtcNow);
                uploadedDocs.Add(new { type = "Passport", url = doc.url });
            }

            // Process selfie with passport
            if (request.SelfieWithPassport != null)
            {
                var doc = await SaveDocumentAsync(request.SelfieWithPassport, user.Id, "selfie");
                verification.AddDocument(KycDocumentType.Selfie, doc.url, null, DateTimeOffset.UtcNow);
                uploadedDocs.Add(new { type = "Selfie", url = doc.url });
            }

            // Process additional document if provided
            if (request.AdditionalDocument != null)
            {
                var doc = await SaveDocumentAsync(request.AdditionalDocument, user.Id, "additional");
                verification.AddDocument(KycDocumentType.Other, doc.url, null, DateTimeOffset.UtcNow);
                uploadedDocs.Add(new { type = "Additional", url = doc.url });
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { 
                message = "Documents uploaded successfully",
                documents = uploadedDocs,
                verificationId = verification.Id
            });
        }).DisableAntiforgery();

        // Trigger AI verification
        group.MapPost("/verify", async (
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            var verification = db.KycVerifications
                .Where(v => v.UserId == user.Id)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault();

            if (verification == null)
                return Results.BadRequest(new { error = "No verification found. Please upload documents first." });

            if (!verification.Documents.Any())
                return Results.BadRequest(new { error = "No documents uploaded" });

            // Mark as under review
            verification.SubmitPersonalData(
                user.DisplayName ?? "",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                "",
                "",
                "",
                null,
                "",
                "",
                DateTimeOffset.UtcNow);

            await db.SaveChangesAsync(ct);

            // In real implementation, this would queue a background job for AI verification
            // For now, return that verification is in progress
            return Results.Accepted($"/api/v1/verification/status", new { 
                message = "Verification in progress",
                verificationId = verification.Id,
                estimatedCompletionMinutes = 2
            });
        });

        // Admin: Complete verification with AI result (includes CV skills)
        group.MapPost("/{verificationId:guid}/complete", async (
            Guid verificationId,
            [FromBody] CompleteVerificationWithSkillsRequest request,
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            var verification = await db.KycVerifications.FindAsync(verificationId);
            if (verification == null || verification.UserId != user.Id)
                return Results.NotFound();

            // Save skills from CV analysis if provided
            if (request.Skills?.Count > 0)
            {
                // Clear old skills
                var oldSkills = db.UserSkills.Where(s => s.UserId == user.Id).ToList();
                foreach (var old in oldSkills) db.UserSkills.Remove(old);

                // Save new skills
                foreach (var skill in request.Skills)
                {
                    db.UserSkills.Add(new Domain.Skills.UserSkill
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Name = skill.Name,
                        Score = skill.Score,  // 0-100
                        Level = skill.Level,
                        Source = skill.Source,
                        ExperienceYears = skill.ExperienceYears,
                        Contexts = skill.Contexts,
                        AssessmentReason = skill.AssessmentReason,
                        AssessedAt = DateTimeOffset.UtcNow
                    });
                }

                // Save assessment summary
                var oldAssessment = db.SkillAssessments.FirstOrDefault(a => a.UserId == user.Id);
                if (oldAssessment != null) db.SkillAssessments.Remove(oldAssessment);

                db.SkillAssessments.Add(new Domain.Skills.SkillAssessment
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    OverallLevel = request.OverallLevel ?? "Not assessed",
                    OverallScore = request.OverallScore ?? 0,
                    PrimaryExpertise = request.PrimaryExpertise ?? "Unknown",
                    SecondaryExpertise = request.SecondaryExpertise ?? new List<string>(),
                    Recommendations = request.Recommendations ?? new List<string>(),
                    AssessedAt = DateTimeOffset.UtcNow
                });
            }

            if (request.IsApproved)
            {
                verification.Approve(RiskRating.Low, false, DateTimeOffset.UtcNow);
            }
            else
            {
                verification.Reject(request.Reason ?? "Verification failed", DateTimeOffset.UtcNow);
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { 
                status = verification.Status.ToString(),
                isApproved = verification.Status == KycStatus.Approved,
                skillsSaved = request.Skills?.Count ?? 0
            });
        });

        return app;
    }

    private static async Task<(string url, string path)> SaveDocumentAsync(IFormFile file, Guid userId, string prefix)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".pdf")
            throw new InvalidOperationException("Invalid file type");

        var uploadsDir = $"/app/uploads/verification/{userId}";
        Directory.CreateDirectory(uploadsDir);
        
        var fileName = $"{prefix}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        return ($"/uploads/verification/{userId}/{fileName}", filePath);
    }
}

// DTOs
public sealed record DocumentUploadRequest(
    IFormFile? Passport,
    IFormFile? SelfieWithPassport,
    IFormFile? AdditionalDocument);

public sealed record VerificationStatusDto(
    Guid UserId,
    string Status,
    bool IsVerified,
    string? RejectionReason,
    List<VerificationDocumentDto>? Documents,
    DateTimeOffset? LastUpdated);

public sealed record VerificationDocumentDto(
    Guid Id,
    string Type,
    string Url,
    string? VerificationResult,
    DateTimeOffset UploadedAt);

public sealed record CompleteVerificationRequest(bool IsApproved, string? Reason);

public sealed record SkillInputDto(
    string Name,
    float Score,
    string Level,
    string Source,
    int ExperienceYears,
    List<string> Contexts,
    string AssessmentReason);

public sealed record CompleteVerificationWithSkillsRequest(
    bool IsApproved,
    string? Reason,
    List<SkillInputDto>? Skills,
    string? OverallLevel,
    float? OverallScore,
    string? PrimaryExpertise,
    List<string>? SecondaryExpertise,
    List<string>? Recommendations);
