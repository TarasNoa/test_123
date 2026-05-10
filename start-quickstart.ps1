# Libr4 Quick Start Script
# Запускает инфраструктуру в Docker + локальные сервисы

Write-Host "🚀 Libr4 Quick Start" -ForegroundColor Cyan
Write-Host ""

# Step 1: Start infrastructure
Write-Host "📦 Step 1: Starting infrastructure containers..." -ForegroundColor Yellow
docker compose -f docker-compose.quickstart.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start infrastructure" -ForegroundColor Red
    exit 1
}

# Step 2: Wait for databases
Write-Host ""
Write-Host "⏳ Step 2: Waiting for databases to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$ready = $false

while (-not $ready -and $attempt -lt $maxAttempts) {
    Start-Sleep -Seconds 2
    $attempt++
    
    $postgres = docker exec libr4-postgres pg_isready -U libr4 2>$null
    $redis = docker exec libr4-redis redis-cli ping 2>$null
    
    if ($postgres -match "accepting connections" -and $redis -match "PONG") {
        $ready = $true
        Write-Host "   ✓ PostgreSQL ready" -ForegroundColor Green
        Write-Host "   ✓ Redis ready" -ForegroundColor Green
    } else {
        Write-Host "   Attempt $attempt/$maxAttempts..." -ForegroundColor Gray
    }
}

if (-not $ready) {
    Write-Host "⚠️  Databases not ready, but continuing..." -ForegroundColor Yellow
}

# Step 3: Show status
Write-Host ""
Write-Host "📊 Step 3: Infrastructure Status" -ForegroundColor Yellow
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | findstr libr4

# Step 4: Instructions
Write-Host ""
Write-Host "✅ Infrastructure is running!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Start Backend (new terminal):" -ForegroundColor White
Write-Host "   cd src/Services/IDE/Libr4.IDE.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start Frontend (new terminal):" -ForegroundColor White  
Write-Host "   cd src/Frontend" -ForegroundColor Gray
Write-Host "   bun dev" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Open browser:" -ForegroundColor White
Write-Host "   Frontend: http://localhost:3000" -ForegroundColor Gray
Write-Host "   API:      http://localhost:5005" -ForegroundColor Gray
Write-Host "   RabbitMQ: http://localhost:15672 (guest/guest)" -ForegroundColor Gray
Write-Host ""
Write-Host "To stop infrastructure:" -ForegroundColor White
Write-Host "   docker compose -f docker-compose.quickstart.yml down" -ForegroundColor Gray
