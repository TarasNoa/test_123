param(
    [switch]$Release
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildType = if ($Release) { "Release" } else { "Debug" }
$buildDir = Join-Path $repoRoot "native\cpp\build"
$outDir = Join-Path $repoRoot "tests\Libr4.IntegrationTests\bin\$buildType\net8.0"

Write-Host "Building Libr4 C++ native libraries ($buildType)..."
& (Join-Path $repoRoot "scripts\build-cpp-native.ps1") @PSBoundParameters

Write-Host "Running C++ bridge smoke tests..."
dotnet test (Join-Path $repoRoot "tests\Libr4.IntegrationTests\Libr4.IntegrationTests.csproj") `
    --configuration $buildType `
    --filter "FullyQualifiedName~CppTreeSitterBridgeSmokeTests|FullyQualifiedName~CppOrtEpBridgeSmokeTests|FullyQualifiedName~CppLibClangBridgeSmokeTests" `
    --verbosity minimal

Write-Host "C++ native smoke complete."
