using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Emits WebApplicationFactory-based HTTP integration tests for repo-bootstrap ASP.NET apps.
/// </summary>
public static class RepoBootstrapHttpTestArtifacts
{
    public static int Apply(IList<GeneratedFile> files, string testsRoot)
    {
        var programPath = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p => p.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase)
                                 || p.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(programPath))
            return 0;

        var changed = 0;
        changed += EnsureProgramTestingEntryPoint(files, programPath);
        changed += UpsertTestProjectPackages(files, testsRoot);
        changed += UpsertHttpTests(files, testsRoot);
        return changed;
    }

    private static int EnsureProgramTestingEntryPoint(IList<GeneratedFile> files, string programPath)
    {
        var idx = FindIndex(files, programPath);
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        if (content.Contains("partial class Program", StringComparison.Ordinal))
            return 0;

        var updated = content.TrimEnd() + "\n\npublic partial class Program { }\n";
        files[idx] = new GeneratedFile(programPath, files[idx].Language, updated);
        return 1;
    }

    private static int UpsertTestProjectPackages(IList<GeneratedFile> files, string testsRoot)
    {
        var csprojPath = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p => p.StartsWith(testsRoot + "/", StringComparison.OrdinalIgnoreCase)
                                 && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        if (csprojPath is null)
            return 0;

        var idx = FindIndex(files, csprojPath);
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        if (content.Contains("Microsoft.AspNetCore.Mvc.Testing", StringComparison.Ordinal))
            return 0;

        const string packageLine =
            "    <PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" Version=\"8.0.8\" />\n";
        var insertAt = content.LastIndexOf("</ItemGroup>", StringComparison.Ordinal);
        if (insertAt < 0)
            return 0;

        var updated = content.Insert(insertAt, packageLine);
        files[idx] = new GeneratedFile(csprojPath, "xml", updated);
        return 1;
    }

    private static int UpsertHttpTests(IList<GeneratedFile> files, string testsRoot)
    {
        const string relativePath = "KanbanAuthHttpTests.cs";
        var fullPath = $"{testsRoot.TrimEnd('/')}/{relativePath}";
        var content = """
            using System.Net;
            using System.Net.Http.Headers;
            using System.Net.Http.Json;
            using System.Text.Json;
            using Microsoft.AspNetCore.Mvc.Testing;
            using Xunit;

            /// <summary>
            /// HTTP integration tests for JWT auth and kanban board (repo-bootstrap contract).
            /// </summary>
            public sealed class KanbanAuthHttpTests : IClassFixture<WebApplicationFactory<Program>>
            {
                private readonly HttpClient _client;

                public KanbanAuthHttpTests(WebApplicationFactory<Program> factory)
                {
                    _client = factory.CreateClient();
                }

                [Fact]
                public async Task TokenEndpoint_ShouldReturnJwtAccessToken()
                {
                    var response = await _client.PostAsync("/api/auth/token", content: null);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                    Assert.True(json.TryGetProperty("access_token", out var token));
                    Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
                }

                [Fact]
                public async Task KanbanBoard_ShouldRejectAnonymous_AndReturnBoardWithBearerToken()
                {
                    var anonymous = await _client.GetAsync("/api/kanban/board");
                    Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

                    var tokenResponse = await _client.PostAsync("/api/auth/token", content: null);
                    tokenResponse.EnsureSuccessStatusCode();
                    var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
                    var accessToken = tokenJson.GetProperty("access_token").GetString();
                    Assert.False(string.IsNullOrWhiteSpace(accessToken));

                    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/kanban/board");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var boardResponse = await _client.SendAsync(request);
                    boardResponse.EnsureSuccessStatusCode();

                    var body = await boardResponse.Content.ReadAsStringAsync();
                    Assert.Contains("columns", body, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("tasks", body, StringComparison.OrdinalIgnoreCase);
                }

                [Fact]
                public async Task KanbanTransition_ShouldMoveTask_WhenAuthorized()
                {
                    var tokenResponse = await _client.PostAsync("/api/auth/token", content: null);
                    tokenResponse.EnsureSuccessStatusCode();
                    var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
                    var accessToken = tokenJson.GetProperty("access_token").GetString()!;

                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        "/api/kanban/tasks/task-1/transition?targetColumn=in_progress");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var response = await _client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var body = await response.Content.ReadAsStringAsync();
                    Assert.Contains("task-1", body, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("in_progress", body, StringComparison.OrdinalIgnoreCase);
                }
            }
            """;

        return UpsertFile(files, fullPath, "csharp", content.TrimStart());
    }

    private static int FindIndex(IList<GeneratedFile> files, string path)
    {
        for (var i = 0; i < files.Count; i++)
        {
            if (files[i].RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static int UpsertFile(IList<GeneratedFile> files, string path, string language, string content)
    {
        var idx = FindIndex(files, path);
        if (idx < 0)
        {
            files.Add(new GeneratedFile(path, language, content));
            return 1;
        }

        if (string.Equals(files[idx].Content, content, StringComparison.Ordinal))
            return 0;

        files[idx] = new GeneratedFile(path, language, content);
        return 1;
    }
}
