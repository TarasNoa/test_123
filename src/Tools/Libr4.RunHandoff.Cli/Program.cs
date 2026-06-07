using System.Net.Http.Headers;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Microsoft.Extensions.DependencyInjection;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
    {
        PrintHelp();
        return 0;
    }

    var command = args[0].ToLowerInvariant();
    var options = ParseOptions(args.Skip(1).ToArray());

    return command switch
    {
        "export" => await ExportAsync(options),
        "import" => await ImportAsync(options),
        "cleanup" => Cleanup(options),
        "delegation-run" => await DelegationRunAsync(options),
        _ => UnknownCommand(command)
    };
}

static async Task<int> ExportAsync(Dictionary<string, string> options)
{
    if (!options.TryGetValue("run-id", out var runIdRaw) || !Guid.TryParse(runIdRaw, out var runId))
    {
        Console.Error.WriteLine("export requires --run-id <guid>");
        return 2;
    }

    if (options.TryGetValue("api-base", out var apiBase))
        return await ExportViaApiAsync(apiBase, runId, options);

    await using var provider = RunHandoffCliBootstrap.Build(BuildCliOptions(options));
    var export = provider.GetRequiredService<IRunExportService>();
    var result = await export.ExportAsync(runId);
    if (result is null)
    {
        Console.Error.WriteLine($"Run {runId:D} not found under runs root");
        return 1;
    }

    WriteJson(new
    {
        result.RunId,
        result.ExportId,
        result.ContentSha256,
        result.ArtifactPath,
        result.BundleBytes,
        result.ExpiresAtUtc
    });

    if (options.TryGetValue("output", out var outputPath))
    {
        File.Copy(result.ArtifactPath, outputPath, overwrite: true);
        Console.Error.WriteLine($"Copied bundle to {outputPath}");
    }

    return 0;
}

static async Task<int> ExportViaApiAsync(string apiBase, Guid runId, Dictionary<string, string> options)
{
    using var client = CreateApiClient(apiBase);
    using var response = await client.PostAsync($"/api/v1/ide/app-generation/{runId:D}/export", null);
    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"API export failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return 1;
    }

    var payload = await response.Content.ReadAsStringAsync();
    Console.WriteLine(payload);

    if (options.TryGetValue("output", out var outputPath))
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var exportId = root.TryGetProperty("exportId", out var exportIdEl)
            ? exportIdEl.GetString()
            : root.TryGetProperty("ExportId", out var exportIdAlt) ? exportIdAlt.GetString() : null;
        if (string.IsNullOrWhiteSpace(exportId))
        {
            Console.Error.WriteLine("API response missing exportId; cannot download bundle");
            return 1;
        }

        using var download = await client.GetAsync($"/api/v1/ide/app-generation/{runId:D}/export/{exportId}/download");
        if (!download.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Download failed: {(int)download.StatusCode}");
            return 1;
        }

        await using var stream = await download.Content.ReadAsStreamAsync();
        await using var file = File.Create(outputPath);
        await stream.CopyToAsync(file);
        Console.Error.WriteLine($"Downloaded bundle to {outputPath}");
    }

    return 0;
}

static async Task<int> ImportAsync(Dictionary<string, string> options)
{
    if (!options.TryGetValue("bundle", out var bundlePath) || !File.Exists(bundlePath))
    {
        Console.Error.WriteLine("import requires --bundle <path-to.tar.gz>");
        return 2;
    }

    if (options.TryGetValue("api-base", out var apiBase))
        return await ImportViaApiAsync(apiBase, bundlePath);

    await using var provider = RunHandoffCliBootstrap.Build(BuildCliOptions(options));
    var import = provider.GetRequiredService<IRunImportService>();
    try
    {
        var result = await import.ImportBundleAsync(bundlePath);
        WriteJson(new
        {
            result.RunId,
            result.SourceRunId,
            result.BundleSha256,
            result.LastStepNumber,
            result.IdempotentReplay,
            result.ImportedAtUtc,
            result.ResumeHint
        });
        return 0;
    }
    catch (RunImportException ex)
    {
        Console.Error.WriteLine($"{ex.ErrorCode}: {ex.Message}");
        return 1;
    }
}

