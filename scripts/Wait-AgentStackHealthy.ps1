param(
    [string[]] $Services = @(
        @{ Name = "obscura"; Url = "http://localhost:9222/json/version" },
        @{ Name = "shadow-sync"; Url = "http://localhost:8080/health" },
        @{ Name = "sandbox-controller"; Url = "http://localhost:9090/health" },
        @{ Name = "security-scanner"; Url = "http://localhost:7070/health" },
        @{ Name = "qdrant"; Url = "http://localhost:6333/healthz" },
        @{ Name = "autonomous-host"; Url = "http://localhost:5199/health/agent-stack" }
    ),
    [int] $TimeoutSeconds = 180,
    [int] $PollIntervalSeconds = 5
)

$ErrorActionPreference = "Stop"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

function Test-EndpointHealthy {
    param([string] $Url)
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 8
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 300
    }
    catch {
        return $false
    }
}

Write-Host "Waiting for agent stack health (timeout ${TimeoutSeconds}s)..."

while ((Get-Date) -lt $deadline) {
    $pending = @()
    foreach ($svc in $Services) {
        if (-not (Test-EndpointHealthy -Url $svc.Url)) {
            $pending += $svc.Name
        }
    }

    if ($pending.Count -eq 0) {
        Write-Host "Agent stack healthy."
        exit 0
    }

    Write-Host ("Pending: " + ($pending -join ", "))
    Start-Sleep -Seconds $PollIntervalSeconds
}

Write-Host "Agent stack health gate timed out."
exit 1
