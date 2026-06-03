using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Post-generation safety-net (audit P1-11): reconciles `using Foo.Bar;` directives
/// in generated .cs files with PackageReference entries in the matching .csproj.
///
/// LLMs frequently emit Program.cs that references OpenTelemetry / Polly / Serilog
/// without updating the project file, which yields CS0246 at build time and aborts
/// the StrictPerPhase gate before fix iterations can run. This reconciler closes
/// that loop deterministically with a curated using→package map for common ASP.NET
/// Core production stacks.
///
/// Single source of truth for the using→package map; expand the map below to teach
/// the reconciler about new packages.
/// </summary>
public static class CsprojPackageReconciler
{
    /// <summary>
    /// Map from `using Namespace.Prefix` (matched as longest-prefix-wins) to the
    /// PackageReference Include + Version that should be present in the .csproj.
    /// Versions pinned to .NET 8 LTS-compatible releases as of 2026-04.
    /// </summary>
    private static readonly IReadOnlyList<(string UsingPrefix, string PackageId, string Version)> UsingToPackage = new[]
    {
        // OpenTelemetry — split across several packages; map by deepest namespace.
        ("OpenTelemetry.Instrumentation.AspNetCore", "OpenTelemetry.Instrumentation.AspNetCore", "1.9.0"),
        ("OpenTelemetry.Instrumentation.Http", "OpenTelemetry.Instrumentation.Http", "1.9.0"),
        ("OpenTelemetry.Instrumentation.Runtime", "OpenTelemetry.Instrumentation.Runtime", "1.9.0"),
        ("OpenTelemetry.Exporter.Prometheus.AspNetCore", "OpenTelemetry.Exporter.Prometheus.AspNetCore", "1.9.0-beta.2"),
        ("OpenTelemetry.Exporter.OpenTelemetryProtocol", "OpenTelemetry.Exporter.OpenTelemetryProtocol", "1.9.0"),
        ("OpenTelemetry.Extensions.Hosting", "OpenTelemetry.Extensions.Hosting", "1.9.0"),
        ("OpenTelemetry.Metrics", "OpenTelemetry.Extensions.Hosting", "1.9.0"),
        ("OpenTelemetry.Trace", "OpenTelemetry.Extensions.Hosting", "1.9.0"),
        ("OpenTelemetry", "OpenTelemetry", "1.9.0"),

        // Polly — resilience/retry policies.
        ("Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly", "8.0.8"),
        ("Polly", "Polly", "8.4.1"),

        // Serilog.
        ("Serilog.AspNetCore", "Serilog.AspNetCore", "8.0.2"),
        ("Serilog.Sinks.Console", "Serilog.Sinks.Console", "6.0.0"),
        ("Serilog.Sinks.File", "Serilog.Sinks.File", "6.0.0"),
        ("Serilog.Sinks.Seq", "Serilog.Sinks.Seq", "8.0.0"),
        ("Serilog", "Serilog", "4.0.1"),

        // EF Core + providers.
        ("Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Design", "8.0.8"),
        ("Microsoft.EntityFrameworkCore.Tools", "Microsoft.EntityFrameworkCore.Tools", "8.0.8"),
        ("Npgsql.EntityFrameworkCore.PostgreSQL", "Npgsql.EntityFrameworkCore.PostgreSQL", "8.0.4"),
        ("Microsoft.EntityFrameworkCore.SqlServer", "Microsoft.EntityFrameworkCore.SqlServer", "8.0.8"),
        ("Microsoft.EntityFrameworkCore.Sqlite", "Microsoft.EntityFrameworkCore.Sqlite", "8.0.8"),
        ("Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore", "8.0.8"),

        // ASP.NET Core auth / identity.
        ("Microsoft.AspNetCore.Authentication.JwtBearer", "Microsoft.AspNetCore.Authentication.JwtBearer", "8.0.8"),
        ("Microsoft.AspNetCore.Authentication.OpenIdConnect", "Microsoft.AspNetCore.Authentication.OpenIdConnect", "8.0.8"),
        ("Microsoft.AspNetCore.Identity.EntityFrameworkCore", "Microsoft.AspNetCore.Identity.EntityFrameworkCore", "8.0.8"),
        ("Microsoft.IdentityModel.Tokens", "Microsoft.IdentityModel.Tokens", "8.0.2"),
        ("System.IdentityModel.Tokens.Jwt", "System.IdentityModel.Tokens.Jwt", "8.0.2"),

        // Swagger / OpenAPI.
        ("Microsoft.OpenApi", "Microsoft.OpenApi", "1.6.21"),
        ("Swashbuckle.AspNetCore", "Swashbuckle.AspNetCore", "6.6.2"),

        // RabbitMQ / messaging.
        ("MassTransit.RabbitMQ", "MassTransit.RabbitMQ", "8.2.5"),
        ("MassTransit", "MassTransit", "8.2.5"),
        ("RabbitMQ.Client", "RabbitMQ.Client", "6.8.1"),

        // Caching.
        ("StackExchange.Redis", "StackExchange.Redis", "2.8.16"),
        ("Microsoft.Extensions.Caching.StackExchangeRedis", "Microsoft.Extensions.Caching.StackExchangeRedis", "8.0.8"),

        // Stripe.
        ("Stripe", "Stripe.net", "46.0.0"),

        // MediatR + FluentValidation common patterns.
        ("MediatR", "MediatR", "12.4.0"),
        ("FluentValidation.AspNetCore", "FluentValidation.AspNetCore", "11.3.0"),
        ("FluentValidation", "FluentValidation", "11.9.2"),
    };

