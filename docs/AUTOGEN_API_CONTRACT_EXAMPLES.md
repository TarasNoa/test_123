# Autonomous App Generation API Contract Examples

This document provides UI-ready sample payloads for the autonomous generation observability APIs.

## 1) Benchmark Dashboard (`GET /api/ide/app-generation/dashboard/benchmark`)

### Healthy trend example

```json
{
  "generatedAtUtc": "2026-04-27T21:58:10Z",
  "totalRuns": 12,
  "succeededRuns": 11,
  "failedRuns": 1,
  "successRate": 0.9167,
  "totalMcpDegradedEvents": 0,
  "topMcpBlockerCodes": [],
  "topFailureReasons": ["build_failed"],
  "stageTrends": [
    { "stage": "plan", "evaluations": 12, "averageScore": 9.9, "passRate": 1.0, "averageDurationMs": 0 },
    { "stage": "generation", "evaluations": 12, "averageScore": 9.2, "passRate": 0.92, "averageDurationMs": 1820 }
  ],
  "topRegressions": [],
  "runs": [
    {
      "runId": "0f5a35d2-40c7-4273-8f1d-d0e16d1e21a4",
      "status": "Completed",
      "startedAtUtc": "2026-04-27T21:55:00Z",
      "completedAtUtc": "2026-04-27T21:56:12Z",
      "overallScore": 9,
      "failedQualityGates": 0,
      "totalCommandDurationMs": 2580
    }
  ]
}
```

### Degraded MCP lane example

```json
{
  "generatedAtUtc": "2026-04-27T22:00:30Z",
  "totalRuns": 10,
  "succeededRuns": 9,
  "failedRuns": 1,
  "successRate": 0.9,
  "totalMcpDegradedEvents": 4,
  "topMcpBlockerCodes": ["mcp_server_missing"],
  "topFailureReasons": ["build_failed"],
  "stageTrends": [],
  "topRegressions": [],
  "runs": []
}
```

### Regression-heavy example

```json
{
  "generatedAtUtc": "2026-04-27T22:02:00Z",
  "totalRuns": 20,
  "succeededRuns": 14,
  "failedRuns": 6,
  "successRate": 0.7,
  "totalMcpDegradedEvents": 2,
  "topMcpBlockerCodes": ["mcp_server_unreachable"],
  "topFailureReasons": ["build_failed", "missing_api_validation_contracts"],
  "stageTrends": [],
  "topRegressions": [
    {
      "stage": "build",
      "baselineAverageScore": 8.6,
      "latestScore": 3,
      "delta": -5.6,
      "latestFailureReasons": ["build_failed", "build_non_zero_exit"]
    }
  ],
  "runs": []
}
```

## 2) Benchmark Export (`GET /api/ide/app-generation/dashboard/benchmark/export`)

```json
{
  "exportId": "benchmark-20260427220545-2ecb2d2d197f4a7eb7f6aebec4a84d5a",
  "contentSha256": "6ff7f0b26e6ef1a0118488a980b2ce0937d9b2f0306753f3eb44aef0a5f4a4c1",
  "artifactPath": "d:/lib4_project/artifacts/benchmark-exports/benchmark-20260427220545-2ecb2d2d197f4a7eb7f6aebec4a84d5a.json",
  "generatedAtUtc": "2026-04-27T22:05:45Z",
  "dashboard": {
    "totalRuns": 20
  }
}
```

## 3) Diagnostics Bundle (`GET /api/ide/app-generation/{id}/diagnostics`)

```json
{
  "runId": "95d15f9b-bbdd-4aa6-92e9-a48bb9373ee6",
  "bundleId": "diagnostics-95d15f9bbbdd4aa692e9a48bb9373ee6-20260427221000",
  "generatedAtUtc": "2026-04-27T22:10:00Z",
  "manifest": {
    "status": "Failed",
    "failureReason": "quality_gate_build_failed",
    "iterationCount": 2,
    "fileCount": 14,
    "qualityGateCount": 7,
    "benchmarkSummary": { "totalQualityEvaluations": 7, "totalFailedEvaluations": 1, "totalCommandDurationMs": 4100, "avgCommandDurationMs": 1025, "topFailureReasons": ["build_failed"], "stages": [] },
    "mcpLaneDiagnostics": [{ "lane": "Browser", "degradedEvents": 2, "topBlockerCodes": ["mcp_server_missing"] }],
    "mcpLaneWatchdogSnapshot": [{ "profileKey": "browser-lane", "lane": "Browser", "lastCheckTimeUtc": "2026-04-27T22:09:58Z", "status": "degraded", "blockerCode": "mcp_server_missing", "diagnosticMessage": "MCP server executable not found", "history": [] }]
  },
  "logs": { "systemLogs": "...", "applicationLogs": "...", "errorLogs": "..." },
  "files": { "files": [] }
}
```

## 4) Diagnostics Export (`GET /api/ide/app-generation/{id}/diagnostics/export`)

```json
{
  "runId": "95d15f9b-bbdd-4aa6-92e9-a48bb9373ee6",
  "exportId": "diagnostics-95d15f9bbbdd4aa692e9a48bb9373ee6-20260427221233",
  "contentSha256": "8bc1a88379fa493510d2c916355b4f734efbc3cd4d91d9f9f5b8e976e88a70b4",
  "artifactPath": "d:/lib4_project/artifacts/diagnostics-exports/diagnostics-95d15f9bbbdd4aa692e9a48bb9373ee6-20260427221233.zip",
  "generatedAtUtc": "2026-04-27T22:12:33Z"
}
```

## 5) Stage C Readiness (`GET /api/ide/app-generation/dashboard/readiness`)

```json
{
  "generatedAtUtc": "2026-04-27T22:20:00Z",
  "deterministicFallbackEnabled": true,
  "stdioTransportEnabled": false,
  "totalProfiles": 2,
  "degradedProfiles": 2,
  "overallStatus": "degraded",
  "overallRecommendations": [
    "EnableStdioTransport must be true for real MCP execution lanes.",
    "Fix lane/profile 'Browser/browser-lane' blocker 'mcp_server_missing'."
  ],
  "items": [
    {
      "profileKey": "browser-lane",
      "lane": "Browser",
      "status": "degraded",
      "blockerCode": "mcp_server_missing",
      "diagnosticMessage": "MCP server executable not found",
      "killSwitchActive": false,
      "remediationHints": [
        "Verify configured executable/script path for Browser lane server.",
        "Install or create local MCP server stub at configured location.",
        "Recheck endpoint /api/ide/app-generation/dashboard/readiness after fix."
      ]
    }
  ]
}
```
