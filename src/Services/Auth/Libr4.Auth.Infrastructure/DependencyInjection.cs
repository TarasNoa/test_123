using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Infrastructure.Persistence;
using Libr4.Auth.Infrastructure.Services;
using Libr4.Shared.Infrastructure.Caching;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Kernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres missing");

        services.AddDbContext<AuthDbContext>(o => o.UseNpgsql(cs, npg => npg.MigrationsAssembly(typeof(AuthDbContext).Assembly.GetName().Name)));
        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddScoped<ITotpService, TotpService>();

        var amlProvider = configuration.GetValue<string>("Aml:Provider") ?? "sumsub";
        var amlApiKey = configuration.GetValue<string>("Aml:ApiKey") ?? "";
        services.AddHttpClient("AmlScreening", client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IAmlScreeningService>(sp =>
            new AmlScreeningService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AmlScreening"), amlProvider, amlApiKey));

        services.AddDataProtection();
        services.AddLibr4Redis(configuration);
        services.AddLibr4MassTransit(configuration);

        services.AddHealthChecks()
            .AddNpgSql(cs, name: "postgres", tags: new[] { "db" })
            .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis", tags: new[] { "cache" });

        return services;
    }
}
