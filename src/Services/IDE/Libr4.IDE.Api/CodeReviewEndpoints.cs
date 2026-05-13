using Libr4.IDE.Application.CodeReview.Commands;
using Libr4.IDE.Application.CodeReview.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class CodeReviewEndpoints
{
    public static void MapCodeReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/code-review")
            .RequireAuthorization();
        
        group.MapPost("/run-review", async (
            [FromBody] RunCodeReviewCommand command,
            IMediator mediator,
            RunCodeReviewCommandValidator validator,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            
            var result = await mediator.Send(command, ct);
            
            return Results.Ok(result);
        });
    }
}
