using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Libr4.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgreSqlContainer PostgresContainer { get; private set; } = null!;
    public RabbitMqContainer RabbitMqContainer { get; private set; } = null!;
    public RedisContainer RedisContainer { get; private set; } = null!;
    
    public string PostgresConnectionString => PostgresContainer.GetConnectionString();
    public string RabbitMqConnectionString => RabbitMqContainer.GetConnectionString();
    public string RedisConnectionString => RedisContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        // PostgreSQL with migration support
        PostgresContainer = new PostgreSqlBuilder()
            .WithDatabase("libr4_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        // RabbitMQ for MassTransit
        RabbitMqContainer = new RabbitMqBuilder()
            .WithUsername("test")
            .WithPassword("test")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
            .Build();

        // Redis for SignalR backplane
        RedisContainer = new RedisBuilder()
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();

        // Start all containers in parallel
        await Task.WhenAll(
            PostgresContainer.StartAsync(),
            RabbitMqContainer.StartAsync(),
            RedisContainer.StartAsync()
        );
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            PostgresContainer.StopAsync(),
            RabbitMqContainer.StopAsync(),
            RedisContainer.StopAsync()
        );
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
