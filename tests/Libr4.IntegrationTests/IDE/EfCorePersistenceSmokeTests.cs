using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.Persistence.Entities;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// P2-1 smoke tests. Verify the EF persistence project wires up and the DbContext
/// model is well-formed. Real Postgres integration is exercised via host smoke tests
/// (out of scope for unit suite — requires a live database).
/// </summary>
public sealed class EfCorePersistenceSmokeTests
{
    [Fact]
    public void DbContext_ModelCreates_WithoutErrors()
    {
        // Use Npgsql provider in design-time mode (no actual connection), validates entity mapping.
        var options = new DbContextOptionsBuilder<AutoGenDbContext>()
            .UseNpgsql("Host=invalid;Database=nope;Username=u;Password=p")
            .EnableServiceProviderCaching(false)
            .Options;

        using var ctx = new AutoGenDbContext(options);

        // Triggers OnModelCreating and validates the model pipeline.
        var model = ctx.Model;

        model.Should().NotBeNull();
        var runEntity = model.FindEntityType(typeof(RunRegistryEntry));
        runEntity.Should().NotBeNull();
        runEntity!.GetTableName().Should().Be("autogen_runs");
        runEntity.FindPrimaryKey()!.Properties.Should().ContainSingle(p => p.Name == nameof(RunRegistryEntry.Id));

        var fpIndex = runEntity.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == nameof(RunRegistryEntry.Fingerprint)));
        fpIndex.Should().NotBeNull("fingerprint must be indexed for FindLatestByFingerprintAsync");
    }

    [Fact]
    public void AddPostgresPersistence_RegistersHybridRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Need an in-memory IAppGenerationRepository to exist before override.
        services.AddSingleton<IAppGenerationRepository, Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.InMemoryAppGenerationRepository>();
        services.AddPostgresPersistence("Host=localhost;Database=test;Username=u;Password=p");

        using var sp = services.BuildServiceProvider();

        var repo = sp.GetRequiredService<IAppGenerationRepository>();
        repo.Should().BeOfType<EfCoreAppGenerationRepository>();
    }

    [Fact]
    public void AddAutoGenPersistence_AcceptsCustomDbContextOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAppGenerationRepository, Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.InMemoryAppGenerationRepository>();

        // Custom configurer (e.g. a future SQLite test path).
        services.AddAutoGenPersistence(opts =>
            opts.UseNpgsql("Host=localhost;Database=alt;Username=u;Password=p"));

        using var sp = services.BuildServiceProvider();

        var repo = sp.GetRequiredService<IAppGenerationRepository>();
        repo.Should().BeOfType<EfCoreAppGenerationRepository>();
    }

    [Fact]
    public void AddPostgresPersistence_NullConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddPostgresPersistence(null!);

        act.Should().Throw<ArgumentException>();
    }
}
