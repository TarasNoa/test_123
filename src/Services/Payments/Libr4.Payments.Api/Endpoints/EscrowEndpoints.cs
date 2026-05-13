using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Application.Escrow.Commands;
using Libr4.Shared.Web.Auth;
using MediatR;

namespace Libr4.Payments.Api.Endpoints;

public static class EscrowEndpoints
{
    public static IEndpointRouteBuilder MapEscrowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/escrow")
            .WithTags("Escrow")
            .WithOpenApi()
            .RequireAuthorization();

        // Create escrow
        group.MapPost("/", async (
            CreateEscrowRequest request,
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var command = new CreateEscrowCommand(
                request.TaskId,
                user.Id, // Client is current user
                request.FreelancerId,
                request.Amount,
                request.Currency);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/escrow/{result.Value.Id}", result.Value)
                                  : Results.BadRequest(result.Error);
        })
        .WithName("CreateEscrow")
        .WithSummary("Create escrow for task (holds client funds)");

        // Release escrow
        group.MapPost("/{escrowId:guid}/release", async (
            Guid escrowId,
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var command = new ReleaseEscrowCommand(escrowId, user.Id);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("ReleaseEscrow")
        .WithSummary("Release escrow to freelancer (task completed)");

        // Refund escrow
        group.MapPost("/{escrowId:guid}/refund", async (
            Guid escrowId,
            RefundEscrowRequest request,
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            // Only client or admin can refund
            var command = new RefundEscrowCommand(escrowId, request.Reason);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("RefundEscrow")
        .WithSummary("Refund escrow to client");

        return app;
    }
}

public record CreateEscrowRequest(
    Guid TaskId,
    Guid FreelancerId,
    decimal Amount,
    string Currency);

public record RefundEscrowRequest(string Reason);
