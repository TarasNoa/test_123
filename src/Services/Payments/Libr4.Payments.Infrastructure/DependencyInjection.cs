using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Transactions.Commands;
using Libr4.Payments.Domain.Invoices;
using Libr4.Payments.Infrastructure.Messaging;
using Libr4.Payments.Infrastructure.Persistence;
using Libr4.Payments.Infrastructure.Repositories;
using Libr4.Payments.Infrastructure.Services;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Kernel.Application;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<PaymentsDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Postgres");
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "payments");
            });
        });
        services.AddScoped<IPaymentsDbContext, PaymentsDbContext>();

        // Fraud Detection
        services.AddScoped<IFraudHistoryRepository, FraudHistoryRepository>();
        // services.AddScoped<FraudDetectionService>(); // TODO: Uncomment when FraudDetectionService is available

        // Stripe
        var stripeApiKey = configuration["Stripe:SecretKey"];
        services.AddSingleton<IStripeService>(_ => new StripeService(stripeApiKey));
        services.AddScoped<StripeWebhookHandler>();

        // MassTransit with RabbitMQ
        services.AddLibr4MassTransit(configuration, x =>
        {
            x.AddConsumers(typeof(DependencyInjection).Assembly);
        });

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<PaymentsDbContext>("payments-db")
            .AddRabbitMQ();

        return services;
    }
}

