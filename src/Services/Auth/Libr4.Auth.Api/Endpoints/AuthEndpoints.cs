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

        return app;
    }
}

public static class AuthEndpointExtensions
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthService service) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email and password are required" });
            }

            try
            {
                var response = await service.LoginAsync(request);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Login failed: {ex.Message}",
                    statusCode: 500,
                    title: "Login Error");
            }
        })
        .WithName("Login")
        .WithSummary("Authenticate user and return tokens");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAuthService service) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email, username, and password are required" });
            }

            if (request.Password.Length < 8)
            {
                return Results.BadRequest(new { error = "Password must be at least 8 characters" });
            }

            try
            {
                var response = await service.RegisterAsync(request);
                return Results.Created("/api/auth/login", response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Registration failed: {ex.Message}",
                    statusCode: 500,
                    title: "Registration Error");
            }
        })
        .WithName("Register")
        .WithSummary("Register a new user");

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequest request,
            IAuthService service) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new { error = "Refresh token is required" });
            }

            try
            {
                var response = await service.RefreshTokenAsync(request.RefreshToken);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Token refresh failed: {ex.Message}",
                    statusCode: 500,
                    title: "Token Refresh Error");
            }
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh access token");

        group.MapPost("/logout", async (
            [FromBody] RefreshTokenRequest request,
            IAuthService service) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new { error = "Refresh token is required" });
            }

            try
            {
                await service.LogoutAsync(request.RefreshToken);
                return Results.Ok(new { message = "Logged out" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Logout failed: {ex.Message}",
                    statusCode: 500,
                    title: "Logout Error");
            }
        })
        .WithName("Logout")
        .WithSummary("Logout user by revoking refresh token");
    }
}

public record RefreshTokenRequest(string RefreshToken);
