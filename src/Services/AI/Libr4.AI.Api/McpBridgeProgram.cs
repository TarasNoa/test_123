/*
using Libr4.AI.Infrastructure.MCP;
using Serilog;

// Standalone MCP Bridge Server entry point
// Usage: dotnet run --project Libr4.AI.Api -- MCP_BRIDGE --db path/to/db.db --channel path/to/channel.txt

if (args.Length > 0 && args[0] == "MCP_BRIDGE")
{
    var dbPath = GetArgValue(args, "--db", "agent_bridge.db");
    var channelPath = GetArgValue(args, "--channel");
    var lockStaleSeconds = int.Parse(GetArgValue(args, "--lock-stale-seconds", "0"));

    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Information()
        .CreateLogger();

    try
    {
        using var server = new AgentBridgeMcpServer(dbPath, channelPath, lockStaleSeconds);
        Console.Error.WriteLine($"Agent Bridge MCP Server starting...");
        Console.Error.WriteLine($"  Database: {Path.GetFullPath(dbPath)}");
        Console.Error.WriteLine($"  Channel: {channelPath ?? "disabled"}");
        Console.Error.WriteLine($"  Lock stale seconds: {lockStaleSeconds}");
        
        await server.RunAsync();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Server crashed");
        Environment.Exit(1);
    }
    finally
    {
        Log.CloseAndFlush();
    }
}
else
{
    // Run normal ASP.NET Core API
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddControllers();
    var app = builder.Build();
    app.MapControllers();
    app.Run();
}

static string GetArgValue(string[] args, string key, string defaultValue = "")
{
    var index = Array.IndexOf(args, key);
    return index >= 0 && index < args.Length - 1 ? args[index + 1] : defaultValue;
}
*/
