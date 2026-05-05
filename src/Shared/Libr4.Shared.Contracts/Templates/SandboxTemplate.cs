namespace Libr4.Shared.Contracts.Templates;

/// <summary>
/// Sandbox template definition for code generation patterns.
/// Based on Fragments by E2B.
/// </summary>
public record SandboxTemplate
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the template.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// List of libraries/dependencies included in the template.
    /// </summary>
    public List<string> Libraries { get; init; } = new();

    /// <summary>
    /// Entry point file path (e.g., "app.py", "pages/index.tsx").
    /// </summary>
    public string? EntryFile { get; init; }

    /// <summary>
    /// Instructions for the LLM about this template.
    /// </summary>
    public string Instructions { get; init; } = string.Empty;

    /// <summary>
    /// Port number for web applications (null for non-web).
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// Programming language of the template.
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// Framework used (if any).
    /// </summary>
    public string? Framework { get; init; }

    /// <summary>
    /// Whether this template supports multi-modal input (images).
    /// </summary>
    public bool SupportsMultiModal { get; init; }

    /// <summary>
    /// Category of the template (e.g., "web", "data", "ml").
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Additional metadata about the template.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Registry for sandbox templates.
/// </summary>
public interface ISandboxTemplateRegistry
{
    /// <summary>
    /// Registers a sandbox template.
    /// </summary>
    void RegisterTemplate(SandboxTemplate template);

    /// <summary>
    /// Unregisters a template by ID.
    /// </summary>
    void UnregisterTemplate(string templateId);

    /// <summary>
    /// Gets a template by ID.
    /// </summary>
    SandboxTemplate? GetTemplate(string templateId);

    /// <summary>
    /// Gets all templates.
    /// </summary>
    IReadOnlyList<SandboxTemplate> GetAllTemplates();

    /// <summary>
    /// Gets templates by category.
    /// </summary>
    IReadOnlyList<SandboxTemplate> GetTemplatesByCategory(string category);

    /// <summary>
    /// Gets templates by language.
    /// </summary>
    IReadOnlyList<SandboxTemplate> GetTemplatesByLanguage(string language);

    /// <summary>
    /// Converts templates to prompt format for LLM.
    /// </summary>
    string TemplatesToPrompt(IReadOnlyList<SandboxTemplate> templates);
}

/// <summary>
/// In-memory implementation of sandbox template registry.
/// </summary>
public class InMemorySandboxTemplateRegistry : ISandboxTemplateRegistry
{
    private readonly Dictionary<string, SandboxTemplate> _templates = new();

    public InMemorySandboxTemplateRegistry()
    {
        RegisterBuiltInTemplates();
    }

