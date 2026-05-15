using System.Security.Claims;
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

        grp.MapPost("/register", async (RegisterRequest body, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new RegisterUserCommand(body));
                return result.IsSuccess
                    ? Results.Created($"/api/v1/users/{result.Value.Id}", result.Value)
                    : ResultExtensions.Problem(result.Error);
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
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId))
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

            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) return Results.NotFound();
            // Update CV URL via reflection or direct EF update
            db.Users.Entry(u).Property("CvUrl").CurrentValue = $"/uploads/cv/{fileName}";
            await db.SaveChangesAsync();

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
}
