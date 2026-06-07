param(
    [ValidateSet("OpenRouter", "DockerModelRunner", "BatchCi", "Benchmark")]
    [string] $Profile = "DockerModelRunner",
    [int] $Port = 5199,
    [switch] $WaitForAgentStack,
    [switch] $DockerAgentStack
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

$hostProject = Join-Path $repoRoot "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/Libr4.IDE.AutonomousAppGeneration.Host.csproj"
if (-not (Test-Path $hostProject)) {
    throw "Host project not found: $hostProject"
}

Write-Host "Starting Autonomous Host with profile=$Profile on port=$Port"

if ($DockerAgentStack) {
    Push-Location $repoRoot
    try {
        docker compose --profile agent up -d
        if ($WaitForAgentStack) {
            & (Join-Path $PSScriptRoot "Wait-AgentStackHealthy.ps1")
        }
    }
    finally {
        Pop-Location
    }
}
elseif ($WaitForAgentStack) {
    & (Join-Path $PSScriptRoot "Wait-AgentStackHealthy.ps1")
}

$env:LIBR4_HOST_PROFILE = $Profile
$env:AutonomousAppGeneration__HostProfile__ActiveProfile = $Profile
$env:PORT = "$Port"

Push-Location (Split-Path $hostProject -Parent)
try {
    dotnet run --project $hostProject --no-launch-profile --urls "http://localhost:$Port"
}
finally {
    Pop-Location
}
