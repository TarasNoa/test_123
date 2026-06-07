param(
    [string]$HostUrl = $env:AUTOGEN_HOST_URL ?? "http://localhost:5200",
    [int]$MaxIterations = 8,
    [switch]$WaitForCompletion
)

$ErrorActionPreference = "Stop"
$scenarios = @(
    @{ id = "calorie-vision"; request = "Build CalorieVision: Django REST backend with calorie tracking API and SolidJS frontend with dashboard." },
    @{ id = "banking"; request = "Build a Banking app: Spring Boot backend with accounts/transfers and React frontend." },
    @{ id = "nextjs"; request = "Build a Next.js 14 fullstack todo app with API routes and Tailwind UI." }
)

$started = @()
foreach ($scenario in $scenarios) {
    $body = @{
        userRequest = $scenario.request
        maxIterations = $MaxIterations
        triggerSource = "nightly-ci"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Method Post -Uri "$HostUrl/api/ide/app-generation/start" -ContentType "application/json" -Body $body
    $runId = $response.id
    if (-not $runId) {
        Write-Warning "Run not registered yet for $($scenario.id): $($response.message)"
        continue
    }

    Write-Host "Started $($scenario.id) run $runId"
    $started += [pscustomobject]@{ ScenarioId = $scenario.id; RunId = $runId }
}

if (-not $WaitForCompletion) {
    $started | ConvertTo-Json -Depth 3
    exit 0
}

$failed = 0
foreach ($item in $started) {
    $deadline = (Get-Date).AddHours(2)
    do {
        Start-Sleep -Seconds 30
        $eval = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/benchmark/regression/evaluate/$($item.RunId)?scenarioId=$($item.ScenarioId)"
        $report = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$($item.RunId)"
        $terminal = $report.status -in @("Completed", "Failed", "Cancelled")
    } while (-not $terminal -and (Get-Date) -lt $deadline)

    if ($eval.passed) {
        Write-Host "PASS $($item.ScenarioId) $($item.RunId)"
    }
    else {
        Write-Host "FAIL $($item.ScenarioId) $($item.RunId)"
        $failed++
    }
}

if ($failed -gt 0) { exit 1 }
exit 0
