using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class HonchoMemoryTests : IDisposable
{
    private readonly string _personaRoot;
    private readonly HonchoMemoryService _service;

    public HonchoMemoryTests()
    {
        _personaRoot = Path.Combine(Path.GetTempPath(), $"honcho-{Guid.NewGuid():N}");
        var options = Options.Create(new HonchoMemoryOptions
        {
            Enabled = true,
            PersonaRoot = _personaRoot,
            UseRemoteDialectic = false,
            FallbackToLocalPersona = true
        });

        _service = new HonchoMemoryService(
            options,
            new NullHonchoMemoryClient(),
            new FilePersonaStore(options),
            NullLogger<HonchoMemoryService>.Instance,
            new UserProfileService(
                new FileUserProfileStore(Options.Create(new UserProfileOptions { UsersRoot = _personaRoot })),
                Options.Create(new UserProfileOptions { Enabled = true, UsersRoot = _personaRoot }),
                NullLogger<UserProfileService>.Instance));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_personaRoot))
                Directory.Delete(_personaRoot, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public void ProjectKeyResolver_UsesWorkspacePathWhenProvided()
    {
        var orchestrator = CreateOrchestrator("alice", "django app");
        var withPath = HonchoProjectKeyResolver.Resolve(orchestrator, @"D:\projects\demo");
        var withoutPath = HonchoProjectKeyResolver.Resolve(orchestrator, null);
        withPath.Should().NotBe(withoutPath);
    }

    [Fact]
    public async Task SyncRun_CompletedRun_WritesProjectPersona()
    {
        var orchestrator = CreateOrchestrator("alice", "build calorie tracker");
        orchestrator.SetTenantId("alice");
        orchestrator.AttachPlan(SamplePlan("CalorieApp", "django"));
        orchestrator.BeginGeneration();
        orchestrator.MarkCompleted();

        await _service.SyncRunAsync(orchestrator, @"D:\projects\calorie");

        var projectKey = HonchoProjectKeyResolver.Resolve(orchestrator, @"D:\projects\calorie");
        var persona = await _service.GetPersonaAsync("alice", projectKey);
        persona.Should().NotBeNull();
        persona!.ProjectPatterns.Should().NotBeEmpty();
        persona.Conclusions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AugmentPlanningRequest_InjectsPersonaSection()
    {
        var orchestrator = CreateOrchestrator("planner", "next shop");
        orchestrator.SetTenantId("planner");
        orchestrator.AttachPlan(SamplePlan("Shop", "next.js"));
        orchestrator.BeginGeneration();
        orchestrator.MarkCompleted();
        await _service.SyncRunAsync(orchestrator, @"D:\projects\shop");

        var augmented = await _service.AugmentPlanningRequestAsync(
            orchestrator,
            "build ecommerce storefront",
            @"D:\projects\shop");

        augmented.Should().Contain("## honcho_persona");
        augmented.Should().EndWith("build ecommerce storefront");
    }

    [Fact]
    public async Task PersonasAreIsolated_PerUserPerProject()
    {
        var aliceRun = CreateOrchestrator("alice", "django app");
        aliceRun.SetTenantId("alice");
        aliceRun.AttachPlan(SamplePlan("AliceApp", "django"));
        aliceRun.BeginGeneration();
        aliceRun.MarkCompleted();

        var bobRun = CreateOrchestrator("bob", "next app");
        bobRun.SetTenantId("bob");
        bobRun.AttachPlan(SamplePlan("BobApp", "next.js"));
        bobRun.BeginGeneration();
        bobRun.MarkCompleted();

        await _service.SyncRunAsync(aliceRun, @"D:\projects\alpha");
        await _service.SyncRunAsync(bobRun, @"D:\projects\beta");

        var aliceKey = HonchoProjectKeyResolver.Resolve(aliceRun, @"D:\projects\alpha");
        var bobKey = HonchoProjectKeyResolver.Resolve(bobRun, @"D:\projects\beta");

        var alicePersona = await _service.GetPersonaAsync("alice", aliceKey);
        var bobPersona = await _service.GetPersonaAsync("bob", bobKey);

        alicePersona!.ProjectPatterns.Should().Contain(p => p.Contains("AliceApp", StringComparison.OrdinalIgnoreCase));
        bobPersona!.ProjectPatterns.Should().Contain(p => p.Contains("BobApp", StringComparison.OrdinalIgnoreCase));
    }

    private static AppGenerationOrchestrator CreateOrchestrator(string actor, string request)
    {
        var orchestrator = AppGenerationOrchestrator.Create(request, $"fp-{actor}");
        orchestrator.RecordTrigger(new TriggerIngestionAuditEntry(
            orchestrator.Id,
            "http",
            "http",
            request,
            Actor: actor,
            CorrelationId: null,
            ReceivedAtUtc: DateTime.UtcNow));
        return orchestrator;
    }

    private static GenerationPlan SamplePlan(string appName, params string[] stackParts) =>
        new(
            appName,
            "sample app",
            new TechStack(
                stackParts.Any(part => string.Equals(part, "python", StringComparison.OrdinalIgnoreCase)) ? ["Python"] : ["TypeScript"],
                stackParts.Where(part => !string.Equals(part, "python", StringComparison.OrdinalIgnoreCase)
                                         && !string.Equals(part, "typescript", StringComparison.OrdinalIgnoreCase)).ToList(),
                ["PostgreSQL"],
                ["Docker"],
                "test stack"),
            Array.Empty<GenerationPhase>(),
            ["CodeGenerationAgent"],
            "python:3.12-slim",
            ["pip install -r requirements.txt"],
            ["pytest"],
            10);
}
