using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class UserProfileTests : IDisposable
{
    private readonly string _usersRoot;
    private readonly FileUserProfileStore _store;
    private readonly UserProfileService _service;

    public UserProfileTests()
    {
        _usersRoot = Path.Combine(Path.GetTempPath(), $"user-profiles-{Guid.NewGuid():N}");
        _store = new FileUserProfileStore(Options.Create(new UserProfileOptions { UsersRoot = _usersRoot }));
        _service = new UserProfileService(
            _store,
            Options.Create(new UserProfileOptions { UsersRoot = _usersRoot }),
            NullLogger<UserProfileService>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_usersRoot))
                Directory.Delete(_usersRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void SanitizeUserId_RemovesInvalidPathCharacters()
    {
        UserProfileIdentityResolver.SanitizeUserId("alice@corp/dev").Should().Be("alicecorpdev");
        UserProfileIdentityResolver.SanitizeUserId("../../etc").Should().Be("etc");
    }

    [Fact]
    public async Task UpdateFromRun_CompletedRun_WritesProfileWithStackAndSuccess()
    {
        var orchestrator = CreateOrchestrator("user-alice", "calorie tracker with django");
        orchestrator.SetTenantId("user-alice");
        orchestrator.AttachPlan(SamplePlan("DjangoApp", "django", "python"));
        orchestrator.BeginGeneration();
        orchestrator.CompleteIteration(
            orchestrator.BeginIteration().Id,
            new ExecutionResult(true, 0, TimeSpan.FromSeconds(2), Array.Empty<ConsoleLogEntry>()),
            Array.Empty<ErrorReport>());
        orchestrator.MarkCompleted();

        await _service.UpdateFromRunAsync(orchestrator);

        var profile = await _store.LoadAsync("user-alice");
        profile.Should().NotBeNull();
        profile!.PreferredStacks.Should().Contain(entry => entry.Stack.Contains("django", StringComparison.OrdinalIgnoreCase));
        profile.SuccessfulPatterns.Should().NotBeEmpty();
        File.Exists(_store.GetProfilePath("user-alice")).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateFromRun_FailedRun_RecordsRecurringFailure()
    {
        var orchestrator = CreateOrchestrator("user-bob", "fix manage.py");
        orchestrator.RecordTrigger(new TriggerIngestionAuditEntry(
            orchestrator.Id,
            "http",
            "http",
            "fix manage.py",
            Actor: "user-bob",
            CorrelationId: null,
            ReceivedAtUtc: DateTime.UtcNow));
        orchestrator.AttachPlan(SamplePlan("BrokenApp", "django", "python"));
        orchestrator.BeginGeneration();
        orchestrator.CompleteIteration(
            orchestrator.BeginIteration().Id,
            new ExecutionResult(false, 1, TimeSpan.FromSeconds(1), Array.Empty<ConsoleLogEntry>()),
            new[] { new ErrorReport("SyntaxError", "invalid json in manage.py", "fix syntax", "manage.py", 4) });
        orchestrator.MarkFailed("build_failed");

        await _service.UpdateFromRunAsync(orchestrator);

        var profile = await _store.LoadAsync("user-bob");
        profile.Should().NotBeNull();
        profile!.RecurringFailures.Should().Contain(entry =>
            entry.Signature.Contains("SyntaxError", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UsersAreIsolated_EachProfileStoredSeparately()
    {
        var alice = CreateOrchestrator("alice", "django app");
        alice.SetTenantId("alice");
        alice.AttachPlan(SamplePlan("AliceApp", "django", "python"));
        alice.BeginGeneration();
        alice.MarkCompleted();

        var bob = CreateOrchestrator("bob", "next app");
        bob.SetTenantId("bob");
        bob.AttachPlan(SamplePlan("BobApp", "next.js", "typescript"));
        bob.BeginGeneration();
        bob.MarkCompleted();

        await _service.UpdateFromRunAsync(alice);
        await _service.UpdateFromRunAsync(bob);

        var aliceProfile = await _store.LoadAsync("alice");
        var bobProfile = await _store.LoadAsync("bob");

        aliceProfile!.PreferredStacks.Should().Contain(entry => entry.Stack.Contains("django", StringComparison.OrdinalIgnoreCase));
        bobProfile!.PreferredStacks.Should().Contain(entry => entry.Stack.Contains("next.js", StringComparison.OrdinalIgnoreCase));
        aliceProfile.PreferredStacks.Should().NotContain(entry => entry.Stack.Contains("next.js", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AugmentPlanningRequest_InjectsProfileSection()
    {
        var orchestrator = CreateOrchestrator("planner-user", "django calorie app");
        orchestrator.SetTenantId("planner-user");
        orchestrator.AttachPlan(SamplePlan("PlannerApp", "django", "python"));
        orchestrator.BeginGeneration();
        orchestrator.CompleteIteration(
            orchestrator.BeginIteration().Id,
            new ExecutionResult(false, 1, TimeSpan.FromSeconds(1), Array.Empty<ConsoleLogEntry>()),
            new[] { new ErrorReport("ImportError", "cannot import settings", "fix imports", "settings.py", 2) });
        orchestrator.MarkFailed("tests_failed");
        await _service.UpdateFromRunAsync(orchestrator);

        var augmented = await _service.AugmentPlanningRequestAsync(orchestrator, "build another django app");

        augmented.Should().Contain("## user_profile");
        augmented.Should().Contain("Preferred Stacks");
        augmented.Should().Contain("Recurring Failures");
        augmented.Should().StartWith("## user_profile");
        augmented.Should().EndWith("build another django app");
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
                stackParts.Contains("python", StringComparer.OrdinalIgnoreCase) ? ["Python"] : ["TypeScript"],
                stackParts.Where(part => part is not "python" and not "typescript").ToList(),
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
