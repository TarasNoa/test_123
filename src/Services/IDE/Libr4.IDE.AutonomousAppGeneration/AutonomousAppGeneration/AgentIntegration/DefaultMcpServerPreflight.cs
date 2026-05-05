using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class DefaultMcpServerPreflight : IMcpServerPreflight
{
    private readonly IOptions<McpExecutionOptions> _options;
    private readonly ILogger<DefaultMcpServerPreflight> _logger;

    public DefaultMcpServerPreflight(
        IOptions<McpExecutionOptions> options,
        ILogger<DefaultMcpServerPreflight> logger)
    {
        _options = options;
        _logger = logger;
    }

    public McpServerPreflightResult CheckServerAvailability(string profileKey)
    {
        var opt = _options.Value;
        
        if (!opt.ServerProfiles.TryGetValue(profileKey, out var profile))
        {
            _logger.LogWarning("MCP server profile '{ProfileKey}' not found in configuration", profileKey);
            return McpServerPreflightResult.ServerMissing($"profile:{profileKey}");
        }

        var fileName = profile.FileName;
        
        // Check if the executable exists
        if (!File.Exists(fileName))
        {
            // Try to find it in PATH
            var inPath = TryFindInPath(fileName);
            if (inPath == null)
            {
                _logger.LogWarning("MCP server executable '{FileName}' not found (profile: {ProfileKey})", fileName, profileKey);
                return McpServerPreflightResult.ServerMissing(fileName);
            }
            fileName = inPath;
        }

        // Try to execute with --version or similar to verify it's runnable
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start MCP server executable '{FileName}' (profile: {ProfileKey})", fileName, profileKey);
                return McpServerPreflightResult.ServerUnreachable(fileName, new InvalidOperationException("Process.Start returned null"));
            }

            if (!process.WaitForExit(5000))
            {
                process.Kill();
                _logger.LogWarning("MCP server executable '{FileName}' did not respond to --version within timeout (profile: {ProfileKey})", fileName, profileKey);
                // Still consider it available - it might just not support --version
                return McpServerPreflightResult.Available();
            }

            if (process.ExitCode != 0)
            {
                _logger.LogDebug("MCP server executable '{FileName}' returned exit code {ExitCode} for --version (profile: {ProfileKey}) - this may be normal", 
                    fileName, process.ExitCode, profileKey);
                // Still consider it available - --version might not be supported
            }

            return McpServerPreflightResult.Available();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify MCP server executable '{FileName}' (profile: {ProfileKey})", fileName, profileKey);
            return McpServerPreflightResult.ServerUnreachable(fileName, ex);
        }
    }

    private static string? TryFindInPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var extensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? new[] { "" };
        var directories = pathEnv.Split(';');

        foreach (var dir in directories)
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(dir, fileName + ext);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }
}