    private void RegisterBuiltInTemplates()
    {
        // Python Data Analyst
        RegisterTemplate(new SandboxTemplate
        {
            Id = "python-data-analyst",
            Name = "Python Data Analyst",
            Libraries = new List<string>
            {
                "python", "jupyter", "numpy", "pandas", 
                "matplotlib", "seaborn", "plotly"
            },
            EntryFile = "script.py",
            Instructions = "Runs code as a Jupyter notebook cell. Strong data analysis angle. Can use complex visualisation to explain results.",
            Port = null,
            Language = "python",
            Framework = null,
            SupportsMultiModal = false,
            Category = "data"
        });

        // Next.js Developer
        RegisterTemplate(new SandboxTemplate
        {
            Id = "nextjs-developer",
            Name = "Next.js Developer",
            Libraries = new List<string>
            {
                "nextjs@14.2.5", "typescript", "@types/node", 
                "@types/react", "@types/react-dom", "postcss", 
                "tailwindcss", "shadcn"
            },
            EntryFile = "pages/index.tsx",
            Instructions = "A Next.js 13+ app that reloads automatically. Using the pages router.",
            Port = 3000,
            Language = "typescript",
            Framework = "nextjs",
            SupportsMultiModal = true,
            Category = "web"
        });

        // Vue.js Developer
        RegisterTemplate(new SandboxTemplate
        {
            Id = "vue-developer",
            Name = "Vue.js Developer",
            Libraries = new List<string>
            {
                "vue@latest", "nuxt@3.13.0", "tailwindcss"
            },
            EntryFile = "app/app.vue",
            Instructions = "A Vue.js 3+ app that reloads automatically. Only when asked specifically for a Vue app.",
            Port = 3000,
            Language = "typescript",
            Framework = "vue",
            SupportsMultiModal = true,
            Category = "web"
        });

        // Streamlit Developer
        RegisterTemplate(new SandboxTemplate
        {
            Id = "streamlit-developer",
            Name = "Streamlit Developer",
            Libraries = new List<string>
            {
                "streamlit", "pandas", "numpy", "matplotlib", 
                "requests", "seaborn", "plotly"
            },
            EntryFile = "app.py",
            Instructions = "A streamlit app that reloads automatically.",
            Port = 8501,
            Language = "python",
            Framework = "streamlit",
            SupportsMultiModal = false,
            Category = "data"
        });

        // Gradio Developer
        RegisterTemplate(new SandboxTemplate
        {
            Id = "gradio-developer",
            Name = "Gradio Developer",
            Libraries = new List<string>
            {
                "gradio", "pandas", "numpy", "matplotlib", 
                "requests", "seaborn", "plotly"
            },
            EntryFile = "app.py",
            Instructions = "A gradio app. Gradio Blocks/Interface should be called demo.",
            Port = 7860,
            Language = "python",
            Framework = "gradio",
            SupportsMultiModal = false,
            Category = "ml"
        });

        // C# Web API
        RegisterTemplate(new SandboxTemplate
        {
            Id = "dotnet-webapi",
            Name = ".NET Web API",
            Libraries = new List<string>
            {
                "Microsoft.AspNetCore.OpenApi", "Swashbuckle.AspNetCore",
                "Microsoft.EntityFrameworkCore", "Npgsql.EntityFrameworkCore.PostgreSQL"
            },
            EntryFile = "Program.cs",
            Instructions = "A .NET 8 Web API with Swagger documentation. Uses minimal APIs pattern.",
            Port = 5000,
            Language = "csharp",
            Framework = "aspnetcore",
            SupportsMultiModal = false,
            Category = "web"
        });

        // React Developer
        RegisterTemplate(new SandboxTemplate
        {
            Id = "react-developer",
            Name = "React Developer",
            Libraries = new List<string>
            {
                "react", "react-dom", "vite", "typescript", 
                "tailwindcss", "lucide-react"
            },
            EntryFile = "src/App.tsx",
            Instructions = "A React 18+ app with Vite. Uses modern hooks and TypeScript.",
            Port = 5173,
            Language = "typescript",
            Framework = "react",
            SupportsMultiModal = true,
            Category = "web"
        });
    }

    public void RegisterTemplate(SandboxTemplate template)
    {
        if (string.IsNullOrEmpty(template.Id))
        {
            throw new ArgumentException("Template ID cannot be null or empty", nameof(template));
        }

        _templates[template.Id] = template;
    }

    public void UnregisterTemplate(string templateId)
    {
        _templates.Remove(templateId);
    }

    public SandboxTemplate? GetTemplate(string templateId)
    {
        _templates.TryGetValue(templateId, out var template);
        return template;
    }

    public IReadOnlyList<SandboxTemplate> GetAllTemplates()
    {
        return _templates.Values.ToList().AsReadOnly();
    }

    public IReadOnlyList<SandboxTemplate> GetTemplatesByCategory(string category)
    {
        return _templates.Values
            .Where(t => t.Category == category)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<SandboxTemplate> GetTemplatesByLanguage(string language)
    {
        return _templates.Values
            .Where(t => t.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public string TemplatesToPrompt(IReadOnlyList<SandboxTemplate> templates)
    {
        if (templates == null || templates.Count == 0)
            return string.Empty;

        var lines = new List<string>();
        for (int i = 0; i < templates.Count; i++)
        {
            var template = templates[i];
            var line = $"{i + 1}. {template.Id}: \"{template.Instructions}\". " +
                       $"File: {template.EntryFile ?? "none"}. " +
                       $"Dependencies installed: {string.Join(", ", template.Libraries)}. " +
                       $"Port: {template.Port?.ToString() ?? "none"}. " +
                       $"Language: {template.Language}. " +
                       $"Framework: {template.Framework ?? "none"}.";
            lines.Add(line);
        }

        return string.Join('\n', lines);
    }
}
