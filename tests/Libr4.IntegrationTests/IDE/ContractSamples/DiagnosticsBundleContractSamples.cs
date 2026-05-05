namespace Libr4.IntegrationTests.IDE.ContractSamples;

/// <summary>
/// Sample JSON payloads for diagnostics bundle endpoint contract examples.
/// These samples demonstrate the expected response shapes for different scenarios.
/// </summary>
public static class DiagnosticsBundleContractSamples
{
    /// <summary>
    /// Sample payload for a healthy diagnostics bundle.
    /// Shows normal operation with no MCP lane degradation.
    /// </summary>
    public const string HealthyDiagnostics = """
{
  "RunId": "00000000-0000-0000-0000-000000000001",
  "BundleId": "diagnostics-00000000000000000000000000000001-20260427220000",
  "GeneratedAtUtc": "2026-04-27T22:00:00Z",
  "Manifest": {
    "Status": "Succeeded",
    "FailureReason": null,
    "IterationCount": 3,
    "FileCount": 5,
    "QualityGateCount": 3,
    "BenchmarkSummary": {
      "TotalQualityEvaluations": 25,
      "TotalFailedEvaluations": 2,
      "TotalCommandDurationMs": 45000,
      "AvgCommandDurationMs": 1800,
      "TopFailureReasons": [],
      "Stages": []
    },
    "McpLaneDiagnostics": [],
    "McpLaneWatchdogSnapshot": [
      {
        "ProfileKey": "browser-lane",
        "Lane": "Browser",
        "LastCheckTimeUtc": "2026-04-27T22:00:00Z",
        "Status": "available",
        "BlockerCode": null,
        "DiagnosticMessage": null,
        "History": [
          {
            "CheckTimeUtc": "2026-04-27T22:00:00Z",
            "Status": "available",
            "BlockerCode": null
          }
        ]
      }
    ]
  },
  "Logs": {
    "SystemLogs": "=== System Logs ===\nStartedAt: 2026-04-27T21:00:00Z\nStatus: Succeeded",
    "ApplicationLogs": "=== Application Logs ===\nIteration 1: Succeeded=true",
    "ErrorLogs": "=== Error Logs ===\nNo errors"
  },
  "Files": {
    "Files": [
      {
        "RelativePath": "src/Program.cs",
        "Language": "csharp",
        "SizeBytes": 1024,
        "Content": "using System;\n\nnamespace MyApp;\n\nclass Program\n{\n    static void Main()\n    {\n        Console.WriteLine(\"Hello World\");\n    }\n}"
      }
    ]
  }
}
""";

    /// <summary>
    /// Sample payload for a degraded MCP lane diagnostics bundle.
    /// Shows MCP server failures with watchdog snapshot and history.
    /// </summary>
    public const string DegradedMcpLaneDiagnostics = """
{
  "RunId": "00000000-0000-0000-0000-000000000001",
  "BundleId": "diagnostics-00000000000000000000000000000001-20260427220000",
  "GeneratedAtUtc": "2026-04-27T22:00:00Z",
  "Manifest": {
    "Status": "Failed",
    "FailureReason": "Browser lane unavailable: mcp_server_missing",
    "IterationCount": 5,
    "FileCount": 3,
    "QualityGateCount": 3,
    "BenchmarkSummary": {
      "TotalQualityEvaluations": 25,
      "TotalFailedEvaluations": 6,
      "TotalCommandDurationMs": 52000,
      "AvgCommandDurationMs": 2080,
      "TopFailureReasons": [
        "mcp_server_missing"
      ],
      "Stages": []
    },
    "McpLaneDiagnostics": [
      {
        "Lane": "Browser",
        "DegradedEvents": 8,
        "TopBlockerCodes": [
          "mcp_server_missing",
          "mcp_server_unreachable"
        ]
      }
    ],
    "McpLaneWatchdogSnapshot": [
      {
        "ProfileKey": "browser-lane",
        "Lane": "Browser",
        "LastCheckTimeUtc": "2026-04-27T22:00:00Z",
        "Status": "degraded",
        "BlockerCode": "mcp_server_missing",
        "DiagnosticMessage": "MCP server executable not found: profile:browser-lane",
        "History": [
          {
            "CheckTimeUtc": "2026-04-27T21:50:00Z",
            "Status": "degraded",
            "BlockerCode": "mcp_server_missing"
          },
          {
            "CheckTimeUtc": "2026-04-27T21:55:00Z",
            "Status": "degraded",
            "BlockerCode": "mcp_server_missing"
          },
          {
            "CheckTimeUtc": "2026-04-27T22:00:00Z",
            "Status": "degraded",
            "BlockerCode": "mcp_server_missing"
          }
        ]
      }
    ]
  },
  "Logs": {
    "SystemLogs": "=== System Logs ===\nStartedAt: 2026-04-27T21:00:00Z\nStatus: Failed",
    "ApplicationLogs": "=== Application Logs ===\nIteration 1: Succeeded=false",
    "ErrorLogs": "=== Error Logs ===\nFailureReason: Browser lane unavailable"
  },
  "Files": {
    "Files": [
      {
        "RelativePath": "src/Program.cs",
        "Language": "csharp",
        "SizeBytes": 512,
        "Content": "using System;\n\nnamespace MyApp;\n\nclass Program\n{\n    static void Main()\n    {\n        // Incomplete due to MCP failure\n    }\n}"
      }
    ]
  }
}
""";
}