static async Task<int> ImportViaApiAsync(string apiBase, string bundlePath)
{
    using var client = CreateApiClient(apiBase);
    await using var stream = File.OpenRead(bundlePath);
    using var content = new MultipartFormDataContent();
    var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
    content.Add(fileContent, "file", Path.GetFileName(bundlePath));

    using var response = await client.PostAsync("/api/v1/ide/app-generation/import", content);
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"API import failed: {(int)response.StatusCode} {body}");
        return 1;
    }

    Console.WriteLine(body);
    return 0;
}

static int Cleanup(Dictionary<string, string> options)
{
    using var provider = RunHandoffCliBootstrap.Build(BuildCliOptions(options));
    var export = provider.GetRequiredService<IRunExportService>();
    var removed = export.PruneExpiredExports();
    WriteJson(new { removed });
    return 0;
}

static async Task<int> DelegationRunAsync(Dictionary<string, string> options)
{
    if (!options.TryGetValue("request", out var requestPath) || !File.Exists(requestPath))
    {
        Console.Error.WriteLine("delegation-run requires --request <worker.json>");
        return 2;
    }

    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(requestPath));
    var root = doc.RootElement;
    var runId = root.GetProperty("runId").GetGuid();
    var delegationId = root.GetProperty("delegationId").GetString() ?? "";
    var task = root.GetProperty("task").GetString() ?? "";
    var runsRoot = root.GetProperty("runsRoot").GetString() ?? ".logs/runs";
    var outputPath = root.GetProperty("outputPath").GetString() ?? "";

    await using var provider = DelegationWorkerCliBootstrap.Build(new DelegationWorkerCliOptions(runsRoot));
    var runner = provider.GetRequiredService<IDelegationExploreRunner>();
    var context = DelegationWorkerCliBootstrap.CreateStubContext(runId, runsRoot);
    var output = await runner.RunExploreAsync(task, context, CancellationToken.None);

    if (!string.IsNullOrWhiteSpace(outputPath))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, output);
    }

    Console.WriteLine(output);
    return 0;
}

static RunHandoffCliOptions BuildCliOptions(Dictionary<string, string> options) =>
    new(
        RunsRoot: options.GetValueOrDefault("runs-root", ".logs/runs"),
        ExportRoot: options.GetValueOrDefault("export-root", ".logs/run-exports"),
        SessionDbPath: options.GetValueOrDefault("session-db", ".logs/agent-sessions.db"),
        IdempotencyRoot: options.GetValueOrDefault("idempotency-root", ".logs/run-imports"),
        RetentionDays: int.TryParse(options.GetValueOrDefault("retention-days", "7"), out var days) ? days : 7);

static HttpClient CreateApiClient(string apiBase)
{
    var client = new HttpClient { BaseAddress = new Uri(apiBase.TrimEnd('/') + "/") };
    var token = Environment.GetEnvironmentVariable("LIBR4_ACCESS_TOKEN");
    if (!string.IsNullOrWhiteSpace(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var token = args[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
            continue;

        var key = token[2..];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[key] = args[++i];
            continue;
        }

        options[key] = "true";
    }

    return options;
}

static void WriteJson(object payload) =>
    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintHelp();
    return 2;
}

static void PrintHelp()
{
    Console.WriteLine("""
        libr4-run — portable run handoff CLI

        Usage:
          dotnet libr4-run.dll export --run-id <guid> [--runs-root .logs/runs] [--export-root .logs/run-exports] [--output bundle.tar.gz]
          dotnet libr4-run.dll export --run-id <guid> --api-base http://localhost:5199 [--output bundle.tar.gz]
          dotnet libr4-run.dll import --bundle <path.tar.gz> [--runs-root .logs/runs]
          dotnet libr4-run.dll import --bundle <path.tar.gz> --api-base http://localhost:5199
          dotnet libr4-run.dll cleanup [--export-root .logs/run-exports] [--retention-days 7]
          dotnet libr4-run.dll delegation-run --request <worker.json>

        Environment:
          LIBR4_ACCESS_TOKEN  Bearer token for --api-base mode
        """);
}
