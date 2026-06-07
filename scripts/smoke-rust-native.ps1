param(
    [switch]$Release,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$configuration = if ($Release) { "Release" } else { "Debug" }

if (-not $SkipBuild) {
    if ($Release) {
        & (Join-Path $repoRoot "scripts\build-rust-native.ps1") -Release
    } else {
        & (Join-Path $repoRoot "scripts\build-rust-native.ps1")
    }
}

Write-Host "Running Wave 3 Rust native bridge smoke tests ($configuration)..."
dotnet test (Join-Path $repoRoot "tests\Libr4.IntegrationTests\Libr4.IntegrationTests.csproj") `
    --configuration $configuration `
    --filter "FullyQualifiedName~RustNativeBridgesSmokeTests|FullyQualifiedName~RustSandboxExecutorBridgeTests|FullyQualifiedName~RustEmbeddingsGrpcClientIntegrationTests" `
    --verbosity minimal
