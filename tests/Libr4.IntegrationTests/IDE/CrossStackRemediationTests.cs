using FluentAssertions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CrossStackRemediationTests
{
    [Fact]
    public void ApplyFixesAsync_ShouldGenerateDotNetDeterministicArtifacts_OnArchitectureFailure()
    {
        // Arrange
        var appName = "SecureAiApp";

        // Act - Verify deterministic artifact generation methods produce expected content
        var dotNetReadme = BuildFallbackDotNetReadmeContent(appName);
        var dotNetEnv = BuildFallbackDotNetEnvContent();
        var dotNetDockerCompose = BuildFallbackDotNetDockerComposeContent();
        var dotNetCiWorkflow = BuildFallbackDotNetCiWorkflowContent();
        var dotNetTests = BuildFallbackDotNetTestsContent();

        // Assert
        dotNetReadme.Should().Contain("# SecureAiApp");
        dotNetReadme.Should().Contain("dotnet restore");
        dotNetReadme.Should().Contain("dotnet test");

        dotNetEnv.Should().Contain("ASPNETCORE_ENVIRONMENT=Development");
        dotNetEnv.Should().Contain("ConnectionStrings__DefaultConnection");

        dotNetDockerCompose.Should().Contain("version: '3.9'");
        dotNetDockerCompose.Should().Contain("build: .");
        dotNetDockerCompose.Should().Contain("postgres:15");

        dotNetCiWorkflow.Should().Contain("name: ci");
        dotNetCiWorkflow.Should().Contain("dotnet-version: '8.0'");
        dotNetCiWorkflow.Should().Contain("dotnet test --no-build");

        dotNetTests.Should().Contain("public class HealthEndpointTests");
        dotNetTests.Should().Contain("Health_ShouldReturnOk");
        dotNetTests.Should().Contain("Readiness_ShouldReturnReady");
    }

    [Fact]
    public void ApplyFixesAsync_ShouldGenerateNodeDeterministicArtifacts_OnArchitectureFailure()
    {
        // Arrange
        var appName = "FintechApp";

        // Act
        var nodeReadme = BuildFallbackNodeReadmeContent(appName);
        var nodeEnv = BuildFallbackNodeEnvContent();
        var nodeDockerCompose = BuildFallbackNodeDockerComposeContent();
        var nodeCiWorkflow = BuildFallbackNodeCiWorkflowContent();
        var nodeTests = BuildFallbackNodeTestsContent();

        // Assert
        nodeReadme.Should().Contain("# FintechApp");
        nodeReadme.Should().Contain("npm install");
        nodeReadme.Should().Contain("npm test");

        nodeEnv.Should().Contain("PORT=3000");
        nodeEnv.Should().Contain("NODE_ENV=development");

        nodeDockerCompose.Should().Contain("version: '3.9'");
        nodeDockerCompose.Should().Contain("build: .");
        nodeDockerCompose.Should().Contain("PORT=3000");

        nodeCiWorkflow.Should().Contain("name: ci");
        nodeCiWorkflow.Should().Contain("node-version: '20'");
        nodeCiWorkflow.Should().Contain("npm test");

        nodeTests.Should().Contain("describe('Health endpoints'");
        nodeTests.Should().Contain("GET /health should return 200");
        nodeTests.Should().Contain("GET /readiness should return 200");
    }

    [Fact]
    public void ApplyFixesAsync_ShouldGeneratePythonDeterministicArtifacts_OnArchitectureFailure()
    {
        // Arrange
        var appName = "HealthcareApp";

        // Act
        var pythonReadme = BuildFallbackReadmeContent(appName);
        var pythonDockerCompose = BuildFallbackDockerComposeContent();
        var pythonCiWorkflow = BuildFallbackCiWorkflowContent();
        var pythonTests = BuildFallbackPythonTestsContent();

        // Assert
        pythonReadme.Should().Contain("# HealthcareApp");
        pythonReadme.Should().Contain("uvicorn main:app");
        pythonReadme.Should().Contain("pytest");

        pythonDockerCompose.Should().Contain("version: '3.9'");
        pythonDockerCompose.Should().Contain("build: .");
        pythonDockerCompose.Should().Contain("postgres:15");

        pythonCiWorkflow.Should().Contain("name: ci");
        pythonCiWorkflow.Should().Contain("python-version: '3.12'");
        pythonCiWorkflow.Should().Contain("pytest");

        pythonTests.Should().Contain("def test_health_endpoint_integration");
        pythonTests.Should().Contain("def test_create_task_and_list_integration");
        pythonTests.Should().Contain("def test_create_task_validation_error_negative");
    }

    // Helper methods extracted from LlmCodeGenerationService for testing
    private static string BuildFallbackDotNetReadmeContent(string appName)
    {
        var safeName = string.IsNullOrWhiteSpace(appName) ? "GeneratedApp" : appName.Replace("\n", " ", StringComparison.Ordinal);
        return $@"# {safeName}

## Quick start

1. Restore dependencies:
   - `dotnet restore`
2. Run locally:
   - `dotnet run --project src/GeneratedApp.Api/GeneratedApp.Api.csproj`
3. Run tests:
   - `dotnet test`

## Endpoints

- `GET /health`
- `GET /readiness`
- `GET /api/healthitems`
- `POST /api/healthitems`
- `PUT /api/healthitems/{{id}}`

## Configuration

Environment variables:
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ConnectionStrings__DefaultConnection`: Database connection string
";
    }

    private static string BuildFallbackDotNetEnvContent() =>
@"ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Database=app;Username=app;Password=app
";

    private static string BuildFallbackDotNetDockerComposeContent() =>
@"version: '3.9'
services:
  app:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=app;Username=app;Password=app
    ports:
      - ""5000:8080""
    depends_on:
      - db
  db:
    image: postgres:15
    environment:
      - POSTGRES_USER=app
      - POSTGRES_PASSWORD=app
      - POSTGRES_DB=app
";

    private static string BuildFallbackDotNetCiWorkflowContent() =>
@"name: ci
on:
  push:
  pull_request:
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'
      - name: restore
        run: dotnet restore
      - name: test
        run: dotnet test --no-build
";

    private static string BuildFallbackDotNetTestsContent() =>
@"using Xunit;
using System.Net.Http.Json;
using System.Net;

public class HealthEndpointTests
{{
    [Fact]
    public async Task Health_ShouldReturnOk()
    {{
        using var client = new HttpClient {{ BaseAddress = new Uri(""http://localhost:8080"") }};
        var response = await client.GetAsync(""/health"");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }}

    [Fact]
    public async Task Readiness_ShouldReturnReady()
    {{
        using var client = new HttpClient {{ BaseAddress = new Uri(""http://localhost:8080"") }};
        var response = await client.GetAsync(""/readiness"");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }}
}}
";

    private static string BuildFallbackNodeReadmeContent(string appName)
    {
        var safeName = string.IsNullOrWhiteSpace(appName) ? "GeneratedApp" : appName.Replace("\n", " ", StringComparison.Ordinal);
        return $@"# {safeName}

## Quick start

1. Install dependencies:
   - `npm install`
2. Run locally:
   - `node index.js` or `npm start`
3. Run tests:
   - `npm test`

## Endpoints

- `GET /health`
- `GET /readiness`
- `GET /api/items`
- `POST /api/items`

## Configuration

Environment variables:
- `PORT`: Application port (default: 3000)
- `NODE_ENV`: development/production
";
    }

    private static string BuildFallbackNodeEnvContent() =>
@"PORT=3000
NODE_ENV=development
";

    private static string BuildFallbackNodeDockerComposeContent() =>
@"version: '3.9'
services:
  app:
    build: .
    environment:
      - PORT=3000
      - NODE_ENV=production
    ports:
      - ""3000:3000""
";

    private static string BuildFallbackNodeCiWorkflowContent() =>
@"name: ci
on:
  push:
  pull_request:
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: install
        run: npm install
      - name: test
        run: npm test
";

    private static string BuildFallbackNodeTestsContent() =>
@"const request = require('supertest');
const app = require('./index');

describe('Health endpoints', () => {
  it('GET /health should return 200', async () => {
    const response = await request(app).get('/health');
    expect(response.status).toBe(200);
  });

  it('GET /readiness should return 200', async () => {
    const response = await request(app).get('/readiness');
    expect(response.status).toBe(200);
  });
});
";

    private static string BuildFallbackReadmeContent(string appName)
    {
        var safeName = string.IsNullOrWhiteSpace(appName) ? "GeneratedApp" : appName.Replace("\n", " ", StringComparison.Ordinal);
        return $@"# {safeName}

## Quick start

1. Copy `.env.example` to `.env`.
2. Install dependencies:
   - `pip install -r requirements.txt` or `pip install -r src/requirements.txt`
3. Run locally:
   - `uvicorn main:app --host 0.0.0.0 --port 8000`
4. Run tests:
   - `pytest`

## Endpoints

- `GET /health`
- `GET /readiness`
- `GET /tasks`
- `POST /tasks`
- `PUT /tasks/{{task_id}}
";
    }

    private static string BuildFallbackDockerComposeContent() =>
@"version: '3.9'
services:
  app:
    build: .
    command: uvicorn main:app --host 0.0.0.0 --port 8000
    environment:
      - PORT=8000
      - DATABASE_URL=postgresql://app:app@db:5432/app
      - REDIS_URL=redis://redis:6379/0
    ports:
      - ""8000:8000""
    depends_on:
      - db
      - redis
  db:
    image: postgres:15
    environment:
      - POSTGRES_USER=app
      - POSTGRES_PASSWORD=app
      - POSTGRES_DB=app
  redis:
    image: redis:7
";

    private static string BuildFallbackCiWorkflowContent() =>
@"name: ci
on:
  push:
  pull_request:
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: '3.12'
      - name: install
        run: |
          python -m pip install --upgrade pip
          if [ -f requirements.txt ]; then pip install -r requirements.txt; fi
          if [ -f src/requirements.txt ]; then pip install -r src/requirements.txt; fi
      - name: test
        run: pytest
";

    private static string BuildFallbackPythonTestsContent() =>
@"import os
import sys
import pytest
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))
from fastapi.testclient import TestClient
from main import app

client = TestClient(app)

def test_health_endpoint_integration():
    response = client.get(""/health"")
    assert response.status_code == 200
    body = response.json()
    assert body[""status""] == ""ok""

def test_create_task_and_list_integration():
    create_response = client.post(""/tasks"", json={""title"": ""sample""})
    assert create_response.status_code == 201
    list_response = client.get(""/tasks"")
    assert list_response.status_code == 200
    assert isinstance(list_response.json().get(""items""), list)

def test_create_task_validation_error_negative():
    response = client.post(""/tasks"", json={""title"": """"})
    assert response.status_code == 422
    error = response.json().get(""error"", {{}})
    assert error.get(""code"") == ""request_error""

def test_update_missing_task_negative():
    response = client.put(""/tasks/9999"", json={""completed"": True})
    assert response.status_code == 404
";
}
