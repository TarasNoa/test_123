using Libr4.Auth.Api;
using Libr4.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.RabbitMq;

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
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add PostgreSQL from TestContainer
            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(_fixture.PostgresConnectionString));

            // Override RabbitMQ configuration
            services.Configure<RabbitMqSettings>(options =>
            {
                options.Host = _fixture.RabbitMqContainer.Hostname;
                options.Port = RabbitMqBuilder.RabbitMqPort;
                options.Username = "test";
                options.Password = "test";
            });

            // Override Redis configuration
            services.Configure<RedisSettings>(options =>
            {
                options.ConnectionString = _fixture.RedisConnectionString;
            });

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Database.Migrate();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }
}

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
}
