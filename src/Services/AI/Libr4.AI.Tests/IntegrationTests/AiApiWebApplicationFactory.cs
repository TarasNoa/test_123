using Libr4.AI.Api;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Agents;
using Libr4.AI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Libr4.AI.Tests.IntegrationTests;

public sealed class AiApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=1;Database=unused",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["RabbitMq:Host"] = "localhost",
                ["RabbitMq:User"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["Jwt:Issuer"] = "libr4-test",
                ["Jwt:Audience"] = "libr4-test",
                ["Jwt:SigningKey"] = "test-signing-key-must-be-at-least-32-characters-long-for-hmac",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AIDbContext>>();
            services.RemoveAll<AIDbContext>();
            services.RemoveAll(typeof(DbContextOptions<AIDbContext>));

            services.AddDbContext<AIDbContext>(options =>
                options.UseInMemoryDatabase("ai-api-tests"));

            services.RemoveAll<IAgentRepository>();
            services.AddSingleton<IAgentRepository, InMemoryAgentRepository>();

            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(Mock.Of<IPublishEndpoint>());

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    private sealed class InMemoryAgentRepository : IAgentRepository
    {
        private readonly List<Agent> _agents = new()
        {
            Agent.Create("TestAgent", "Tester", "You are a test agent.")
        };

        public Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_agents.FirstOrDefault(a => a.Id == id));

        public Task<IEnumerable<Agent>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Agent>>(_agents);

        public Task AddAsync(Agent agent, CancellationToken cancellationToken = default)
        {
            _agents.Add(agent);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _agents.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }
    }
}
