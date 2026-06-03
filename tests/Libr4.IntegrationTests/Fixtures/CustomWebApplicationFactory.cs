using Libr4.Auth.Api;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Libr4.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<AuthApiWebApplicationFactoryAnchor>
{
    private readonly IntegrationTestFixture _fixture;

    public CustomWebApplicationFactory(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, Environments.Development);
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                ["RabbitMq:Host"] = _fixture.RabbitMqContainer.Hostname,
                ["RabbitMq:User"] = "test",
                ["RabbitMq:Password"] = "test",
                ["Jwt:Issuer"] = "libr4-test",
                ["Jwt:Audience"] = "libr4-test",
                ["Jwt:SigningKey"] = "test-signing-key-must-be-at-least-32-characters-long-for-hmac",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveDbContext<AuthDbContext>(services);

            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(
                    _fixture.PostgresConnectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(AuthDbContext).Assembly.GetName().Name)));

            services.RemoveAll<IAuthDbContext>();
            services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(Mock.Of<IPublishEndpoint>());
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
        return host;
    }

    private static void RemoveDbContext<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(TContext)
                || d.ServiceType == typeof(DbContextOptions<TContext>)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }
}
