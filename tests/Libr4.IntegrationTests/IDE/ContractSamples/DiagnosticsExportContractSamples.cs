namespace Libr4.IntegrationTests.IDE.ContractSamples;

/// <summary>
/// Sample JSON payloads for diagnostics export endpoint contract examples.
/// These samples demonstrate the expected response shapes for export operations.
/// </summary>
public static class DiagnosticsExportContractSamples
{
    /// <summary>
    /// Sample payload for a successful diagnostics export.
    /// Shows the export metadata after creating a zipped artifact.
    /// </summary>
    public const string SuccessfulExport = """
{
  "RunId": "00000000-0000-0000-0000-000000000001",
  "ExportId": "diagnostics-00000000000000000000000000000001-20260427220000",
  "ContentSha256": "a1b2c3d4e5f6...",
  "ArtifactPath": "d:/lib4_project/artifacts/diagnostics-exports/diagnostics-00000000000000000000000000000001-20260427220000.zip",
  "GeneratedAtUtc": "2026-04-27T22:00:00Z"
}
""";
}
