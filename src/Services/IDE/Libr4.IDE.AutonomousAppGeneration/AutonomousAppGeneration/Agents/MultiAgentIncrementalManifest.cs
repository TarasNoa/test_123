using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// File-scoped implementer tasks: one path per LLM call with optional expanded full-repo manifest.
/// </summary>
public static class MultiAgentIncrementalManifest
{
    private static readonly Regex PathInDescription = new(
        @"([\w./-]+\.(?:java|kt|ts|tsx|js|jsx|py|cs|json|yml|yaml|xml|sql|md|gradle|kts|properties))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SubagentDirective = new(
        @"@subagent\s+([A-Za-z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? TryParseSubagentSpecName(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var match = SubagentDirective.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static PlannedFilePathRegistry? CreateRegistry(GenerationPlan plan, AgentOrchestrationOptions options)
    {
        if (!options.UseIncrementalFileScopedGeneration)
            return null;

        var entries = options.UseExpandedJavaReactManifest
                          && StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack
            ? JavaReactExpandedFileManifest.AllForPlan(plan)
            : UniversalExpandedFileManifest.AllForPlan(plan);

        return entries.Count == 0
            ? null
            : new PlannedFilePathRegistry(entries);
    }

    public static List<AgentTask> CreateFileScopedTasks(
        AgentPhase phase,
        GenerationPlan plan,
        AgentOrchestrationOptions options,
        PlannedFilePathRegistry? registry = null,
        IRepoGraphBuilder? repoGraphBuilder = null,
        IOptions<RepoGraphOptions>? repoGraphOptions = null,
        IReadOnlyDictionary<string, string>? contentsByPath = null)
    {
        registry ??= CreateRegistry(plan, options);
        if (registry is not null)
            return CreateFromRegistry(phase, plan, registry, options, repoGraphBuilder, repoGraphOptions, contentsByPath);

        return ScopeLegacyTasks(MultiAgentTaskPlanner.CreateTasksForPhase(phase, plan, includeSubagentRoles: false));
    }

    internal static IReadOnlyList<(string Path, string Desc, string? Role)> LegacyBackendManifestEntries() =>
        LegacyBackendEntries();

    internal static IReadOnlyList<(string Path, string Desc, string? Role)> LegacyDatabaseManifestEntries() =>
        LegacyDatabaseEntries();

    private static List<AgentTask> CreateFromRegistry(
        AgentPhase phase,
        GenerationPlan plan,
        PlannedFilePathRegistry registry,
        AgentOrchestrationOptions options,
        IRepoGraphBuilder? repoGraphBuilder = null,
        IOptions<RepoGraphOptions>? repoGraphOptions = null,
        IReadOnlyDictionary<string, string>? contentsByPath = null)
    {
        var baseContext = BuildBaseContext(plan);
        var entries = OrderEntriesByRepoGraph(
            registry.EntriesForPhase(phase),
            repoGraphBuilder,
            repoGraphOptions,
            contentsByPath);
        var batches = IncrementalFileBatchGrouper.GroupEntries(
            entries,
            options.MaxFilesPerIncrementalTask,
            phase,
            options.UseFeatureScopedGeneration,
            repoGraphBuilder,
            repoGraphOptions,
            contentsByPath);

        var tasks = new List<AgentTask>(batches.Count);
        foreach (var batch in batches)
        {
            if (batch.Count == 1)
            {
                var entry = batch[0];
                var single = CreateFileTask(baseContext, entry.Path, entry.Description, entry.ImplementerRole);
                single.Context.PlannedPhasePaths = new[] { entry.Path };
                tasks.Add(single);
                continue;
            }

            var paths = batch.Select(e => e.Path).ToArray();
            var summary = string.Join(
                "; ",
                batch.Select(e => $"{e.Path} — {e.Description}"));
            var batchTask = CreateBatchTask(baseContext, paths, summary, batch[0].ImplementerRole);
            batchTask.Context.PlannedPhasePaths = paths;
            tasks.Add(batchTask);
        }

        return tasks;
    }

    private static List<AgentTask> CreateLegacyJavaReactTasks(AgentPhase phase, GenerationPlan plan)
    {
        var baseContext = BuildBaseContext(plan);
        var entries = phase switch
        {
            AgentPhase.Backend => LegacyBackendEntries(),
            AgentPhase.Frontend => LegacyFrontendEntries(),
            AgentPhase.Database => LegacyDatabaseEntries(),
            _ => Array.Empty<(string Path, string Desc, string? Role)>()
        };

        return entries
            .Select(e => CreateFileTask(baseContext, e.Path, e.Desc, e.Role))
            .ToList();
    }

    private static List<AgentTask> ScopeLegacyTasks(List<AgentTask> legacy)
    {
        var scoped = new List<AgentTask>();
        foreach (var task in legacy)
        {
            task.Subtasks.Clear();
            task.Context.ScopedOutputOnly = true;
            task.Context.Task = task;

            var paths = InferPathsFromDescription(task.Description);
            if (paths.Count > 0)
                task.Context.TargetRelativePaths = paths.ToArray();
            else
                task.Description += " Return JSON files[] for ONLY the files this task describes — not the full repository.";

            scoped.Add(task);
        }

        return scoped;
    }

    private static AgentTask CreateFileTask(
        AgentContext baseContext,
        string relativePath,
        string description,
        string? techStackOverride = null)
    {
        var ctx = CloneContext(baseContext, description);
        ctx.TargetRelativePaths = new[] { relativePath };
        ctx.ScopedOutputOnly = true;
        if (!string.IsNullOrWhiteSpace(techStackOverride))
            ctx.TechStack = techStackOverride;

        var task = new AgentTask
        {
            Description = description,
            Context = ctx
        };
        task.SubagentSpecName = TryParseSubagentSpecName(description)
                                ?? TryParseSubagentSpecName(techStackOverride);
        return task;
    }

    private static AgentTask CreateBatchTask(
        AgentContext baseContext,
        IReadOnlyList<string> relativePaths,
        string description,
        string? techStackOverride = null)
    {
        var scope =
            $"FEATURE BATCH: implement {relativePaths.Count} cohesive files in one session. " +
            $"Write each TARGET FILE via write_file (complete content). Files: {string.Join(", ", relativePaths)}. {description}";
        var ctx = CloneContext(baseContext, scope);
        ctx.TargetRelativePaths = relativePaths.ToArray();
        ctx.ScopedOutputOnly = true;
        if (!string.IsNullOrWhiteSpace(techStackOverride))
            ctx.TechStack = techStackOverride;

        var task = new AgentTask
        {
            Description = scope,
            Context = ctx
        };
        task.SubagentSpecName = TryParseSubagentSpecName(scope)
                                ?? TryParseSubagentSpecName(techStackOverride);
        return task;
    }

    private static IReadOnlyList<(string Path, string Desc, string? Role)> LegacyBackendEntries() => new[]
    {
        ("backend/pom.xml", "Maven pom: Spring Boot 3.2+, Java 21, web, validation, test deps.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/BankingApplication.java", "Spring Boot entry point.", "java-spring"),
        ("backend/src/main/resources/application.yml", "Datasource H2, server port, logging.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/service/AccountService.java", "Account service.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/service/TransferService.java", "Transfer service.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/web/AccountController.java", "REST /api/accounts.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/web/TransferController.java", "REST /api/transfers.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/web/PaymentController.java", "REST /api/payments.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/web/AuthController.java", "POST /api/auth/token.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/web/GlobalExceptionHandler.java", "@ControllerAdvice.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/config/CorrelationIdFilter.java", "Correlation id filter.", "java-spring"),
        ("backend/src/test/java/com/generated/banking/BankingApiTests.java", "Integration tests.", "java-spring")
    };

    private static IReadOnlyList<(string Path, string Desc, string? Role)> LegacyFrontendEntries() => new[]
    {
        ("frontend/package.json", "React 18 + TypeScript + Vite.", "typescript"),
        ("frontend/tsconfig.json", "TypeScript config.", "typescript"),
        ("frontend/vite.config.ts", "Vite config.", "typescript"),
        ("frontend/index.html", "HTML shell.", "typescript"),
        ("frontend/src/main.tsx", "React root.", "typescript"),
        ("frontend/src/App.tsx", "Banking UI shell.", "typescript"),
        ("frontend/src/api/client.ts", "API client.", "typescript"),
        ("frontend/src/App.test.tsx", "Vitest smoke.", "typescript")
    };

    private static IReadOnlyList<(string Path, string Desc, string? Role)> LegacyDatabaseEntries() => new[]
    {
        ("backend/src/main/resources/db/migration/V1__accounts.sql", "Flyway schema.", "java-spring"),
        ("backend/src/main/java/com/generated/banking/model/Account.java", "JPA Account entity.", "java-spring")
    };

    private static List<string> InferPathsFromDescription(string description)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in PathInDescription.Matches(description))
        {
            var path = match.Groups[1].Value.Trim().Replace('\\', '/');
            if (path.Length > 0)
                found.Add(path);
        }

