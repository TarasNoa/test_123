using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SkillCrystallizationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _crystallizedRoot;
    private readonly SqliteRepairPlaybookStore _store;

    public SkillCrystallizationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"skill-crystal-{Guid.NewGuid():N}.db");
        _crystallizedRoot = Path.Combine(Path.GetTempPath(), $"crystallized-{Guid.NewGuid():N}");
        _store = new SqliteRepairPlaybookStore(
            Options.Create(new RepairPlaybookOptions { DbPath = _dbPath }),
            NullLogger<SqliteRepairPlaybookStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_crystallizedRoot))
                Directory.Delete(_crystallizedRoot, recursive: true);
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task ThreeSuccessfulRepairs_CrystallizesSkillAndRefreshesManifest()
    {
        const string signature = "ImportError|django.conf|settings";
        const string fix = "apply_patch:normalize_django_settings";
        var registry = CreateRegistry();
        var crystallizer = CreateCrystallizer(registry, requireApproval: false);
        var playbook = new RepairPlaybookService(_store, crystallizer);

        for (var i = 0; i < 3; i++)
            await playbook.RecordOutcomeAsync(signature, fix, succeeded: true, stackPattern: "django|python");

        var activeFiles = Directory.GetFiles(_crystallizedRoot, "*.md", SearchOption.TopDirectoryOnly);
        activeFiles.Should().HaveCount(1);
        var content = await File.ReadAllTextAsync(activeFiles[0]);
        content.Should().Contain("Trigger Conditions");
        content.Should().Contain(fix);
        content.Should().Contain("approval: active");

        registry.List().Should().Contain(entry => entry.Id.StartsWith("crystallized-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RequireHumanApproval_QueuesPendingSkill_NotIndexedUntilApproved()
    {
        const string signature = "SyntaxError|manage.py|invalid";
        const string fix = "edit_file:manage.py";
        var registry = CreateRegistry();
        var crystallizer = CreateCrystallizer(registry, requireApproval: true);
        var playbook = new RepairPlaybookService(_store, crystallizer);

        for (var i = 0; i < 3; i++)
            await playbook.RecordOutcomeAsync(signature, fix, succeeded: true, stackPattern: "django|python");

        var pendingDir = Path.Combine(_crystallizedRoot, "pending");
        Directory.GetFiles(pendingDir, "*.md").Should().HaveCount(1);
        Directory.GetFiles(_crystallizedRoot, "*.md", SearchOption.TopDirectoryOnly).Should().BeEmpty();
        registry.List().Should().NotContain(entry => entry.Id.Contains("SyntaxError", StringComparison.OrdinalIgnoreCase));

        var approved = await crystallizer.ApprovePendingAsync(signature);
        approved.Should().BeTrue();
        Directory.GetFiles(_crystallizedRoot, "*.md", SearchOption.TopDirectoryOnly).Should().HaveCount(1);
        registry.List().Should().Contain(entry => entry.Id.StartsWith("crystallized-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BelowThreshold_DoesNotCrystallize()
    {
        const string signature = "ModuleNotFoundError|django|missing";
        var registry = CreateRegistry();
        var crystallizer = CreateCrystallizer(registry, requireApproval: false);
        var playbook = new RepairPlaybookService(_store, crystallizer);

        await playbook.RecordOutcomeAsync(signature, "pip install django", succeeded: true, stackPattern: "django|python");
        await playbook.RecordOutcomeAsync(signature, "pip install django", succeeded: true, stackPattern: "django|python");

        if (Directory.Exists(_crystallizedRoot))
            Directory.GetFiles(_crystallizedRoot, "*.md", SearchOption.AllDirectories).Should().BeEmpty();
    }

    private FileSkillManifestRegistry CreateRegistry() =>
        new(Options.Create(new SkillActivationOptions { CrystallizedSkillsRoot = _crystallizedRoot }));

    private FileSkillCrystallizer CreateCrystallizer(FileSkillManifestRegistry registry, bool requireApproval) =>
        new(
            Options.Create(new SkillCrystallizationOptions
            {
                CrystallizedSkillsRoot = _crystallizedRoot,
                CrystallizeAfterSuccessCount = 3,
                RequireHumanApproval = requireApproval
            }),
            NullLogger<FileSkillCrystallizer>.Instance,
            registry);
}
