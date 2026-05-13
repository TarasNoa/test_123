using Libr4.Payments.Application.Wallets.Commands;
using Libr4.Payments.Application.Wallets.Queries;
using Libr4.Shared.Web.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Payments.Api.Endpoints;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/wallets")
            .WithTags("Wallets")
            .WithOpenApi()
            .RequireAuthorization();

        // Get or create wallet (/my)
        group.MapGet("/my", async (
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var query = new GetWalletQuery(user.Id);
            var result = await mediator.Send(query, ct);

            if (result.IsSuccess)
                return Results.Ok(result.Value);

            if (result.Error.Code == "Wallet.NotFound")
            {
                var createCmd = new CreateWalletCommand(user.Id, "USD");
                var createResult = await mediator.Send(createCmd, ct);
                return createResult.IsSuccess ? Results.Ok(createResult.Value) : Results.BadRequest(createResult.Error);
            }

            return Results.BadRequest(result.Error);
        })
        .WithName("GetMyWallet")
        .WithSummary("Get or create user wallet");

        // Alias /me
        group.MapGet("/me", async (
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            var query = new GetWalletQuery(user.Id);
            var result = await mediator.Send(query, ct);

            if (result.IsSuccess)
                return Results.Ok(result.Value);

            if (result.Error.Code == "Wallet.NotFound")
            {
                var createCmd = new CreateWalletCommand(user.Id, "USD");
                var createResult = await mediator.Send(createCmd, ct);
                return createResult.IsSuccess ? Results.Ok(createResult.Value) : Results.BadRequest(createResult.Error);
            }

            return Results.BadRequest(result.Error);
        })
        .WithName("GetMeWallet")
        .WithSummary("Get or create user wallet (alias)");

        // Withdraw funds
        group.MapPost("/withdraw", async (
            WithdrawWalletRequest request,
            IMediator mediator,
            CurrentUser user,
            CancellationToken ct) =>
        {
            // Resolve wallet from current user if walletId not provided explicitly
            var walletId = request.WalletId;
            if (walletId == Guid.Empty)
            {
                var walletQuery = new GetWalletQuery(user.Id);
                var walletResult = await mediator.Send(walletQuery, ct);
                if (!walletResult.IsSuccess)
                    return Results.BadRequest(walletResult.Error);
                walletId = walletResult.Value.Id;
            }

            var command = new WithdrawWalletCommand(walletId, request.Amount, request.Currency, request.StripeAccountId);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("WithdrawWallet")
        .WithSummary("Withdraw funds from wallet");

        // Get wallet entries (ledger)
        group.MapGet("/{walletId:guid}/entries", async (
            Guid walletId,
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var query = new GetWalletEntriesQuery(walletId, page, pageSize);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        })
        .WithName("GetWalletEntries")
        .WithSummary("Get wallet ledger entries");

        return app;
    }
}

public record WithdrawWalletRequest(
    Guid WalletId,
    decimal Amount,
    string Currency,
    string? StripeAccountId);
