using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Libr4.Auth.Application.Dtos;
using Libr4.Auth.Application.Users.Commands;
using Libr4.Auth.Application.Users.Queries;
using Libr4.Shared.Web.Middleware;
using Libr4.Shared.Web.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/auth").WithTags("Auth");

        grp.MapPost("/register", async (RegisterRequest body, HttpContext ctx, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new RegisterUserCommand(body));
                if (!result.IsSuccess)
                    return ResultExtensions.Problem(result.Error);

                // Auto-login after registration
                var ip = ctx.Connection.RemoteIpAddress?.ToString();
                var loginResult = await mediator.Send(new LoginCommand(new LoginRequest(body.Email, body.Password, null), ip));
                return loginResult.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).AllowAnonymous();

        grp.MapPost("/login", async (LoginRequest body, HttpContext ctx, ISender mediator) =>
        {
            try
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString();
                var result = await mediator.Send(new LoginCommand(body, ip));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).AllowAnonymous();

        grp.MapPost("/refresh", async ([FromBody] RefreshRequest body, HttpContext ctx, ISender mediator) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            var result = await mediator.Send(new RefreshTokenCommand(body.RefreshToken, ip));
            return result.ToHttpResult();
        }).AllowAnonymous();

        grp.MapPost("/logout", async ([FromBody] LogoutRequest body, HttpContext ctx, ISender mediator) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            var result = await mediator.Send(new LogoutCommand(body.RefreshToken, ip));
            return result.ToHttpResult();
        });

        grp.MapGet("/me", async (ClaimsPrincipal user, ISender mediator) =>
        {
            var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new GetCurrentUserQuery(userId));
            return result.ToHttpResult();
        }).RequireAuthorization();

        // 2FA
        grp.MapPost("/2fa/setup", async (ClaimsPrincipal user, ISender mediator) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new SetupTwoFactorCommand(userId));
            return result.ToHttpResult();
        }).RequireAuthorization();

        grp.MapPost("/2fa/verify", async (TwoFactorVerifyRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new VerifyTwoFactorCommand(userId, body.Code));
            return result.ToHttpResult();
        }).RequireAuthorization();

        grp.MapPost("/2fa/disable", async (TwoFactorDisableRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new DisableTwoFactorCommand(userId, body.Password));
            return result.ToHttpResult();
        }).RequireAuthorization();

        // Email confirmation
        grp.MapPost("/email/confirm-request", async (ClaimsPrincipal user, ISender mediator) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new RequestEmailConfirmationCommand(userId));
            return result.ToHttpResult();
        }).RequireAuthorization();

        grp.MapPost("/email/confirm", async ([FromBody] ConfirmEmailRequest body, ISender mediator) =>
        {
            var result = await mediator.Send(new ConfirmEmailCommand(body.Token));
            return result.ToHttpResult();
        }).AllowAnonymous();

        // Password reset
        grp.MapPost("/password/reset-request", async ([FromBody] PasswordResetRequest body, ISender mediator) =>
        {
            var result = await mediator.Send(new RequestPasswordResetCommand(body.Email));
            return result.ToHttpResult();
        }).AllowAnonymous();

        grp.MapPost("/password/reset", async ([FromBody] ResetPasswordRequest body, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new ResetPasswordCommand(body.Token, body.NewPassword));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).AllowAnonymous();

        grp.MapPost("/password/change", async ([FromBody] ChangePasswordRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            try
            {
                if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                    return Results.Unauthorized();
                var result = await mediator.Send(new ChangePasswordCommand(userId, body.CurrentPassword, body.NewPassword));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).RequireAuthorization();

        // CV Upload
        grp.MapPost("/cv", async (IFormFile file, ClaimsPrincipal user, Application.Abstractions.IAuthDbContext db) =>
        {
            var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".doc" && ext != ".docx")
                return Results.BadRequest(new { error = "Only PDF, DOC, DOCX allowed" });

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { error = "File size exceeds 10MB" });

            var uploadsDir = "/app/uploads/cv";
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) return Results.NotFound();
            db.Users.Entry(u).Property("CvUrl").CurrentValue = $"/uploads/cv/{fileName}";
            await db.SaveChangesAsync();

            // Analyze CV inline
            string? analysisError = null;
            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                var cvText = ExtractPdfText(bytes);
                var skills = string.IsNullOrWhiteSpace(cvText) ? new List<(string, float, string, int)>() : ParseSkillsFromText(cvText);

                if (skills.Count > 0)
                {
                    var oldSkills = db.UserSkills.Where(s => s.UserId == userId).ToList();
                    foreach (var old in oldSkills) db.UserSkills.Remove(old);

                    foreach (var (name, score, level, years) in skills)
                    {
                        db.UserSkills.Add(new Domain.Skills.UserSkill
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Name = name,
                            Score = score,
                            Level = level,
                            Source = "cv",
                            ExperienceYears = years,
                            Contexts = new List<string>(),
                            AssessedAt = DateTimeOffset.UtcNow
                        });
                    }

                    var oldAssessment = db.SkillAssessments.FirstOrDefault(a => a.UserId == userId);
                    if (oldAssessment != null) db.SkillAssessments.Remove(oldAssessment);

                    var avgScore = skills.Average(s => s.Item2);
                    var primary = skills.OrderByDescending(s => s.Item2).First().Item1;
                    var skillNames = skills.Select(s => s.Item1).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    db.SkillAssessments.Add(new Domain.Skills.SkillAssessment
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        OverallLevel = avgScore switch { < 40 => "Junior", < 60 => "Mid", < 80 => "Senior", _ => "Principal" },
                        OverallScore = (float)avgScore,
                        PrimaryExpertise = primary,
                        SecondaryExpertise = skills.OrderByDescending(s => s.Item2).Skip(1).Take(3).Select(s => s.Item1).ToList(),
                        Recommendations = RecommendProfessions(skillNames),
                        AssessedAt = DateTimeOffset.UtcNow
                    });

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex) { analysisError = ex.Message; }

            return Results.Ok(new { cvUrl = $"/uploads/cv/{fileName}" });
        }).RequireAuthorization().DisableAntiforgery();

        // Avatar Upload
        grp.MapPost("/avatar", async (IFormFile file, ClaimsPrincipal user, Application.Abstractions.IAuthDbContext db) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded" });
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                return Results.BadRequest(new { error = "Only images allowed" });
            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { error = "File size exceeds 5MB" });
            var uploadsDir = "/app/uploads/avatars";
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);
            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) return Results.NotFound();
            db.Users.Entry(u).Property("AvatarUrl").CurrentValue = $"/uploads/avatars/{fileName}";
            await db.SaveChangesAsync();
            return Results.Ok(new { avatarUrl = $"/uploads/avatars/{fileName}" });
        }).RequireAuthorization().DisableAntiforgery();

        // Cover Upload
        grp.MapPost("/cover", async (IFormFile file, ClaimsPrincipal user, Application.Abstractions.IAuthDbContext db) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded" });
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                return Results.BadRequest(new { error = "Only images allowed" });
            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { error = "File size exceeds 10MB" });
            var uploadsDir = "/app/uploads/covers";
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);
            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) return Results.NotFound();
            db.Users.Entry(u).Property("CoverUrl").CurrentValue = $"/uploads/covers/{fileName}";
            await db.SaveChangesAsync();
            return Results.Ok(new { coverUrl = $"/uploads/covers/{fileName}" });
        }).RequireAuthorization().DisableAntiforgery();

        return app;
    }

    private static readonly (string keyword, string displayName, string domain)[] KnownSkills =
    [
        // IT — languages
        ("python", "Python", "it"), ("c#", "C#", "it"), ("javascript", "JavaScript", "it"),
        ("typescript", "TypeScript", "it"), ("java", "Java", "it"), (" go ", "Go", "it"),
        ("rust", "Rust", "it"), ("c++", "C++", "it"), ("kotlin", "Kotlin", "it"), ("swift", "Swift", "it"),
        ("php", "PHP", "it"), ("ruby", "Ruby", "it"), ("scala", "Scala", "it"),
        // IT — frontend/backend
        ("react", "React", "it"), ("angular", "Angular", "it"), ("vue", "Vue.js", "it"),
        ("node", "Node.js", "it"), ("django", "Django", "it"), ("flask", "Flask", "it"),
        ("fastapi", "FastAPI", "it"), ("spring", "Spring", "it"), ("asp.net", "ASP.NET", "it"),
        ("next.js", "Next.js", "it"), ("nestjs", "NestJS", "it"), ("laravel", "Laravel", "it"),
        // IT — data/db
        ("sql", "SQL", "it"), ("postgresql", "PostgreSQL", "it"), ("mysql", "MySQL", "it"),
        ("mongodb", "MongoDB", "it"), ("redis", "Redis", "it"), ("elasticsearch", "Elasticsearch", "it"),
        ("sqlite", "SQLite", "it"), ("oracle", "Oracle DB", "it"),
        // IT — devops/cloud
        ("docker", "Docker", "it"), ("kubernetes", "Kubernetes", "it"), ("aws", "AWS", "it"),
        ("azure", "Azure", "it"), ("gcp", "GCP", "it"), ("terraform", "Terraform", "it"),
        ("linux", "Linux", "it"), ("git", "Git", "it"), ("ci/cd", "CI/CD", "it"),
        // IT — ML
        ("machine learning", "Machine Learning", "it"), ("deep learning", "Deep Learning", "it"),
        ("pytorch", "PyTorch", "it"), ("tensorflow", "TensorFlow", "it"),
        // IT — markup
        ("html", "HTML", "it"), ("css", "CSS", "it"), ("tailwind", "Tailwind CSS", "it"),
        // Non-IT — sales/business
        ("продажи", "Продажи", "sales"), ("sales", "Sales", "sales"),
        ("переговоры", "Переговоры", "sales"), ("crm", "CRM", "sales"),
        ("маркетинг", "Маркетинг", "sales"), ("marketing", "Marketing", "sales"),
        ("smm", "SMM", "marketing"), ("таргет", "Таргетированная реклама", "marketing"),
        // Non-IT — logistics/warehouse
        ("склад", "Складская логистика", "logistics"), ("логистика", "Логистика", "logistics"),
        ("logistics", "Logistics", "logistics"), ("1с", "1С", "it"),
        ("инвентаризация", "Инвентаризация", "logistics"),
        // Non-IT — sport/fitness
        ("пауэрлифтинг", "Пауэрлифтинг", "sport"), ("powerlifting", "Powerlifting", "sport"),
        ("тренер", "Персональный тренер", "sport"), ("фитнес", "Фитнес", "sport"),
        // Non-IT — soft skills
        ("коммуникаб", "Коммуникабельность", "soft"), ("teamwork", "Teamwork", "soft"),
        ("команд", "Работа в команде", "soft"), ("лидерств", "Лидерство", "soft"),
        ("leadership", "Leadership", "soft"), ("управление", "Управление", "management"),
        ("менеджмент", "Менеджмент", "management"), ("management", "Management", "management"),
        // Non-IT — languages
        ("английский", "Английский язык", "language"), ("english", "English", "language"),
        ("русский", "Русский язык", "language"), ("казахский", "Казахский язык", "language"),
    ];

    private static List<(string name, float score, string level, int years)> ParseSkillsFromText(string text)
    {
        var result = new List<(string, float, string, int)>();
        var lower = text.ToLowerInvariant();
        foreach (var (keyword, displayName, _) in KnownSkills)
        {
            if (!lower.Contains(keyword.Trim())) continue;
            var years = 1;
            var escaped = System.Text.RegularExpressions.Regex.Escape(keyword.Trim());
            var m = System.Text.RegularExpressions.Regex.Match(lower,
                $@"(\d+)\+?\s*(?:years?|лет|года?|г\.)\s+.{{0,20}}{escaped}");
            if (!m.Success)
                m = System.Text.RegularExpressions.Regex.Match(lower,
                    $@"{escaped}.{{0,10}}[-–:]\s*(\d+)\+?\s*(?:years?|лет|года?|г\.)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var y)) years = Math.Min(y, 30);
            var score = Math.Min(95f, 38f + years * 9f);
            var level = score switch { < 50f => "Beginner", < 65f => "Intermediate", < 80f => "Advanced", _ => "Expert" };
            result.Add((displayName, score, level, years));
        }
        return result;
    }

    private static readonly (string profession, string[] requiredAny, string[] bonus)[] ProfessionRules =
    [
        // IT
        ("Backend Developer",       ["C#", "Python", "Java", "Go", "Rust", "Kotlin", "PHP", "Ruby", "Scala"],    ["SQL", "Docker", "Git", "PostgreSQL", "MySQL", "MongoDB"]),
        ("Frontend Developer",      ["JavaScript", "TypeScript", "React", "Angular", "Vue.js"],                   ["HTML", "CSS", "Tailwind CSS", "Next.js"]),
        ("Full-Stack Developer",    ["JavaScript", "TypeScript", "React", "Angular", "Vue.js", "Next.js"],        ["C#", "Python", "Java", "Node.js", "SQL"]),
        ("Mobile Developer",        ["Kotlin", "Swift"],                                                           ["React", "TypeScript"]),
        ("Data Engineer",           ["Python", "SQL", "PostgreSQL", "MySQL", "MongoDB", "Elasticsearch"],         ["Docker", "AWS", "GCP", "Azure"]),
        ("ML Engineer",             ["Machine Learning", "Deep Learning", "PyTorch", "TensorFlow"],               ["Python", "SQL", "Docker"]),
        ("DevOps Engineer",         ["Docker", "Kubernetes", "Terraform", "CI/CD", "Linux"],                      ["AWS", "Azure", "GCP", "Git"]),
        ("Cloud Architect",         ["AWS", "Azure", "GCP"],                                                      ["Docker", "Kubernetes", "Terraform"]),
        // Sales / Marketing
        ("Sales Manager",           ["Продажи", "Sales", "Переговоры"],                                           ["CRM", "Коммуникабельность", "Работа в команде"]),
        ("Marketing Specialist",    ["Маркетинг", "Marketing", "SMM"],                                            ["Таргетированная реклама", "CRM"]),
        ("Account Manager",         ["CRM", "Переговоры", "Продажи"],                                             ["Коммуникабельность", "Работа в команде"]),
        // Logistics
        ("Логист",                  ["Логистика", "Logistics", "Складская логистика"],                            ["1С", "Инвентаризация"]),
        ("Кладовщик / Оператор склада", ["Складская логистика", "Инвентаризация"],                               ["1С"]),
        // Sport
        ("Персональный тренер",     ["Пауэрлифтинг", "Фитнес", "Персональный тренер"],                           ["Коммуникабельность", "Работа в команде"]),
        ("Спортивный инструктор",   ["Пауэрлифтинг", "Powerlifting"],                                             ["Коммуникабельность"]),
        // Management
        ("Team Lead",               ["Лидерство", "Leadership", "Управление", "Менеджмент", "Management"],       ["Работа в команде", "Коммуникабельность"]),
        ("Project Manager",         ["Управление", "Менеджмент", "Management"],                                   ["Работа в команде", "CRM"]),
    ];

    private static List<string> RecommendProfessions(HashSet<string> skillNames)
    {
        var scored = new List<(string profession, int score)>();
        foreach (var (profession, requiredAny, bonus) in ProfessionRules)
        {
            var matchRequired = requiredAny.Count(s => skillNames.Contains(s));
            if (matchRequired == 0) continue;
            var matchBonus = bonus.Count(s => skillNames.Contains(s));
            scored.Add((profession, matchRequired * 3 + matchBonus));
        }
        return scored.OrderByDescending(x => x.score).Take(4).Select(x => x.profession).ToList();
    }

    private static string ExtractPdfText(byte[] pdfBytes)
    {
        var sb = new StringBuilder();
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        foreach (var page in doc.GetPages())
        {
            foreach (var word in page.GetWords())
                sb.Append(word.Text).Append(' ');
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
