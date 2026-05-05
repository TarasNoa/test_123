namespace Libr4.IntegrationTests.IDE.ContractSamples;

/// <summary>
/// Sample JSON payloads for benchmark export endpoint contract examples.
/// These samples demonstrate the expected response shapes for export operations.
/// </summary>
public static class BenchmarkExportContractSamples
{
    /// <summary>
    /// Sample payload for a successful benchmark export.
    /// Shows the export metadata after persisting dashboard snapshot.
    /// </summary>
    public const string SuccessfulExport = """
{
  "ExportId": "benchmark-dashboard-20260427220000",
  "ContentSha256": "f1e2d3c4b5a6...",
  "ArtifactPath": "d:/lib4_project/artifacts/benchmark-exports/benchmark-dashboard-20260427220000.json",
  "GeneratedAtUtc": "2026-04-27T22:00:00Z",
  "Dashboard": {
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
      }
    ],
    "TopRegressions": [],
    "Runs": []
  }
}
""";
}
