using Libr4.Payments.Application.PaymentMethods.Commands;
using Libr4.Payments.Application.PaymentMethods.Queries;
using Libr4.Payments.Application.Transactions.Commands;
using Libr4.Payments.Application.Transactions.Queries;
using Libr4.Shared.Web.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Payments.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .WithOpenApi()
            .RequireAuthorization();

        // Create PaymentIntent
        group.MapPost("/intents", async (
            CreatePaymentIntentRequest request,
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var command = new CreatePaymentIntentCommand(
                user.Id,
                request.Amount,
                request.Currency,
                request.TaskId,
                request.Description);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("CreatePaymentIntent")
        .WithSummary("Create Stripe PaymentIntent for deposit");

        // Confirm payment (internal or from webhook)
        group.MapPost("/confirm", async (
            [FromBody] ConfirmPaymentRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ConfirmPaymentCommand(request.PaymentIntentId, request.ChargeId);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        })
        .WithName("ConfirmPayment")
        .RequireAuthorization("Admin"); // Only admin/webhook can confirm

        // Get transactions
        group.MapGet("/transactions", async (
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            // Parse enums
            Enum.TryParse<Domain.Transactions.TransactionType>(type, out var txType);
            Enum.TryParse<Domain.Transactions.TransactionStatus>(status, out var txStatus);

            var query = new GetTransactionsQuery(
                user.Id,
                string.IsNullOrEmpty(type) ? null : txType,
                string.IsNullOrEmpty(status) ? null : txStatus,
                page,
                pageSize);

            var result = await mediator.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetTransactions")
        .WithSummary("Get user transactions with filtering");

        // Get payment methods
        group.MapGet("/methods", async (
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var query = new GetPaymentMethodsQuery(user.Id);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetPaymentMethods")
        .WithSummary("Get saved payment methods");

        // Add payment method
        group.MapPost("/methods", async (
            AddPaymentMethodRequest request,
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var command = new AddPaymentMethodCommand(
                user.Id,
                request.StripePaymentMethodId,
                request.Last4,
                request.Brand,
                request.ExpMonth,
                request.ExpYear,
                request.SetAsDefault);

            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/payments/methods/{result.Value.Id}", result.Value)
                                  : Results.BadRequest(result.Error);
        })
        .WithName("AddPaymentMethod")
        .WithSummary("Add new payment method");

        // Stripe Webhook
        app.MapPost("/api/v1/payments/webhook", async (
            HttpRequest request,
            Libr4.Payments.Infrastructure.Messaging.StripeWebhookHandler handler,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync(ct);
            var stripeSignature = request.Headers["Stripe-Signature"].ToString();
            var webhookSecret = config["Stripe:WebhookSecret"];

            try
            {
                await handler.HandleAsync(json, stripeSignature, webhookSecret, ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("StripeWebhook")
        .WithTags("Payments")
        .WithSummary("Stripe webhook handler")
        .AllowAnonymous();

        return app;
    }
}

// Request DTOs for endpoints
public record CreatePaymentIntentRequest(
    decimal Amount,
    string Currency,
    Guid? TaskId,
    string? Description);

public record ConfirmPaymentRequest(
    string PaymentIntentId,
    string? ChargeId);

public record AddPaymentMethodRequest(
    string StripePaymentMethodId,
    string Last4,
    string Brand,
    int ExpMonth,
    int ExpYear,
    bool SetAsDefault = false);