        return found.ToList();
    }

    private static AgentContext BuildBaseContext(GenerationPlan plan)
    {
        var monorepoHint = StackLayoutHeuristics.UsesBackendFrontendLayout(plan)
            ? " Monorepo: backend/ + frontend/ only."
            : string.Empty;

        return new AgentContext
        {
            ApplicationName = plan.ApplicationName,
            Description = plan.ApplicationDescription + monorepoHint,
            TechStack = string.Join(", ", plan.TechStack.Languages.Concat(plan.TechStack.Frameworks))
        };
    }

    public static AgentTask CreateSinglePathRetryTask(AgentTask source, string relativePath)
    {
        var path = StackArtifactCompleteness.SanitizeRelativePath(relativePath);
        var description =
            $"Implement ONLY {path} with a complete, compilable file. Prior batched attempt returned no output for this path.";
        var task = CreateFileTask(
            CloneContext(source.Context, description),
            path,
            description,
            source.Context.TechStack);
        task.Context.PlannedPhasePaths = source.Context.PlannedPhasePaths;
        return task;
    }

    internal static IReadOnlyList<PlannedFileEntry> OrderEntriesByRepoGraph(
        IReadOnlyList<PlannedFileEntry> entries,
        IRepoGraphBuilder? repoGraphBuilder,
        IOptions<RepoGraphOptions>? repoGraphOptions,
        IReadOnlyDictionary<string, string>? contentsByPath)
    {
        if (repoGraphBuilder is null || repoGraphOptions?.Value.UseRepoGraphOrdering == false || entries.Count <= 1)
            return entries;

        var paths = entries.Select(e => e.Path).ToList();
        var ordered = repoGraphBuilder.OrderForGeneration(paths, contentsByPath);
        var byPath = entries.ToDictionary(e => e.Path, e => e, StringComparer.OrdinalIgnoreCase);
        var sorted = ordered.Where(byPath.ContainsKey).Select(p => byPath[p]).ToList();
        foreach (var entry in entries)
        {
            if (!sorted.Any(e => e.Path.Equals(entry.Path, StringComparison.OrdinalIgnoreCase)))
                sorted.Add(entry);
        }

        return sorted;
    }

    private static AgentContext CloneContext(AgentContext source, string scopeHint) =>
        new()
        {
            ApplicationName = source.ApplicationName,
            Description = scopeHint,
            TechStack = source.TechStack
        };
}
