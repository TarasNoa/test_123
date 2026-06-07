using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

/// <summary>
/// Known scaffold files that should not consume LLM turns (manage.py, wsgi, Docker, etc.).
/// </summary>
public static class BoilerplateRegistry
{
    public static bool HasBoilerplate(string relativePath) =>
        TryGetContent(relativePath, null, null) is not null;

    public static AgentToolCall? TryCreateWriteCall(
        string relativePath,
        IList<GeneratedFile>? workingFiles,
        GenerationPlan? plan)
    {
        var content = TryGetContent(relativePath, workingFiles, plan);
        if (content is null)
            return null;

        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(relativePath);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { path, content }));
        return new AgentToolCall("write_file", doc.RootElement.Clone());
    }

    public static string? TryGetContent(
        string relativePath,
        IList<GeneratedFile>? workingFiles,
        GenerationPlan? plan)
    {
        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(relativePath);
        if (path.Length == 0)
            return null;

        var fileName = Path.GetFileName(path).ToLowerInvariant();
        var settingsModule = InferDjangoSettingsModule(workingFiles) ?? "app.settings";
        var appName = plan?.ApplicationName ?? "app";

        return fileName switch
        {
            "manage.py" when path.EndsWith("manage.py", StringComparison.OrdinalIgnoreCase)
                => BuildDjangoManagePy(settingsModule),
            "wsgi.py" when path.EndsWith("wsgi.py", StringComparison.OrdinalIgnoreCase)
                => BuildDjangoWsgi(settingsModule),
            "asgi.py" when path.EndsWith("asgi.py", StringComparison.OrdinalIgnoreCase)
                => BuildDjangoAsgi(settingsModule),
            "__init__.py" when path.EndsWith("__init__.py", StringComparison.OrdinalIgnoreCase)
                => "\"\"\"Package marker.\"\"\"\n",
            ".gitignore" => BuildGitIgnore(),
            "dockerfile" when path.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase)
                              || path.Equals("backend/Dockerfile", StringComparison.OrdinalIgnoreCase)
                => BuildDockerfile(plan),
            "docker-compose.yml" or "docker-compose.yaml"
                when path.Equals("docker-compose.yml", StringComparison.OrdinalIgnoreCase)
                     || path.Equals("docker-compose.yaml", StringComparison.OrdinalIgnoreCase)
                => BuildDockerCompose(appName, plan),
            _ => null
        };
    }

    public static string? InferDjangoSettingsModule(IList<GeneratedFile>? workingFiles)
    {
        if (workingFiles is null || workingFiles.Count == 0)
            return null;

        var settings = workingFiles
            .Select(f => FixerPatchScopePolicy.NormalizePatchRelativePath(f.RelativePath))
            .Where(p => p.EndsWith("/settings.py", StringComparison.OrdinalIgnoreCase)
                        || p.Equals("settings.py", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Contains("backend/", StringComparison.OrdinalIgnoreCase))
            .ThenBy(p => p.Length)
            .FirstOrDefault();

        if (settings is null)
            return null;

        var dir = Path.GetDirectoryName(settings)?.Replace('\\', '/') ?? string.Empty;
        if (string.IsNullOrEmpty(dir))
            return "settings";

        var modulePath = dir.Replace('/', '.');
        return $"{modulePath}.settings";
    }

    private static bool IsDjangoPlan(GenerationPlan? plan) =>
        plan?.TechStack.Frameworks.Any(f => f.Contains("django", StringComparison.OrdinalIgnoreCase)) == true;

    private static string BuildDjangoManagePy(string settingsModule) =>
        $"""
        #!/usr/bin/env python
        import os
        import sys

        def main():
            os.environ.setdefault('DJANGO_SETTINGS_MODULE', '{settingsModule}')
            from django.core.management import execute_from_command_line
            execute_from_command_line(sys.argv)

        if __name__ == '__main__':
            main()
        """;

    private static string BuildDjangoWsgi(string settingsModule) =>
        $"""
        import os
        from django.core.wsgi import get_wsgi_application

        os.environ.setdefault('DJANGO_SETTINGS_MODULE', '{settingsModule}')
        application = get_wsgi_application()
        """;

    private static string BuildDjangoAsgi(string settingsModule) =>
        $"""
        import os
        from django.core.asgi import get_asgi_application

        os.environ.setdefault('DJANGO_SETTINGS_MODULE', '{settingsModule}')
        application = get_asgi_application()
        """;

    private static string BuildGitIgnore() =>
        """
        __pycache__/
        *.py[cod]
        .venv/
        venv/
        .env
        .env.*
        node_modules/
        dist/
        build/
        .pytest_cache/
        .mypy_cache/
        *.sqlite3
        db.sqlite3
        .DS_Store
        """;

    private static string BuildDockerfile(GenerationPlan? plan)
    {
        if (IsDjangoPlan(plan))
        {
            return """
                FROM python:3.12-slim
                WORKDIR /app
                COPY backend/requirements.txt .
                RUN pip install --no-cache-dir -r requirements.txt
                COPY backend/ .
                ENV DJANGO_SETTINGS_MODULE=app.settings
                EXPOSE 8000
                CMD ["python", "manage.py", "runserver", "0.0.0.0:8000"]
                """;
        }

        return """
            FROM node:20-slim
            WORKDIR /app
            COPY package*.json ./
            RUN npm ci
            COPY . .
            EXPOSE 3000
            CMD ["npm", "start"]
            """;
    }

    private static string BuildDockerCompose(string appName, GenerationPlan? plan)
    {
        if (IsDjangoPlan(plan))
        {
            return """
                version: '3.9'
                services:
                  backend:
                    build:
                      context: .
                      dockerfile: backend/Dockerfile
                    ports:
                      - "8000:8000"
                    environment:
                      - DJANGO_SETTINGS_MODULE=app.settings
                      - OPENAI_API_KEY=${OPENAI_API_KEY}
                  frontend:
                    image: node:20-slim
                    working_dir: /app
                    volumes:
                      - ./frontend:/app
                    command: sh -c "npm install && npm run dev -- --host 0.0.0.0 --port 5173"
                    ports:
                      - "5173:5173"
                """;
        }

        return "version: '3.9'\n" +
               "services:\n" +
               "  app:\n" +
               "    build: .\n" +
               "    ports:\n" +
               "      - \"8000:8000\"\n" +
               $"    environment:\n      - APP_NAME={appName}\n";
    }
}
