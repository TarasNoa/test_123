param(
    [string]$GrpcAddress = "http://localhost:50061",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $SkipBuild) {
    Write-Host "Building libr4-embeddings (release)..."
    Push-Location (Join-Path $repoRoot "rust")
    try {
        cargo build --release -p libr4-embeddings
    } finally {
        Pop-Location
    }
}

$env:LIBR4_EMBEDDINGS_GRPC = $GrpcAddress
Write-Host "Running embeddings smoke tests against $GrpcAddress ..."
dotnet test (Join-Path $repoRoot "tests\Libr4.IntegrationTests\Libr4.IntegrationTests.csproj") `
    --configuration Release `
    --filter "FullyQualifiedName~RustEmbeddingsGrpcClientIntegrationTests" `
    --verbosity minimal
