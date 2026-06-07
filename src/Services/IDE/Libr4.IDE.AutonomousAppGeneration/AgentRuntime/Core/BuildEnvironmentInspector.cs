using System.Diagnostics;
using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

/// <summary>Claude Code /doctor-style environment snapshot (host + optional workspace).</summary>
public static class BuildEnvironmentInspector
{
    public static string Inspect(string? workspaceHostPath = null)
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["os"] = Environment.OSVersion.ToString(),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["workspace"] = workspaceHostPath ?? "(none)",
            ["python"] = ProbeVersion("python", "--version"),
            ["pip"] = ProbePip(),
            ["node"] = ProbeVersion("node", "--version"),
            ["npm"] = ProbeVersion("npm", "--version"),
            ["docker"] = ProbeDocker(),
            ["dotnet"] = ProbeVersion("dotnet", "--version"),
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ProbePip()
    {
        var viaModule = ProbeVersion("python", "-m pip --version");
        if (!viaModule.StartsWith("missing:", StringComparison.Ordinal))
            return $"available ({viaModule})";
        var bare = ProbeVersion("pip", "--version");
        return bare.StartsWith("missing:", StringComparison.Ordinal) ? "not_found" : $"available ({bare})";
    }

    private static string ProbeDocker()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info --format \"{{.ServerVersion}}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (p is null)
                return "not_found";
            p.WaitForExit(5000);
            var output = p.StandardOutput.ReadToEnd().Trim();
            return p.ExitCode == 0 && output.Length > 0 ? $"running ({output})" : "stopped_or_unavailable";
        }
        catch
        {
            return "not_found";
        }
    }

    private static string ProbeVersion(string exe, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (p is null)
                return "missing:process_start_failed";
            p.WaitForExit(5000);
            var output = (p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()).Trim();
            return p.ExitCode == 0 && output.Length > 0 ? output.Split('\n')[0].Trim() : $"missing:exit_{p.ExitCode}";
        }
        catch (Exception ex)
        {
            return $"missing:{ex.Message}";
        }
    }
}
