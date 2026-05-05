using System.Security.Claims;
using FluentValidation;
using Libr4.Shared.Web.Results;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Application.Reviews.Commands;
using Libr4.Tasks.Application.Reviews.Queries;
using Libr4.Tasks.Application.Tasks.Commands;
using Libr4.Tasks.Application.Tasks.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Tasks.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/tasks").WithTags("Tasks");

        // Tasks
        grp.MapGet("/", async ([AsParameters] GetTasksQuery query, ISender mediator) =>
        {
            var result = await mediator.Send(query);
            return Results.Ok(result);
        }).AllowAnonymous();

        grp.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender mediator) =>
        {
            var userId = GetUserId(user);
            var result = await mediator.Send(new GetTaskByIdQuery(id, userId));
            return result.ToHttpResult();
        }).AllowAnonymous();

        grp.MapPost("/", async ([FromBody] CreateTaskRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            try
            {
                var clientId = GetUserId(user);
                if (clientId is null) return Results.Unauthorized();

                var result = await mediator.Send(new CreateTaskCommand(body, clientId.Value));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).RequireAuthorization();

        grp.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTaskRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            try
            {
                var clientId = GetUserId(user);
                if (clientId is null) return Results.Unauthorized();

                var result = await mediator.Send(new UpdateTaskCommand(id, body, clientId.Value));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).RequireAuthorization();

        grp.MapPost("/{id:guid}/publish", async (Guid id, ClaimsPrincipal user, ISender mediator) =>
        {
            var clientId = GetUserId(user);
            if (clientId is null) return Results.Unauthorized();

            var result = await mediator.Send(new PublishTaskCommand(id, clientId.Value));
            return result.ToHttpResult();
        }).RequireAuthorization();

        grp.MapPost("/{id:guid}/complete", async (Guid id, ClaimsPrincipal user, ISender mediator) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await mediator.Send(new CompleteTaskCommand(id, userId.Value));
            return result.ToHttpResult();
        }).RequireAuthorization();

        grp.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, ISender mediator) =>
        {
            var clientId = GetUserId(user);
            if (clientId is null) return Results.Unauthorized();

            var result = await mediator.Send(new CancelTaskCommand(id, clientId.Value));
            return result.ToHttpResult();
        }).RequireAuthorization();

        // Applications
        grp.MapPost("/{id:guid}/apply", async (Guid id, [FromBody] ApplyToTaskRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            try
            {
                var freelancerId = GetUserId(user);
                if (freelancerId is null) return Results.Unauthorized();

                var result = await mediator.Send(new ApplyToTaskCommand(id, body, freelancerId.Value));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).RequireAuthorization();

        grp.MapPost("/{taskId:guid}/applications/{applicationId:guid}/accept", async (Guid taskId, Guid applicationId, ClaimsPrincipal user, ISender mediator) =>
        {
            var clientId = GetUserId(user);
            if (clientId is null) return Results.Unauthorized();

            var result = await mediator.Send(new AcceptApplicationCommand(taskId, applicationId, clientId.Value));
            return result.ToHttpResult();
        }).RequireAuthorization();

        grp.MapGet("/{id:guid}/applications", async (Guid id, ClaimsPrincipal user, ISender mediator) =>
        {
            var clientId = GetUserId(user);
            if (clientId is null) return Results.Unauthorized();

            var result = await mediator.Send(new GetTaskApplicationsQuery(id, clientId.Value));
            return Results.Ok(result);
        }).RequireAuthorization();

        // My Applications
        grp.MapGet("/my/applications", async (ClaimsPrincipal user, ISender mediator, [FromQuery] string? status) =>
        {
            var freelancerId = GetUserId(user);
            if (freelancerId is null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyApplicationsQuery(freelancerId.Value, status));
            return Results.Ok(result);
        }).RequireAuthorization();

        grp.MapPost("/my/applications/{applicationId:guid}/withdraw", async (Guid applicationId, ClaimsPrincipal user, ISender mediator) =>
        {
            var freelancerId = GetUserId(user);
            if (freelancerId is null) return Results.Unauthorized();

            var result = await mediator.Send(new WithdrawApplicationCommand(applicationId, freelancerId.Value));
            return result.ToHttpResult();
        }).RequireAuthorization();

        // Reviews
        grp.MapGet("/{id:guid}/reviews", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetTaskReviewsQuery(id));
            return Results.Ok(result);
        }).AllowAnonymous();

        grp.MapPost("/reviews", async ([FromBody] CreateReviewRequest body, ClaimsPrincipal user, ISender mediator) =>
        {
            try
            {
                var reviewerId = GetUserId(user);
                if (reviewerId is null) return Results.Unauthorized();

                var result = await mediator.Send(new CreateReviewCommand(body, reviewerId.Value));
                return result.ToHttpResult();
            }
            catch (ValidationException vx)
            {
                return Results.ValidationProblem(vx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
        }).RequireAuthorization();

        grp.MapGet("/users/{userId:guid}/reviews", async (Guid userId, [FromQuery] bool asReviewee, ISender mediator) =>
        {
            var result = await mediator.Send(new GetUserReviewsQuery(userId, asReviewee));
            return Results.Ok(result);
        }).AllowAnonymous();

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
