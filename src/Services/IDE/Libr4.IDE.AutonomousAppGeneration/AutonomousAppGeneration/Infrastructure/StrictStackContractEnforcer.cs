using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Locks explicit user-requested tech stack so planners/golden paths cannot substitute React/Vue/Nuxt/Express for Django+SolidJS etc.
/// </summary>
public static class StrictStackContractEnforcer
{
    public const string ContractMarker = "[[STRICT_STACK_CONTRACT]]";

    public sealed record StackContract(
        IReadOnlyList<string> Languages,
        IReadOnlyList<string> Frameworks,
        IReadOnlyList<string> ForbiddenFrameworks,
        string Layout);

    public static bool HasActiveContract(GenerationPlan plan, string? userRequest = null) =>
        plan.ApplicationDescription.Contains(ContractMarker, StringComparison.Ordinal)
        || Parse(userRequest) is not null;

    public static StackContract? Parse(string? userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            return null;

        var s = userRequest.ToLowerInvariant();
        var explicitIntent = Contains(s, "строго")
                             || Contains(s, "strict")
                             || Contains(s, "не подменять")
                             || Contains(s, "do not substitute")
                             || Contains(s, "не использовать")
                             || Contains(s, "only solidjs")
                             || Contains(s, "только solidjs")
                             || Contains(s, "только django")
                             || Contains(s, "only django");

        var hasBackend = Contains(s, "django") || Contains(s, "fastapi") || Contains(s, "flask")
                         || Contains(s, "spring boot") || Contains(s, "asp.net") || Contains(s, "nestjs")
                         || Contains(s, "laravel") || Contains(s, "express");
        var hasFrontend = Contains(s, "solidjs") || Contains(s, "react") || Contains(s, "vue")
                          || Contains(s, "angular") || Contains(s, "nuxt") || Contains(s, "svelte");

        if (!explicitIntent)
            return null;

        if (!(hasBackend && hasFrontend))
            return null;

        var languages = new List<string>();
        var frameworks = new List<string>();
        var forbidden = new List<string>();

        if (Contains(s, "python"))
            languages.Add("Python");
        if (Contains(s, "typescript"))
            languages.Add("TypeScript");
        if (Contains(s, "javascript") && !Contains(s, "typescript"))
            languages.Add("JavaScript");
        if (Contains(s, "java") && !Contains(s, "javascript"))
            languages.Add("Java");
        if (Contains(s, "c#") || Contains(s, "csharp"))
            languages.Add("C#");
        if (ContainsWholeWord(s, "go") || ContainsWholeWord(s, "golang"))
            languages.Add("Go");
        if (Contains(s, "rust"))
            languages.Add("Rust");
        if (Contains(s, "php"))
            languages.Add("PHP");

        if (Contains(s, "django"))
        {
            frameworks.Add("Django");
            if (Contains(s, "django rest") || Contains(s, "drf") || Contains(s, "rest framework"))
                frameworks.Add("Django REST Framework");
        }

        if (Contains(s, "fastapi"))
            frameworks.Add("FastAPI");
        if (Contains(s, "flask"))
            frameworks.Add("Flask");
        if (Contains(s, "spring boot") || Contains(s, "spring "))
            frameworks.Add("Spring Boot");
        if (Contains(s, "asp.net") || Contains(s, "aspnet"))
            frameworks.Add("ASP.NET Core");
        if ((Contains(s, "nestjs") || Contains(s, "nest.js"))
            && !IsForbiddenMention(s, "nestjs"))
            frameworks.Add("NestJS");
        if (Contains(s, "laravel"))
            frameworks.Add("Laravel");
        if (Contains(s, "express"))
            frameworks.Add("Express");
        if (Contains(s, "solidjs") || Contains(s, "solid js"))
            frameworks.Add("SolidJS");
        if (Contains(s, "vite"))
            frameworks.Add("Vite");
        if (Contains(s, "react") && !IsForbiddenMention(s, "react"))
            frameworks.Add("React");

        AddForbiddenFromNegationList(s, forbidden);

        if (IsForbiddenMention(s, "react"))
            forbidden.AddRange(new[] { "React", "Next.js", "Remix" });
        if (IsForbiddenMention(s, "vue") || Contains(s, "только solidjs") || Contains(s, "only solidjs"))
            forbidden.AddRange(new[] { "Vue", "Nuxt", "Angular" });
        if (IsForbiddenMention(s, "fastapi") || Contains(s, "только django") || Contains(s, "only django"))
            forbidden.AddRange(new[] { "FastAPI", "Flask", "Express" });
        if (IsForbiddenMention(s, "nestjs"))
            forbidden.Add("NestJS");
        if (IsForbiddenMention(s, "express"))
            forbidden.Add("Express");

        if (languages.Count == 0 && frameworks.Count == 0)
            return null;

        var layout = Contains(s, "backend/") && Contains(s, "frontend/")
            ? "backend/ + frontend/"
            : "app/";

        return new StackContract(languages, frameworks, forbidden.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), layout);
    }

    public static GenerationPlan Enforce(GenerationPlan plan, string? userRequest)
    {
        var contract = Parse(userRequest) ?? ParseFromPlan(plan);
        if (contract is null)
            return plan;

        var languages = contract.Languages.Count > 0
            ? contract.Languages.ToList()
            : plan.TechStack.Languages.ToList();

        var frameworks = contract.Frameworks.Count > 0
            ? contract.Frameworks.ToList()
            : plan.TechStack.Frameworks.ToList();

        if (contract.Frameworks.Count == 0)
            frameworks = MergeAllowedFrameworksFromPlan(frameworks, plan, contract.ForbiddenFrameworks);

        frameworks = frameworks
            .Where(f => !contract.ForbiddenFrameworks.Any(b =>
                f.Contains(b, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (languages.Count == 0)
            languages = plan.TechStack.Languages.ToList();
        if (frameworks.Count == 0)
            frameworks = plan.TechStack.Frameworks.ToList();

        var techStack = new TechStack(
            languages,
            frameworks,
            plan.TechStack.Databases.Count > 0 ? plan.TechStack.Databases.ToList() : new List<string> { "SQLite" },
            plan.TechStack.Infrastructure.Count > 0 ? plan.TechStack.Infrastructure.ToList() : new List<string> { "Docker" },
            $"Strict user contract: {string.Join(", ", languages)} + {string.Join(", ", frameworks)}");

        var description = EnsureContractBlock(plan.ApplicationDescription, contract);

        var runtime = plan.RuntimeImage;
        if (languages.Any(l => l.Contains("python", StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(runtime) || runtime.Contains("node", StringComparison.OrdinalIgnoreCase)))
            runtime = "python:3.12-slim";

        var build = plan.BuildCommands.ToList();
        if (contract.Layout.Contains("backend/", StringComparison.Ordinal)
            && languages.Any(l => l.Contains("python", StringComparison.OrdinalIgnoreCase))
            && (build.Count == 0 || build.All(c => c.Contains("npm", StringComparison.OrdinalIgnoreCase))))
        {
            build = new List<string>
            {
                "cd backend && python -m pip install -r requirements.txt && python manage.py check",
                "cd frontend && npm ci && npm run build"
            };
        }

        var test = plan.TestCommands.ToList();
        if (contract.Layout.Contains("backend/", StringComparison.Ordinal)
            && languages.Any(l => l.Contains("python", StringComparison.OrdinalIgnoreCase))
            && test.Count == 0)
        {
            test = new List<string>
            {
                "cd backend && python manage.py test",
                "cd frontend && npm test -- --watch=false"
            };
        }

        return new GenerationPlan(
            plan.ApplicationName,
            description,
            techStack,
            plan.Phases,
            plan.RequiredAgents,
            runtime,
            build,
            test,
            plan.MaxIterations);
    }

    private static StackContract? ParseFromPlan(GenerationPlan plan)
    {
        if (!plan.ApplicationDescription.Contains(ContractMarker, StringComparison.Ordinal))
            return null;

        return new StackContract(
            plan.TechStack.Languages.ToList(),
            plan.TechStack.Frameworks.ToList(),
            Array.Empty<string>(),
            plan.ApplicationDescription.Contains("backend/", StringComparison.OrdinalIgnoreCase)
                ? "backend/ + frontend/"
                : "app/");
    }

    private static List<string> MergeAllowedFrameworksFromPlan(
        List<string> frameworks,
        GenerationPlan plan,
        IReadOnlyList<string> forbidden)
    {
        foreach (var fw in plan.TechStack.Frameworks)
        {
            if (forbidden.Any(b => fw.Contains(b, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (!frameworks.Any(f => f.Equals(fw, StringComparison.OrdinalIgnoreCase)))
                frameworks.Add(fw);
        }

        return frameworks;
    }

    private static string EnsureContractBlock(string description, StackContract contract)
    {
        if (description.Contains(ContractMarker, StringComparison.Ordinal))
            return description;

        return description +
               $"\n\n{ContractMarker}\n" +
               $"languages={string.Join(",", contract.Languages)}\n" +
               $"frameworks={string.Join(",", contract.Frameworks)}\n" +
               $"forbidden={string.Join(",", contract.ForbiddenFrameworks)}\n" +
               $"layout={contract.Layout}\n" +
               "reject_single_stack_substitution=true\n";
    }

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsWholeWord(string text, string word)
    {
        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(text))
            return false;

        for (var i = 0; i <= text.Length - word.Length; i++)
        {
            if (string.Compare(text, i, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
                continue;

            var leftOk = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            var end = i + word.Length;
            var rightOk = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (leftOk && rightOk)
                return true;
        }

        return false;
    }

    private static bool IsForbiddenMention(string haystack, string term) =>
        Contains(haystack, "не использовать " + term)
        || Contains(haystack, "не использовать" + term)
        || Contains(haystack, "not use " + term)
        || Contains(haystack, "without " + term)
        || Contains(haystack, "без " + term);

    private static void AddForbiddenFromNegationList(string haystack, List<string> forbidden)
    {
        var negStart = haystack.IndexOf("не использовать", StringComparison.OrdinalIgnoreCase);
        if (negStart < 0)
            negStart = haystack.IndexOf("not use", StringComparison.OrdinalIgnoreCase);
        if (negStart < 0)
            return;

        var segment = haystack[negStart..Math.Min(negStart + 160, haystack.Length)];
        if (segment.Contains("react", StringComparison.OrdinalIgnoreCase))
            forbidden.AddRange(new[] { "React", "Next.js", "Remix" });
        if (segment.Contains("vue", StringComparison.OrdinalIgnoreCase))
            forbidden.AddRange(new[] { "Vue", "Nuxt", "Angular" });
        if (segment.Contains("nestjs", StringComparison.OrdinalIgnoreCase)
            || segment.Contains("nest.js", StringComparison.OrdinalIgnoreCase))
            forbidden.Add("NestJS");
        if (segment.Contains("express", StringComparison.OrdinalIgnoreCase))
            forbidden.Add("Express");
        if (segment.Contains("fastapi", StringComparison.OrdinalIgnoreCase))
            forbidden.Add("FastAPI");
    }
}
