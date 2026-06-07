param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot
try {
    dotnet test "tests/Libr4.IntegrationTests/Libr4.IntegrationTests.csproj" `
        --configuration $Configuration `
        --filter "FullyQualifiedName~InternalEvalHarnessTests" `
        --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        throw "Internal eval regression gate failed"
    }
    Write-Host "Internal eval regression gate passed."
}
finally {
    Pop-Location
}
