<#
.SYNOPSIS
    Full E2E test for all Libr4 APIs.
.DESCRIPTION
    Starts all backend APIs, waits for them to be ready, runs health + basic smoke tests,
    and reports results.
#>
$ErrorActionPreference = "Stop"
$env:ASPNETCORE_ENVIRONMENT = "Development"

$apis = @(
    @{ Name = "Gateway";       Proj = "d:\lib4_project\libr4\src\Gateway\Libr4.Gateway\Libr4.Gateway.csproj";                       Port = 5000; Health = "/health" },
    @{ Name = "Auth";          Proj = "d:\lib4_project\libr4\src\Services\Auth\Libr4.Auth.Api\Libr4.Auth.Api.csproj";              Port = 5001; Health = "/health" },
    @{ Name = "Chat";          Proj = "d:\lib4_project\libr4\src\Services\Chat\Libr4.Chat.Api\Libr4.Chat.Api.csproj";              Port = 5004; Health = "/health" },
    @{ Name = "AI";            Proj = "d:\lib4_project\libr4\src\Services\AI\Libr4.AI.Api\Libr4.AI.Api.csproj";                    Port = 5006; Health = "/health" },
    @{ Name = "Analytics";     Proj = "d:\lib4_project\libr4\src\Services\Analytics\Libr4.Analytics.Api\Libr4.Analytics.Api.csproj"; Port = 5007; Health = "/health" },
    @{ Name = "IDE";           Proj = "d:\lib4_project\libr4\src\Services\IDE\Libr4.IDE.Api\Libr4.IDE.Api.csproj";                  Port = 5008; Health = "/health" },
    @{ Name = "Matching";      Proj = "d:\lib4_project\libr4\src\Services\Matching\Libr4.Matching.Api\Libr4.Matching.Api.csproj";  Port = 5009; Health = "/health" },
    @{ Name = "Payments";      Proj = "d:\lib4_project\libr4\src\Services\Payments\Libr4.Payments.Api\Libr4.Payments.Api.csproj";  Port = 5010; Health = "/health" },
    @{ Name = "Social";        Proj = "d:\lib4_project\libr4\src\Services\Social\Libr4.Social.Api\Libr4.Social.Api.csproj";       Port = 5011; Health = "/health" },
    @{ Name = "Tasks";         Proj = "d:\lib4_project\libr4\src\Services\Tasks\Libr4.Tasks.Api\Libr4.Tasks.Api.csproj";          Port = 5012; Health = "/health" },
    @{ Name = "Trading";       Proj = "d:\lib4_project\libr4\src\Services\Trading\Libr4.Trading.Api\Libr4.Trading.Api.csproj";    Port = 5013; Health = "/health" },
    @{ Name = "Collaboration"; Proj = "d:\lib4_project\libr4\src\Services\Collaboration\Libr4.Collaboration.Api\Libr4.Collaboration.Api.csproj"; Port = 5015; Health = "/health" }
)

$logDir = "d:\lib4_project\libr4\e2e-logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

# 1. Kill any leftover dotnet / API processes
Write-Host "`n[1/5] Cleaning up old processes..." -ForegroundColor Cyan
Get-Process | Where-Object { $_.ProcessName -in @("dotnet","Libr4.Gateway","Libr4.Auth.Api","Libr4.Chat.Api","Libr4.AI.Api","Libr4.Analytics.Api","Libr4.IDE.Api","Libr4.Matching.Api","Libr4.Payments.Api","Libr4.Social.Api","Libr4.Tasks.Api","Libr4.Trading.Api","Libr4.Collaboration.Api") } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep 2

# 2. Start all APIs in background
Write-Host "`n[2/5] Starting all APIs..." -ForegroundColor Cyan
$processes = @()
foreach ($api in $apis) {
    $outFile = "$logDir\$($api.Name)-out.log"
    $errFile = "$logDir\$($api.Name)-err.log"
    $proc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run","--project",$api.Proj,"--urls",("http://localhost:" + $api.Port) `
        -RedirectStandardOutput $outFile -RedirectStandardError $errFile `
        -PassThru -WindowStyle Hidden
    $processes += [PSCustomObject]@{ Name = $api.Name; Proc = $proc; Port = $api.Port; Health = $api.Health }
    Write-Host "  $($api.Name) -> PID $($proc.Id) : $($api.Port)" -ForegroundColor Gray
}

# 3. Wait for APIs to be ready
Write-Host "`n[3/5] Waiting for APIs to be ready (40s)..." -ForegroundColor Cyan
Start-Sleep 40

# 4. Run smoke tests
Write-Host "`n[4/5] Running smoke tests..." -ForegroundColor Cyan
$results = @()
foreach ($api in $processes) {
    $url = "http://localhost:$($api.Port)$($api.Health)"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $resp = Invoke-WebRequest -Uri $url -TimeoutSec 5 -ErrorAction Stop
        $sw.Stop()
        $results += [PSCustomObject]@{
            API      = $api.Name
            URL      = $url
            Status   = $resp.StatusCode
            Latency  = "$($sw.ElapsedMilliseconds)ms"
            Result   = "PASS"
            Color    = "Green"
        }
        Write-Host "  $($api.Name) : $($resp.StatusCode) in $($sw.ElapsedMilliseconds)ms" -ForegroundColor Green
    } catch {
        $sw.Stop()
        $results += [PSCustomObject]@{
            API      = $api.Name
            URL      = $url
            Status   = $_.Exception.Response.StatusCode.value__
            Latency  = "$($sw.ElapsedMilliseconds)ms"
            Result   = "FAIL"
            Color    = "Red"
        }
        Write-Host "  $($api.Name) : FAIL - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 5. Gateway routing smoke test
Write-Host "`n[5/5] Gateway routing smoke test..." -ForegroundColor Cyan
$gatewayTests = @(
    @{ Name = "Gateway -> Auth";    Path = "/api/auth/health" },
    @{ Name = "Gateway -> Social";  Path = "/api/social/health" }
)
foreach ($gt in $gatewayTests) {
    $url = "http://localhost:5000$($gt.Path)"
    try {
        $resp = Invoke-WebRequest -Uri $url -TimeoutSec 5 -ErrorAction Stop
        Write-Host "  $($gt.Name) : $($resp.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "  $($gt.Name) : FAIL - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 6. Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "           E2E TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
$pass = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$fail = ($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "  Passed : $pass / $($results.Count)" -ForegroundColor Green
Write-Host "  Failed : $fail / $($results.Count)" -ForegroundColor $(if ($fail -gt 0) { "Red" } else { "Green" })

# 7. Cleanup
Write-Host "`nStopping all API processes..." -ForegroundColor Cyan
foreach ($p in $processes) {
    try { Stop-Process -Id $p.Proc.Id -Force -ErrorAction SilentlyContinue } catch {}
}
Write-Host "Done.`n" -ForegroundColor Cyan
