using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Application.CodeSnippets;
using Libr4.Chat.Application.Files.Commands;
using Libr4.Chat.Application.Servers;
using Libr4.Chat.Infrastructure.Hubs;
using Libr4.Chat.Infrastructure.Persistence;
using Libr4.Chat.Infrastructure.Repositories;
using Libr4.Chat.Infrastructure.Services;
using Libr4.Chat.Infrastructure.Storage;
using Libr4.Shared.Infrastructure.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ChatDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "chat"));
        });

        services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ChatDbContext>());

        // SignalR with Redis backplane for scaling
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddSignalR()
                .AddStackExchangeRedis(redisConnection);
        }
        else
        {
            services.AddSignalR();
        }

        // MassTransit with RabbitMQ
        services.AddLibr4MassTransit(configuration, x =>
        {
            x.AddConsumers(typeof(DependencyInjection).Assembly);
        });

        // Storage service (S3/MinIO) — conditional registration
        var awsKey = configuration["AWS:AccessKey"];
        if (!string.IsNullOrEmpty(awsKey))
        {
            services.AddSingleton<IStorageService, S3StorageService>();
        }
        else
        {
            services.AddSingleton<IStorageService, LocalStorageService>();
        }

        // Server feature
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IServerService, ServerService>();

        // Code snippets
        services.AddScoped<ICodeSnippetService, CodeSnippetService>();

        // Repositories
        services.AddScoped<ICallRepository, CallRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // File storage (local disk for dev)
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Media service (WebRTC) — dev stub
        services.AddScoped<IMediaService, NullMediaService>();

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<ChatDbContext>("chat-db");

        return services;
    }
}