using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Application.GitAutomation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ShadowGitCheckpointTests
{
    [Fact]
    public async Task EnsureInitialized_CreatesRepoWithInitialCommit()
    {
        var root = CreateTempWorkspace();
        try
        {
            var svc = CreateService();
            await svc.EnsureInitializedAsync(root);

            Directory.Exists(Path.Combine(root, ".git")).Should().BeTrue();
            var diff = await svc.GetWorkingDiffAsync(root, 4000);
            diff.Should().BeEmpty();
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public async Task TagRepairAttempt_AndRewind_RestoresPriorContent()
    {
        var root = CreateTempWorkspace();
        try
        {
            var svc = CreateService();
            await svc.EnsureInitializedAsync(root);
            await WriteFile(root, "app.txt", "v1");

            await svc.TagRepairAttemptAsync(root, 1);
            await WriteFile(root, "app.txt", "v2");

            (await File.ReadAllTextAsync(Path.Combine(root, "app.txt"))).Should().Be("v2");

            var ok = await svc.RewindToTagAsync(root, IShadowGitCheckpointService.RepairTagName(1));
            ok.Should().BeTrue();
            (await File.ReadAllTextAsync(Path.Combine(root, "app.txt"))).Should().Be("v1");
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public void RepairFragmentAssembler_IncludesGitDiffFragment()
    {
        var manager = new ContextFragmentManager(Options.Create(new ContextFragmentOptions()));
        ContextFragmentRepairAssembler.Populate(
            manager,
            new RepairFragmentInput(
                "build failed",
                [],
                [],
                RepairAttempt: 2,
                GitDiffEvidence: "diff --git a/app.txt b/app.txt\n+broken"));

        manager.Fragments.Should().Contain(f => f.Type == ContextFragmentType.GitDiff);
        manager.Assemble().Should().Contain("git_diff");
    }

    private static ShadowGitCheckpointService CreateService() =>
        new(
            Options.Create(new ShadowGitCheckpointOptions { Enabled = true }),
            NullLogger<ShadowGitCheckpointService>.Instance);

    private static string CreateTempWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "libr4-shadowgit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static Task WriteFile(string root, string relativePath, string content)
    {
        var abs = Path.Combine(root, relativePath);
        var dir = Path.GetDirectoryName(abs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        return File.WriteAllTextAsync(abs, content);
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Thread.Sleep(100);
            }
        }
    }
}
