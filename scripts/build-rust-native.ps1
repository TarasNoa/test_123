param(
    [switch]$Release
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$configuration = if ($Release) { "release" } else { "debug" }

Write-Host "Building Libr4 Rust native libraries ($configuration)..."

Push-Location (Join-Path $repoRoot "rust")
try {
    if ($Release) {
        cargo build --release
    } else {
        cargo build
    }
} finally {
    Pop-Location
}

Push-Location (Join-Path $repoRoot "src\Services\AI\Rust\libr4-sandbox-executor")
try {
    if ($Release) {
        cargo build --release
    } else {
        cargo build
    }
} finally {
    Pop-Location
}

Push-Location (Join-Path $repoRoot "rust")
try {
    if ($Release) {
        cargo build --release -p libr4-embeddings
    } else {
        cargo build -p libr4-embeddings
    }
} finally {
    Pop-Location
}

Write-Host "Rust native libraries built successfully."
