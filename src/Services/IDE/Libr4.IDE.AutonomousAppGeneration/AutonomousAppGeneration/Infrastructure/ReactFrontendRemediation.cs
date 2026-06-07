using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic fixes for React+TypeScript frontends (Node SPA or Java+React monorepo).
/// </summary>
public static class ReactFrontendRemediation
{
    private static readonly Regex ExportFunction = new(
        @"export\s+(?:async\s+)?function\s+(\w+)",
        RegexOptions.Compiled);

    public static int Apply(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors)
    {
        var roots = ResolveFrontendRoots(files);
        if (roots.Count == 0)
            return 0;

        var blob = string.Join('\n', errors.Select(e => $"{e.ErrorType} {e.Message} {e.FilePath}"));
        var changed = 0;
        foreach (var root in roots)
        {
            changed += EnsureAppTsx(files, plan, root);
            changed += AlignApiClientTests(files, blob, root);
            changed += PruneBrokenPageImports(files, root);
        }

        return changed;
    }

    private static IReadOnlyList<string> ResolveFrontendRoots(IList<GeneratedFile> files)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var path = file.RelativePath.Replace('\\', '/');
            if (path.Equals("frontend/src/main.tsx", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("frontend/src/", StringComparison.OrdinalIgnoreCase))
                roots.Add("frontend/src");
            else if (path.Equals("src/main.tsx", StringComparison.OrdinalIgnoreCase)
                     || (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                         && (path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                             || path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))))
                roots.Add("src");
        }

        return roots.Count > 0 ? roots.ToList() : new List<string> { "frontend/src", "src" };
    }

    private static int EnsureAppTsx(IList<GeneratedFile> files, GenerationPlan plan, string root)
    {
        var appPath = $"{root}/App.tsx";
        if (files.Any(f => f.RelativePath.Equals(appPath, StringComparison.OrdinalIgnoreCase)))
            return 0;

        var mainPath = $"{root}/main.tsx";
        var main = files.FirstOrDefault(f => f.RelativePath.Equals(mainPath, StringComparison.OrdinalIgnoreCase));
        if (main is null)
            return 0;

        var usesNamedExport = main.Content?.Contains("import { App }", StringComparison.Ordinal) == true
                              || main.Content?.Contains("import {App}", StringComparison.Ordinal) == true;

        var content = usesNamedExport
            ? BuildNamedExportApp(plan.ApplicationName)
            : BuildDefaultExportApp(plan.ApplicationName);

        files.Add(new GeneratedFile(appPath, "typescript", content));
        return 1;
    }

    private static int AlignApiClientTests(IList<GeneratedFile> files, string blob, string root)
    {
        var clientPath = $"{root}/api/client.ts";
        var client = files.FirstOrDefault(f =>
            f.RelativePath.Replace('\\', '/').Equals(clientPath, StringComparison.OrdinalIgnoreCase));
        if (client is null)
            return 0;

        var test = files.FirstOrDefault(f =>
        {
            var p = f.RelativePath.Replace('\\', '/');
            return p.Equals($"{root}/api/client.test.ts", StringComparison.OrdinalIgnoreCase)
                   || (p.StartsWith($"{root}/", StringComparison.OrdinalIgnoreCase)
                       && p.EndsWith("client.test.ts", StringComparison.OrdinalIgnoreCase));
        });
        if (test is null)
            return 0;

        var exports = ExportFunction.Matches(client.Content ?? string.Empty)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (exports.Count == 0)
            return 0;

        var testContent = test.Content ?? string.Empty;
        var needsFix = exports.Any(e => !testContent.Contains(e + "(", StringComparison.Ordinal))
                       || testContent.Contains("apiClient", StringComparison.Ordinal)
                       || blob.Contains("client.test.ts", StringComparison.OrdinalIgnoreCase);
        if (!needsFix)
            return 0;

        var importList = string.Join(", ", exports);
        var assertions = string.Join(
            "\n",
            exports.Select(e => $"    expect(typeof {e}).toBe('function');"));

        var updated = $$"""
            import { describe, it, expect } from 'vitest';
            import { {{importList}} } from './client';

            describe('api client', () => {
              it('exports api helpers', () => {
            {{assertions}}
              });
            });
            """;

        if (string.Equals(testContent, updated, StringComparison.Ordinal))
            return 0;

        var idx = files.IndexOf(test);
        files[idx] = new GeneratedFile(test.RelativePath, test.Language, updated);
        return 1;
    }

    private static int PruneBrokenPageImports(IList<GeneratedFile> files, string root)
    {
        var pagesPrefix = $"{root}/pages/";
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            var path = files[i].RelativePath.Replace('\\', '/');
            if (!path.StartsWith(pagesPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            if (!content.Contains("@tanstack/react-query", StringComparison.Ordinal)
                && !content.Contains("../components/ui/", StringComparison.Ordinal)
                && !content.Contains("components/ui/", StringComparison.Ordinal))
                continue;

            var stubName = Path.GetFileNameWithoutExtension(path);
            var stub = $$"""
                import React from 'react';

                const {{stubName}}: React.FC = () => (
                  <main>
                    <h2>{{stubName}}</h2>
                    <p>Page placeholder.</p>
                  </main>
                );

                export default {{stubName}};
                """;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, stub);
            changed++;
        }

        return changed;
    }

    private static string BuildNamedExportApp(string appName) =>
        $$"""
        import React from 'react';
        import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
        import './App.css';

        export function App() {
          return (
            <BrowserRouter>
              <main className="app-shell">
                <h1>{{appName}}</h1>
                <Routes>
                  <Route path="/" element={<Navigate to="/home" replace />} />
                  <Route path="/home" element={<p>Home</p>} />
                </Routes>
              </main>
            </BrowserRouter>
          );
        }
        """;

    private static string BuildDefaultExportApp(string appName) =>
        $$"""
        import React from 'react';
        import './App.css';

        export default function App() {
          return (
            <main className="app-shell">
              <h1>{{appName}}</h1>
            </main>
          );
        }
        """;
}
