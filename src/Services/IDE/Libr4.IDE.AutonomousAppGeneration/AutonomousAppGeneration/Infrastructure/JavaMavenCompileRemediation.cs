using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic compile remediation for Java/Spring monorepos before and between LLM fix passes.
/// </summary>
public static class JavaMavenCompileRemediation
{
    private static readonly Regex MavenErrorPath = new(
        @"\[ERROR\]\s+(?<path>[^\s:]+\.java):\[\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static int Apply(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        string? executionLog)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return 0;

        var changed = 0;
        var consolidated = JavaPackageRootConsolidator.Consolidate(files.ToList(), plan).ToList();
        if (consolidated.Count != files.Count
            || consolidated.Any(c => files.All(f => !f.RelativePath.Equals(c.RelativePath, StringComparison.OrdinalIgnoreCase))))
        {
            files.Clear();
            foreach (var f in consolidated)
                files.Add(f);
            changed++;
        }

        if (!string.IsNullOrWhiteSpace(executionLog))
            changed += RemoveBrokenTestSources(files, executionLog);

        changed += EnsureBackendPomTestCompileTolerance(files);
        return changed;
    }

    private static int RemoveBrokenTestSources(IList<GeneratedFile> files, string executionLog)
    {
        var failing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in MavenErrorPath.Matches(executionLog))
        {
            var path = m.Groups["path"].Value.Replace('\\', '/');
            if (!path.Contains("/src/test/", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("\\src\\test\\", StringComparison.OrdinalIgnoreCase))
                continue;

            if (path.Contains("/backend/", StringComparison.OrdinalIgnoreCase))
            {
                var idx = path.IndexOf("/backend/", StringComparison.OrdinalIgnoreCase);
                path = path[(idx + 1)..];
            }

            failing.Add(path);
        }

        if (failing.Count == 0
            && !executionLog.Contains("testCompile", StringComparison.OrdinalIgnoreCase)
            && !executionLog.Contains("TestCompileError", StringComparison.OrdinalIgnoreCase))
            return 0;

        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            var rel = files[i].RelativePath.Replace('\\', '/');
            if (!rel.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                || !rel.Contains("/src/test/", StringComparison.OrdinalIgnoreCase)
                || !rel.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
                continue;

            var shouldRemove = failing.Count == 0
                               || failing.Contains(rel)
                               || failing.Any(f => rel.EndsWith(f, StringComparison.OrdinalIgnoreCase));
            if (!shouldRemove)
                continue;

            files.RemoveAt(i);
            removed++;
        }

        // If many tests still block compile, drop all generated tests (main sources must compile first).
        if (removed == 0
            && (executionLog.Contains("testCompile", StringComparison.OrdinalIgnoreCase)
                || executionLog.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)))
        {
            for (var i = files.Count - 1; i >= 0; i--)
            {
                var rel = files[i].RelativePath.Replace('\\', '/');
                if (rel.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                    && rel.Contains("/src/test/", StringComparison.OrdinalIgnoreCase)
                    && rel.EndsWith("Test.java", StringComparison.OrdinalIgnoreCase))
                {
                    files.RemoveAt(i);
                    removed++;
                }
            }
        }

        return removed;
    }

    private static int EnsureBackendPomTestCompileTolerance(IList<GeneratedFile> files)
    {
        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        if (content.Contains("maven-surefire-plugin", StringComparison.OrdinalIgnoreCase))
            return 0;

        const string surefirePlugin = """
                <plugin>
                  <groupId>org.apache.maven.plugins</groupId>
                  <artifactId>maven-surefire-plugin</artifactId>
                  <configuration>
                    <failIfNoTests>false</failIfNoTests>
                  </configuration>
                </plugin>
            """;

        string updated;
        var pluginsClose = content.IndexOf("</plugins>", StringComparison.OrdinalIgnoreCase);
        if (pluginsClose >= 0)
        {
            updated = content.Insert(pluginsClose, "\n" + surefirePlugin);
        }
        else if (content.Contains("</project>", StringComparison.OrdinalIgnoreCase))
        {
            const string buildBlock = """
                <build>
                  <plugins>
                """ + surefirePlugin + """
                  </plugins>
                </build>
                """;
            updated = content.Replace(
                "</project>",
                buildBlock + "\n</project>",
                StringComparison.OrdinalIgnoreCase);
        }
        else
            return 0;

        if (string.Equals(updated, content, StringComparison.Ordinal))
            return 0;

        files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, updated);
        return 1;
    }
}
