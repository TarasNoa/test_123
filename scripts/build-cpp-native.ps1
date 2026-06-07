param(
    [switch]$Release
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildType = if ($Release) { "Release" } else { "Debug" }
$buildDir = Join-Path $repoRoot "native\cpp\build"

Write-Host "Building Libr4 C++ native libraries ($buildType)..."

cmake -S (Join-Path $repoRoot "native\cpp") -B $buildDir -DCMAKE_BUILD_TYPE=$buildType
cmake --build $buildDir --config $buildType --parallel

Write-Host "C++ native libraries built successfully."
