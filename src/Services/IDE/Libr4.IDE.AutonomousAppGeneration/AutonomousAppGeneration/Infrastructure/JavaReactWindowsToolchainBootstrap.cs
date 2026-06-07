using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Windows host prep for Java/React builds when WSL distro cannot run apt/apk (e.g. docker-desktop).
/// Downloads portable Maven into the workspace when mvn is not on PATH.
/// </summary>
public static class JavaReactWindowsToolchainBootstrap
{
    public const string MavenDownloadUri =
        "https://archive.apache.org/dist/maven/maven-3/3.9.9/binaries/apache-maven-3.9.9-bin.zip";

    public const string MavenExecutable = ".libr4-toolchain\\apache-maven\\bin\\mvn.cmd";

    public const string Command =
        "if not exist .libr4-toolchain mkdir .libr4-toolchain && " +
        "if not exist .libr4-toolchain\\apache-maven\\bin\\mvn.cmd (" +
        "powershell -NoProfile -Command \"& { " +
        "$ErrorActionPreference='Stop'; " +
        "$zip = '.libr4-toolchain/maven.zip'; " +
        "$uri = 'https://archive.apache.org/dist/maven/maven-3/3.9.9/binaries/apache-maven-3.9.9-bin.zip'; " +
        "Invoke-WebRequest -Uri $uri -OutFile $zip; " +
        "Expand-Archive -Force $zip '.libr4-toolchain'; " +
        "if (Test-Path '.libr4-toolchain/apache-maven-3.9.9') { Move-Item -Force '.libr4-toolchain/apache-maven-3.9.9' '.libr4-toolchain/apache-maven' } " +
        "}\"" +
        ") && " +
        "if not exist .libr4-toolchain\\apache-maven\\bin\\mvn.cmd (echo LIBR4_MAVEN_BOOTSTRAP_FAILED & exit /b 1)";

    public const string JavaHomeExports =
        "if not defined JAVA_HOME for /f \"delims=\" %J in ('where java 2^>nul') do set \"JAVA_HOME=%~dpJ..\"";

    public const string MavenPathExports =
        $"{JavaHomeExports} && set \"PATH=%CD%\\.libr4-toolchain\\apache-maven\\bin;%PATH%\"";

    public static bool IsMavenInvocation(string command) =>
        !string.IsNullOrWhiteSpace(command)
        && System.Text.RegularExpressions.Regex.IsMatch(command, @"\bmvn\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        && !command.Contains("maven.zip", StringComparison.OrdinalIgnoreCase)
        && !command.Contains("apache-maven", StringComparison.OrdinalIgnoreCase)
        && !command.Contains("mvn.cmd", StringComparison.OrdinalIgnoreCase);

    public static string QualifyMavenExecutable(string command) =>
        System.Text.RegularExpressions.Regex.Replace(
            command,
            @"\bmvn\b",
            $"\"%CD%\\{MavenExecutable}\"",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public static bool ShouldPrepend(string providerName, GenerationPlan plan) =>
        OperatingSystem.IsWindows()
        && (string.Equals(providerName, "process", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(providerName, "wsl", StringComparison.OrdinalIgnoreCase)
                && WslIsolatedRuntime.UsesHostWindowsExecution))
        && StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;
}
