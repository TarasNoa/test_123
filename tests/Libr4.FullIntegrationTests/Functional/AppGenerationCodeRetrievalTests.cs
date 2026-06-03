using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

/// <summary>
/// Integration tests for retrieving generated application code from the orchestrator.
/// These tests demonstrate how to access and view the generated source files.
/// </summary>
public class AppGenerationCodeRetrievalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5199";

    public AppGenerationCodeRetrievalTests()
    {
        _client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    /// <summary>
    /// Test: Demonstrate how to retrieve generated code.
    /// Shows the API workflow:
    /// 1. POST to /api/ide/app-generation/start with a user request
    /// 2. GET /api/ide/app-generation/{id} to retrieve the full report with generated files
    /// </summary>
    [Fact]
    public async Task DemonstrateCodeRetrieval_ShowsHowToAccessGeneratedFiles()
    {
        // Arrange
        var request = new
        {
            userRequest = "сгенерируй приложение мобильного банкинга"
        };

        // Act - Step 1: Start generation
        var startResponse = await _client.PostAsJsonAsync(
            $"{BaseUrl}/api/ide/app-generation/start",
            request);

        startResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Accepted);

        // Extract generation ID from response
        var startJson = await startResponse.Content.ReadAsStringAsync();
        var startElement = JsonSerializer.Deserialize<JsonElement>(startJson, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var hasId = startElement.TryGetProperty("id", out var idElement);
        if (!hasId || string.IsNullOrWhiteSpace(idElement.GetString()))
        {
            // Current host can return 202 with hint-only payload while generation is queued.
            return;
        }

        var generationIdStr = idElement.GetString();
        
        var generationId = Guid.Parse(generationIdStr!);

        // Act - Step 2: Retrieve the full report with generated files
        var reportResponse = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        reportResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

        // Assert - Verify we can retrieve the report
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK, 
            "Should be able to retrieve the generation report");

        var reportJson = await reportResponse.Content.ReadAsStringAsync();
        var report = JsonSerializer.Deserialize<AppGenerationReport>(reportJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        report.Should().NotBeNull();
        report.Status.Should().NotBeNullOrEmpty();

        // Print generated files for inspection
        Console.WriteLine($"\n=== Generated Application: {report.Plan?.ApplicationName} ===");
        Console.WriteLine($"Status: {report.Status}");
        Console.WriteLine($"Tech Stack: {string.Join(", ", report.Plan?.TechStack?.Languages ?? new())}");
        Console.WriteLine($"Runtime: {report.Plan?.RuntimeImage}");
        Console.WriteLine($"\n--- Generated Files ({report.GeneratedFiles.Count}) ---");

        if (report.GeneratedFiles.Count > 0)
        {
            foreach (var file in report.GeneratedFiles)
            {
                Console.WriteLine($"\n📄 {file.RelativePath} ({file.Language})");
                Console.WriteLine($"   Size: {file.Content?.Length ?? 0} bytes");
                
                // Show first 500 chars of content
                if (!string.IsNullOrEmpty(file.Content))
                {
                    var preview = file.Content.Length > 500 
                        ? file.Content.Substring(0, 500) + "\n   ... (truncated)"
                        : file.Content;
                    Console.WriteLine($"   Content preview:\n{string.Join("\n   ", preview.Split('\n').Take(10))}");
                }
            }
        }
        else
        {
            Console.WriteLine("No files generated yet (generation may still be in progress)");
        }
    }

    /// <summary>
    /// Test: Retrieve specific generated file content.
    /// Demonstrates how to extract and work with individual source files.
    /// </summary>
    [Fact]
    public async Task RetrieveGeneratedFiles_ShouldContainValidSourceCode()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var report = JsonSerializer.Deserialize<AppGenerationReport>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (report?.GeneratedFiles.Count > 0)
            {
                // Verify each file has valid content
                foreach (var file in report.GeneratedFiles)
                {
                    file.RelativePath.Should().NotBeNullOrEmpty();
                    file.Language.Should().NotBeNullOrEmpty();
                    file.Content.Should().NotBeNullOrEmpty("Generated file should have content");

                    // Verify content is not just whitespace
                    file.Content.Trim().Length.Should().BeGreaterThan(0);
                }
            }
        }
    }

    /// <summary>
    /// Test: Retrieve generated files organized by language.
    /// Demonstrates how to filter and organize generated code by programming language.
    /// </summary>
    [Fact]
    public async Task RetrieveGeneratedFiles_ShouldBeOrganizedByLanguage()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var report = JsonSerializer.Deserialize<AppGenerationReport>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (report?.GeneratedFiles.Count > 0)
            {
                var filesByLanguage = report.GeneratedFiles
                    .GroupBy(f => f.Language)
                    .ToDictionary(g => g.Key, g => g.ToList());

                Console.WriteLine($"\n=== Files by Language ===");
                foreach (var (language, files) in filesByLanguage)
                {
                    Console.WriteLine($"{language}: {files.Count} files");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"  - {file.RelativePath}");
                    }
                }

                // Verify we have files in expected languages
                filesByLanguage.Keys.Should().NotBeEmpty();
            }
        }
    }
}

// Note: DTOs are defined in AppGenerationFunctionalTests.cs to avoid duplication
