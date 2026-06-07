using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RepairPlaybookPersistenceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteRepairPlaybookStore _store;
    private readonly RepairPlaybookService _playbook;

    public RepairPlaybookPersistenceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"repair-playbook-{Guid.NewGuid():N}.db");
        _store = new SqliteRepairPlaybookStore(
            Options.Create(new RepairPlaybookOptions { DbPath = _dbPath }),
            NullLogger<SqliteRepairPlaybookStore>.Instance);
        _playbook = new RepairPlaybookService(_store);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task SameErrorTwice_SecondRepairUsesPersistedPlaybookHint()
    {
        var plan = SamplePlan();
        var errors = new[]
        {
            new ErrorReport(
                "ImportError",
                "cannot import name settings from django.conf",
                "fix imports",
                "backend/settings.py",
                12)
        };

        var signature = RepairPlaybookSignature.FromErrors(errors, buildLog: null, plan);
        const string fix = "apply_patch:normalize_django_settings";

        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: true);
        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: true);

        var firstHint = await _playbook.TryGetHintAsync(errors, null, plan);
        firstHint.Should().Be(fix);

        var reloadedStore = new SqliteRepairPlaybookStore(
            Options.Create(new RepairPlaybookOptions { DbPath = _dbPath }),
            NullLogger<SqliteRepairPlaybookStore>.Instance);
        var reloadedPlaybook = new RepairPlaybookService(reloadedStore);

        var secondHint = await reloadedPlaybook.TryGetHintAsync(errors, null, plan);
        secondHint.Should().Be(fix);
        signature.Signature.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RecordFail_DecaysScoreBelowHintThreshold()
    {
        var plan = SamplePlan();
        var errors = new[]
        {
            new ErrorReport("SyntaxError", "invalid syntax in manage.py", "fix syntax", "manage.py", 4)
        };
        const string fix = "edit_file:manage.py";

        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: true);
        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: true);
        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: false);
        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: false);
        await _playbook.RecordOutcomeAsync(errors, null, plan, fix, succeeded: false);

        var hint = await _playbook.TryGetHintAsync(errors, null, plan);
        hint.Should().BeNull();
    }

    [Fact]
    public void Signature_IsStableForSameError()
    {
        var error = new ErrorReport(
            "ModuleNotFoundError",
            "No module named django",
            "install django",
            "backend/app/views.py",
            3);
        var a = RepairPlaybookSignature.FromError(error);
        var b = RepairPlaybookSignature.FromError(error);
        a.Should().Be(b);
    }

    private static GenerationPlan SamplePlan() =>
        new(
            "DjangoApp",
            "Calorie tracker",
            new TechStack(["Python"], ["Django"], [], [], "django"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12-slim",
            Array.Empty<string>(),
            Array.Empty<string>());
}
