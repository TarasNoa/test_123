using System.Diagnostics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BankingSeedCompileRemediationTests
{
    [Fact]
    public void Normalize_BankingSeed_AddsPaymentResponse_AndSimplifiesRepositories()
    {
        var snapshot = LoadBankingSeed();
        if (snapshot is null)
            return;

        var files = snapshot.Files.ToList();
        var report = StackArtifactRecoveryRouter.Normalize(files, snapshot.Plan, autoFix: true);

        report.FixesApplied.Should().BeGreaterThan(0);
        files.Should().Contain(f => f.RelativePath.EndsWith("dto/PaymentResponse.java", StringComparison.OrdinalIgnoreCase));
        files.Single(f => f.RelativePath.EndsWith("repository/AccountRepository.java", StringComparison.OrdinalIgnoreCase)).Content
            .Should().NotContain("Account.AccountStatus");
        files.Single(f => f.RelativePath.EndsWith("service/PaymentService.java", StringComparison.OrdinalIgnoreCase)).Content
            .Should().Contain("getAllPayments");
    }

    [Fact]
    public async Task Normalize_BankingSeed_BackendMavenPackage_Succeeds()
    {
        var snapshot = LoadBankingSeed();
        if (snapshot is null)
            return;

        var files = snapshot.Files.ToList();
        StackArtifactRecoveryRouter.Normalize(files, snapshot.Plan, autoFix: true);

        var workspace = Path.Combine(Path.GetTempPath(), "libr4-banking-remediation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            foreach (var file in files)
            {
                var abs = Path.Combine(workspace, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(abs);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(abs, file.Content ?? string.Empty);
            }

            var mvn = Path.Combine(
                Path.GetTempPath(),
                "libr4-shadow-pool",
                "_warm-cache",
                "apache-maven",
                "bin",
                "mvn.cmd");
            if (!File.Exists(mvn))
                return;

            var m2 = Path.Combine(Path.GetTempPath(), "libr4-shadow-pool", "_warm-cache", "m2", "repository");
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments =
                    $"/c cd /d \"{Path.Combine(workspace, "backend")}\" && " +
                    $"\"{mvn}\" -B -ntp -DskipTests package -Dmaven.repo.local=\"{m2}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start mvn");
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var tail = string.Join('\n', (stdout + "\n" + stderr).Split('\n').TakeLast(40));
                throw new Xunit.Sdk.XunitException($"mvn package failed (exit {process.ExitCode}):\n{tail}");
            }

            process.ExitCode.Should().Be(0);
        }
        finally
        {
            try { Directory.Delete(workspace, recursive: true); }
            catch { /* best effort */ }
        }
    }

    private static ResumeSeedSnapshot? LoadBankingSeed()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            ".logs", "resume-seeds", "20729f31-a895-480d-a429-653aba47080f.json"));
        return File.Exists(path) ? ResumeSeedLoader.TryLoad(path) : null;
    }
}
