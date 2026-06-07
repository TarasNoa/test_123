param(
    [Parameter(Mandatory = $true)]
    [string]$Query,

    [int]$Limit = 25,

    [string]$HostUrl = "http://localhost:5199"
)

$encoded = [uri]::EscapeDataString($Query)
$url = "$HostUrl/api/ide/memory/search?q=$encoded&limit=$Limit"

try {
    $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 30
    if ($response.count -eq 0) {
        Write-Host "No hits for: $Query"
        exit 0
    }

    foreach ($hit in $response.hits) {
        $step = if ($null -ne $hit.stepNumber) { " step=$($hit.stepNumber)" } else { "" }
        $tool = if ($hit.toolName) { " tool=$($hit.toolName)" } else { "" }
        $memory = if ($hit.memoryKey) { " key=$($hit.memoryKey) kind=$($hit.memoryKind)" } else { "" }
        Write-Host "[$($hit.source)] run=$($hit.runId)$step$tool$memory"
        Write-Host "  $($hit.snippet)"
    }
}
catch {
    Write-Error "Session search failed: $($_.Exception.Message)"
    exit 1
}
