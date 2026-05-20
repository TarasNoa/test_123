#!/usr/bin/env pwsh
# Start all Libr4 services

$ProjectRoot = "d:\lib4_project\libr4"
$Services = @(
    @{ Name="Gateway"; Project="src/Gateway/Libr4.Gateway"; Port=5000 },
    @{ Name="Auth API"; Project="src/Services/Auth/Libr4.Auth.Api"; Port=5001 },
    @{ Name="Tasks API"; Project="src/Services/Tasks/Libr4.Tasks.Api"; Port=5002 },
    @{ Name="Chat API"; Project="src/Services/Chat/Libr4.Chat.Api"; Port=5004 },
    @{ Name="Social API"; Project="src/Services/Social/Libr4.Social.Api"; Port=5007 }
)

Write-Host "=== Starting Libr4 Services ===" -ForegroundColor Green

foreach ($svc in $Services) {
    $proc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", "$ProjectRoot/$($svc.Project)", "--urls", "http://localhost:$($svc.Port)" `
        -WindowStyle Minimized -PassThru
    Write-Host "Started $($svc.Name) on port $($svc.Port) (PID: $($proc.Id))" -ForegroundColor Cyan
    Start-Sleep -Seconds 2
}

Write-Host "`nWaiting for services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "`n=== Health Checks ===" -ForegroundColor Green
foreach ($svc in $Services) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($svc.Port)/health" -UseBasicParsing -TimeoutSec 5
        Write-Host "$($svc.Name) (port $($svc.Port)): $($response.StatusCode) OK" -ForegroundColor Green
    } catch {
        Write-Host "$($svc.Name) (port $($svc.Port)): Not responding" -ForegroundColor Red
    }
}

Write-Host "`nAll services started! Open http://localhost:3000" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop all services" -ForegroundColor Yellow

# Keep script running
while ($true) { Start-Sleep -Seconds 10 }