    /// <summary>BCL namespaces and own-project namespaces never need a PackageReference.</summary>
    private static readonly HashSet<string> BclPrefixes = new(StringComparer.Ordinal)
    {
        "System", "Microsoft.AspNetCore", "Microsoft.Extensions", "Microsoft.AspNetCore.Mvc",
        "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Routing",
        "Microsoft.AspNetCore.Hosting", "Xunit",
    };

    /// <summary>
    /// Scans all generated .cs files for `using` directives, collects required packages,
    /// and ensures each .csproj that owns those .cs files has matching PackageReference
    /// entries. Mutates content of .csproj files in <paramref name="files"/> in place.
    /// Returns the number of PackageReference entries added across all .csproj files.
    /// </summary>
    public static int ReconcilePackages(IList<GeneratedFile> files)
    {
        if (files is null || files.Count == 0) return 0;

        // Group .cs files by their owning .csproj directory.
        var csprojByDir = files
            .Where(f => f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => NormalizeDir(GetDirectory(f.RelativePath)), f => f, StringComparer.OrdinalIgnoreCase);

        if (csprojByDir.Count == 0) return 0;

        var addedTotal = 0;
        foreach (var csproj in csprojByDir.Values)
        {
            var ownerDir = NormalizeDir(GetDirectory(csproj.RelativePath));
            // Find .cs files that belong to this csproj (path starts with ownerDir).
            var owned = files
                .Where(f => f.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                            && NormalizeDir(GetDirectory(f.RelativePath)).StartsWith(ownerDir, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (owned.Count == 0) continue;

            var requiredPackages = CollectRequiredPackages(owned);
            if (requiredPackages.Count == 0) continue;

            var existing = ExtractExistingPackageIds(csproj.Content);
            var toAdd = requiredPackages
                .Where(p => !existing.Contains(p.PackageId, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (toAdd.Count == 0) continue;

            var newContent = InsertPackageReferences(csproj.Content, toAdd);
            csproj.Update(newContent);
            addedTotal += toAdd.Count;
        }

        return addedTotal;
    }

    private static IReadOnlyList<(string PackageId, string Version)> CollectRequiredPackages(IReadOnlyList<GeneratedFile> csFiles)
    {
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usingRegex = new Regex(@"^\s*using\s+(?:static\s+)?([A-Za-z_][A-Za-z0-9_\.]*)\s*;", RegexOptions.Multiline);

        foreach (var f in csFiles)
        {
            if (string.IsNullOrEmpty(f.Content)) continue;
            foreach (Match m in usingRegex.Matches(f.Content))
            {
                var ns = m.Groups[1].Value;
                if (IsBclNamespace(ns)) continue;

                // Longest-prefix wins: walk our map ordered by descending prefix length.
                foreach (var (prefix, packageId, version) in UsingToPackage.OrderByDescending(p => p.UsingPrefix.Length))
                {
                    if (ns.Equals(prefix, StringComparison.Ordinal)
                        || ns.StartsWith(prefix + ".", StringComparison.Ordinal))
                    {
                        found[packageId] = version;
                        break;
                    }
                }
            }
        }

        return found.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    private static bool IsBclNamespace(string ns)
    {
        // These require explicit PackageReference entries (not covered by shared framework alone).
        if (ns.StartsWith("Microsoft.AspNetCore.Authentication.JwtBearer", StringComparison.Ordinal)
            || ns.StartsWith("System.IdentityModel.Tokens", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.IdentityModel", StringComparison.Ordinal))
        {
            return false;
        }

        // System.* and any explicitly listed BCL prefix is BCL/own.
        if (ns.StartsWith("System", StringComparison.Ordinal)
            && !ns.StartsWith("System.IdentityModel", StringComparison.Ordinal))
        {
            return true;
        }
        foreach (var prefix in BclPrefixes)
        {
            if (ns.Equals(prefix, StringComparison.Ordinal) || ns.StartsWith(prefix + ".", StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static HashSet<string> ExtractExistingPackageIds(string csprojContent)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var regex = new Regex(@"<PackageReference\s+Include\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
        foreach (Match m in regex.Matches(csprojContent ?? string.Empty))
        {
            ids.Add(m.Groups[1].Value);
        }
        return ids;
    }

    private static string InsertPackageReferences(
        string csprojContent,
        IReadOnlyList<(string PackageId, string Version)> toAdd)
    {
        var snippet = string.Join(Environment.NewLine,
            toAdd.Select(p => $"    <PackageReference Include=\"{p.PackageId}\" Version=\"{p.Version}\" />"));

        // Prefer to inject inside the first <ItemGroup> that already contains PackageReference entries.
        var packageItemGroupRegex = new Regex(
            @"(<ItemGroup[^>]*>)([^<]*?<PackageReference[\s\S]*?)(</ItemGroup>)",
            RegexOptions.IgnoreCase);
        var match = packageItemGroupRegex.Match(csprojContent);
        if (match.Success)
        {
            var head = match.Groups[1].Value;
            var body = match.Groups[2].Value.TrimEnd();
            var tail = match.Groups[3].Value;
            var rebuilt = $"{head}{Environment.NewLine}{body}{Environment.NewLine}{snippet}{Environment.NewLine}  {tail}";
            return csprojContent.Substring(0, match.Index) + rebuilt + csprojContent.Substring(match.Index + match.Length);
        }

        // Otherwise insert a new ItemGroup just before </Project>.
        var newGroup = $"  <ItemGroup>{Environment.NewLine}{snippet}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        var endProjectIdx = csprojContent.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
        if (endProjectIdx >= 0)
        {
            return csprojContent.Substring(0, endProjectIdx) + newGroup + csprojContent.Substring(endProjectIdx);
        }

        return csprojContent + Environment.NewLine + newGroup;
    }

    private static string GetDirectory(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? string.Empty : normalized.Substring(0, lastSlash);
    }

    private static string NormalizeDir(string dir)
    {
        var normalized = dir.Replace('\\', '/');
        if (normalized.Length > 0 && !normalized.EndsWith('/')) normalized += "/";
        return normalized;
    }
}
