using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// One-shot WSL prep before Java/React build commands. Installs maven/npm only when missing.
/// Kept separate from planner build commands so PlanCommandValidator does not wrap it in cd frontend.
/// </summary>
public static class JavaReactWslToolchainBootstrap
{
    public const string Command =
        "export DEBIAN_FRONTEND=noninteractive && " +
        "(command -v mvn >/dev/null 2>&1 || " +
        "(command -v apt-get >/dev/null 2>&1 && apt-get update -qq && apt-get install -y -qq maven openjdk-21-jdk-headless) || " +
        "(command -v apk >/dev/null 2>&1 && apk add --no-cache openjdk21-jdk maven) || " +
        "(command -v dnf >/dev/null 2>&1 && dnf install -y -q maven java-21-openjdk-devel) || " +
        "(command -v yum >/dev/null 2>&1 && yum install -y -q maven java-21-openjdk-devel)" +
        ") && " +
        "(command -v npm >/dev/null 2>&1 || " +
        "(command -v apt-get >/dev/null 2>&1 && apt-get install -y -qq npm) || " +
        "(command -v apk >/dev/null 2>&1 && apk add --no-cache npm)" +
        ")";

    public const string JavaHomeExports =
        "if [ -z \"${JAVA_HOME:-}\" ]; then " +
        "if [ -d /usr/lib/jvm/java-21-openjdk ]; then export JAVA_HOME=/usr/lib/jvm/java-21-openjdk; " +
        "elif [ -d /usr/lib/jvm/default-java ]; then export JAVA_HOME=/usr/lib/jvm/default-java; " +
        "elif command -v java >/dev/null 2>&1; then export JAVA_HOME=\"$(cd \"$(dirname \"$(command -v java)\")/..\" && pwd)\"; " +
        "fi; fi && " +
        "[ -z \"${JAVA_HOME:-}\" ] || export PATH=\"$JAVA_HOME/bin:$PATH\"";

    public static bool ShouldPrepend(string providerName, GenerationPlan plan) =>
        string.Equals(providerName, "wsl", StringComparison.OrdinalIgnoreCase)
        && !WslIsolatedRuntime.UsesHostWindowsExecution
        && StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;
}
