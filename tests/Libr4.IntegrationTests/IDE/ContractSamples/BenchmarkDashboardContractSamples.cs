namespace Libr4.IntegrationTests.IDE.ContractSamples;

/// <summary>
/// Sample JSON payloads for benchmark dashboard endpoint contract examples.
/// These samples demonstrate the expected response shapes for different scenarios.
/// </summary>
public static class BenchmarkDashboardContractSamples
{
    /// <summary>
    /// Sample payload for a healthy run trend scenario.
    /// Shows normal operation with good quality scores and no regressions.
    /// </summary>
    public const string HealthyRunTrend = """
{
  "GeneratedAtUtc": "2026-04-27T22:00:00Z",
  "TotalRuns": 25,
  "SucceededRuns": 23,
  "FailedRuns": 2,
  "SuccessRate": 0.92,
  "TotalMcpDegradedEvents": 0,
  "TopMcpBlockerCodes": [],
  "TopFailureReasons": [],
  "StageTrends": [
    {
      "Stage": "Build",
      "Evaluations": 25,
      "AverageScore": 9.5,
      "PassRate": 0.95,
      "AverageDurationMs": 12000
    },
    {
      "Stage": "Test",
      "Evaluations": 25,
      "AverageScore": 9.2,
      "PassRate": 0.90,
      "AverageDurationMs": 18000
    },
    {
      "Stage": "Fix",
      "Evaluations": 25,
      "AverageScore": 9.0,
      "PassRate": 0.88,
      "AverageDurationMs": 15000
    }
  ],
  "TopRegressions": [],
  "Runs": [
    {
      "RunId": "00000000-0000-0000-0000-000000000001",
      "Status": "Succeeded",
      "StartedAtUtc": "2026-04-27T21:00:00Z",
      "CompletedAtUtc": "2026-04-27T21:00:42Z",
      "OverallScore": 9,
      "FailedQualityGates": 0,
      "TotalCommandDurationMs": 42000
    }
  ]
}
""";

    /// <summary>
    /// Sample payload for a degraded MCP lane scenario.
    /// Shows MCP server failures with degraded events and blocker codes.
    /// </summary>
    public const string DegradedMcpLane = """
{
  "GeneratedAtUtc": "2026-04-27T22:00:00Z",
  "TotalRuns": 25,
  "SucceededRuns": 19,
  "FailedRuns": 6,
  "SuccessRate": 0.76,
  "TotalMcpDegradedEvents": 12,
  "TopMcpBlockerCodes": [
    "mcp_server_missing",
    "mcp_server_unreachable"
  ],
  "TopFailureReasons": [
    "Browser lane unavailable",
    "n8n workflow timeout"
  ],
  "StageTrends": [
    {
      "Stage": "Build",
      "Evaluations": 25,
      "AverageScore": 8.5,
      "PassRate": 0.80,
      "AverageDurationMs": 15000
    },
    {
      "Stage": "Test",
      "Evaluations": 25,
      "AverageScore": 7.0,
      "PassRate": 0.70,
      "AverageDurationMs": 22000
    },
    {
      "Stage": "Fix",
      "Evaluations": 25,
      "AverageScore": 7.5,
      "PassRate": 0.75,
      "AverageDurationMs": 15000
    }
  ],
  "TopRegressions": [],
  "Runs": [
    {
      "RunId": "00000000-0000-0000-0000-000000000001",
      "Status": "Failed",
      "StartedAtUtc": "2026-04-27T21:00:00Z",
      "CompletedAtUtc": "2026-04-27T21:00:58Z",
      "OverallScore": 7,
      "FailedQualityGates": 2,
      "TotalCommandDurationMs": 58000
    }
  ]
}
""";

    /// <summary>
    /// Sample payload for a regression-heavy scenario.
    /// Shows significant quality drops with populated top_regressions.
    /// </summary>
    public const string RegressionHeavy = """
{
  "GeneratedAtUtc": "2026-04-27T22:00:00Z",
  "TotalRuns": 25,
  "SucceededRuns": 17,
  "FailedRuns": 8,
  "SuccessRate": 0.68,
  "TotalMcpDegradedEvents": 3,
  "TopMcpBlockerCodes": [
    "mcp_server_unavailable"
  ],
  "TopFailureReasons": [
    "Quality gate failed: Build stage score below threshold",
    "Test execution timeout"
  ],
  "StageTrends": [
    {
      "Stage": "Build",
      "Evaluations": 25,
      "AverageScore": 7.2,
      "PassRate": 0.65,
      "AverageDurationMs": 14000
    },
    {
      "Stage": "Test",
      "Evaluations": 25,
      "AverageScore": 6.8,
      "PassRate": 0.60,
      "AverageDurationMs": 20000
    },
    {
      "Stage": "Fix",
      "Evaluations": 25,
      "AverageScore": 7.0,
      "PassRate": 0.70,
      "AverageDurationMs": 14000
    }
  ],
  "TopRegressions": [
    {
      "Stage": "Build",
      "BaselineAverageScore": 9.5,
      "LatestScore": 7,
      "Delta": -2.5,
      "LatestFailureReasons": [
        "Build stage score dropped below threshold (9.0 -> 7.0)",
        "Compilation errors in generated code"
      ]
    },
    {
      "Stage": "Test",
      "BaselineAverageScore": 9.2,
      "LatestScore": 6,
      "Delta": -3.2,
      "LatestFailureReasons": [
        "Test execution timeout",
        "Test coverage below threshold"
      ]
    }
  ],
  "Runs": [
    {
      "RunId": "00000000-0000-0000-0000-000000000001",
      "Status": "Failed",
      "StartedAtUtc": "2026-04-27T21:00:00Z",
      "CompletedAtUtc": "2026-04-27T21:00:55Z",
      "OverallScore": 6,
      "FailedQualityGates": 3,
      "TotalCommandDurationMs": 55000
    }
  ]
}
""";
}
