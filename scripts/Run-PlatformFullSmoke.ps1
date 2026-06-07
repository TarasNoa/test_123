param(
    [string]$HostUrl = "http://localhost:5199",
    [string]$IdeApiUrl = "http://localhost:5005",
    [string]$OpenRouterModel = "deepseek/deepseek-v4-flash",
    [switch]$SkipDocker,
    [switch]$SkipIntegrationTests,
    [switch]$SkipLiveLlm,
    [switch]$FullModules,
    [int]$GenerationTimeoutMinutes = 25,
    [int]$GenerationMaxIterations = 7
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$results = [ordered]@{}

function Write-Phase([string]$Title) {
    Write-Host "`n=== $Title ===" -ForegroundColor Cyan
}

function Test-HttpOk([string]$Url, [int]$TimeoutSec = 10) {
    try {
        $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec
        return @{ Ok = ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 300); Status = $resp.StatusCode; Error = $null }
    }
    catch {
        return @{ Ok = $false; Status = $null; Error = $_.Exception.Message }
    }
}

function Load-OpenRouterApiKey {
    if ($env:OPENROUTER_API_KEY) { return $env:OPENROUTER_API_KEY }

    $envFile = Join-Path $repoRoot ".env"
    if (Test-Path $envFile) {
        foreach ($line in Get-Content $envFile) {
            if ($line -match '^\s*OPENROUTER_API_KEY\s*=\s*(.+)\s*$') {
                $key = $Matches[1].Trim().Trim('"').Trim("'")
                if ($key) { return $key }
            }
        }
    }

    return $null
}

function Stop-ProcessOnPort([int]$Port) {
    $conns = @(Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue)
    if ($conns.Count -eq 0) { return }

    foreach ($procId in ($conns | Select-Object -ExpandProperty OwningProcess -Unique)) {
        if ($procId -le 0) { continue }
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if (-not $proc) { continue }
        Write-Host "  Stopping $($proc.ProcessName) (PID $procId) on port $Port" -ForegroundColor Yellow
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 2
}

function Get-LatestQualityGateHint($qualityGates) {
    if (-not $qualityGates) { return "" }
    $interesting = @("build", "verify", "repair", "fix", "test", "startup", "review", "iteration")
    $gate = $qualityGates |
        Where-Object {
            $stage = if ($_.stage) { "$($_.stage)".ToLowerInvariant() } else { "" }
            ($interesting | Where-Object { $stage -like "*$_*" }).Count -gt 0
        } |
        Select-Object -Last 1
    if (-not $gate) { return "" }
    $reason = ($gate.reasons | Select-Object -First 2) -join ";"
    return " gate=$($gate.stage):$($gate.passed) $reason"
}

function Write-GenerationProgressLine($report, $runState) {
    $iterCount = @($report.iterations).Count
    $lastIter = if ($iterCount -gt 0) { $report.iterations[-1] } else { $null }
    $iterHint = if ($lastIter) {
        " iter=$iterCount/$($report.plan.maxIterations) ok=$($lastIter.succeeded) errs=$($lastIter.errorCount) fixes=$($lastIter.appliedFixes.Count)"
    } else {
        " iter=0/$($report.plan.maxIterations)"
    }
    $activeHint = ""
    if ($runState) {
        $activeHint = " active=$($runState.stage) prog=$($runState.progressPercent)%"
    }
    $gateHint = Get-LatestQualityGateHint $report.qualityGates
    Write-Host "  Status: $($report.status) files=$($report.fileCount)$iterHint$activeHint$gateHint" -ForegroundColor Gray
}

function Write-GenerationPipelineAudit([string]$HostUrl, [string]$RunId) {
    Write-Host "`n  --- Pipeline audit (repair / test / launch) ---" -ForegroundColor Cyan
    try {
        $report = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$RunId" -TimeoutSec 30
        if ($report.plan) {
            Write-Host "  Plan: $($report.plan.applicationName) stack=$($report.plan.techStack.backendFramework)" -ForegroundColor Gray
            if ($report.plan.buildCommands) {
                Write-Host "  Build commands:" -ForegroundColor Gray
                $report.plan.buildCommands | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
            }
            if ($report.plan.testCommands) {
                Write-Host "  Test commands:" -ForegroundColor Gray
                $report.plan.testCommands | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
            }
        }
        if ($report.iterations -and $report.iterations.Count -gt 0) {
            Write-Host "  Iterations:" -ForegroundColor Gray
            foreach ($it in $report.iterations) {
                $fixes = ($it.appliedFixes -join ", ")
                if ([string]::IsNullOrWhiteSpace($fixes)) { $fixes = "(none)" }
                Write-Host "    #$($it.number) ok=$($it.succeeded) errors=$($it.errorCount) fixes=$fixes" -ForegroundColor DarkGray
            }
        }
        if ($report.outstandingErrors -and $report.outstandingErrors.Count -gt 0) {
            Write-Host "  Outstanding errors:" -ForegroundColor Yellow
            $report.outstandingErrors | Select-Object -First 5 | ForEach-Object {
                Write-Host "    [$($_.errorType)] $($_.filePath): $($_.message)" -ForegroundColor DarkYellow
            }
        }
        if ($report.manifest -and $report.manifest.commands) {
            Write-Host "  Command executions ($($report.manifest.totalCommands)):" -ForegroundColor Gray
            $report.manifest.commands | Select-Object -Last 8 | ForEach-Object {
                Write-Host "    [$($_.phase)] exit=$($_.exitCode) $($_.command)" -ForegroundColor DarkGray
            }
        }
    }
    catch {
        Write-Host "  Report audit skipped: $_" -ForegroundColor Yellow
    }

    try {
        $dash = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$RunId/dashboard/build" -TimeoutSec 30
        Write-Host "  Build dashboard: stack=$($dash.summary.detectedStack) gates=$($dash.summary.passedGates)/$($dash.summary.totalGates) pipeline=$($dash.recoveryEfficiency.pipelineStageReached)" -ForegroundColor Gray
        if ($dash.repairTiers -and $dash.repairTiers.Count -gt 0) {
            Write-Host "  Repair tiers:" -ForegroundColor Gray
            $dash.repairTiers | ForEach-Object {
                Write-Host "    $($_.tierName): attempts=$($_.attempts) resolved=$($_.resolved) failed=$($_.failed)" -ForegroundColor DarkGray
            }
        }
        if ($dash.verifyEvidence -and $dash.verifyEvidence.artifacts) {
            Write-Host "  Verify artifacts ($($dash.verifyEvidence.artifacts.Count)):" -ForegroundColor Gray
            $dash.verifyEvidence.artifacts | Select-Object -First 6 | ForEach-Object {
                Write-Host "    $($_.kind) $($_.fileName) ($($_.sizeBytes) bytes)" -ForegroundColor DarkGray
            }
        }
        if ($dash.recoveryEfficiency -and $dash.recoveryEfficiency.events) {
            Write-Host "  Recovery events:" -ForegroundColor Gray
            $dash.recoveryEfficiency.events | Select-Object -Last 5 | ForEach-Object {
                Write-Host "    iter=$($_.iteration) root=$($_.rootCauseCategory) by=$($_.recoveredBy) buildAfter=$($_.buildSucceededAfterRepair)" -ForegroundColor DarkGray
            }
        }
    }
    catch {
        Write-Host "  Build dashboard skipped: $_" -ForegroundColor Yellow
    }
    Write-Host "  --- end pipeline audit ---" -ForegroundColor Cyan
}

Write-Phase "Phase 0 - Wiring audit"
$wiringChecks = @(
    @{ Name = "AutonomousAppGeneration DI in IDE.Api"; Path = "src/Services/IDE/Libr4.IDE.Api/Program.cs"; Pattern = "AddAutonomousAppGeneration" },
    @{ Name = "App-generation endpoints in IDE.Api"; Path = "src/Services/IDE/Libr4.IDE.Api/Program.cs"; Pattern = "MapAutonomousAppGenerationEndpoints" },
    @{ Name = "OpenRouter profile overlay"; Path = "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/appsettings.Profile.OpenRouter.json"; Pattern = "deepseek/deepseek-v4-flash" },
    @{ Name = "Agent stack health endpoint"; Path = "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/Program.cs"; Pattern = "/health/agent-stack" },
    @{ Name = "Obscura browser plane"; Path = "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/DependencyInjection.cs"; Pattern = "AddObscuraBrowserPlane" },
    @{ Name = "Qdrant sync DI"; Path = "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/Memory/Qdrant/QdrantSyncServiceCollectionExtensions.cs"; Pattern = "AddQdrantSync" },
    @{ Name = "Agent runtime stream"; Path = "src/Services/IDE/Libr4.IDE.Api/AgentRuntimeWebSocketBridge.cs"; Pattern = "AgentRuntime" },
    @{ Name = "F# algorithms bridge"; Path = "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Infrastructure/Algorithms/FSharpAlgorithmsBridge.cs"; Pattern = "BuildRepoGraph" },
    @{ Name = "Rust native bridges"; Path = "build/Libr4.RustNative.targets"; Pattern = "CopyLibr4RustNativeLibraries" },
    @{ Name = "C++ native bridges"; Path = "build/Libr4.CppNative.targets"; Pattern = "CopyLibr4CppNativeLibraries" }
)

$wiringOk = $true
foreach ($check in $wiringChecks) {
    $fullPath = Join-Path $repoRoot $check.Path
    $content = Get-Content $fullPath -Raw -ErrorAction SilentlyContinue
    $ok = $content -and ($content -match [regex]::Escape($check.Pattern))
    if (-not $ok) { $wiringOk = $false }
    Write-Host ("  [{0}] {1}" -f $(if ($ok) { "OK" } else { "FAIL" }), $check.Name) -ForegroundColor $(if ($ok) { "Green" } else { "Red" })
}
$results["WiringAudit"] = $wiringOk

Write-Phase "Phase 1 - Build"
if (-not $SkipLiveLlm) {
    Write-Host "  Freeing port 5199 (stop stale autonomous host before rebuild)..." -ForegroundColor Gray
    Stop-ProcessOnPort 5199
}
Push-Location $repoRoot
try {
    dotnet build libr4.sln --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
    $results["Build"] = $true
}
catch {
    $results["Build"] = $false
    Write-Host "  Build failed: $_" -ForegroundColor Red
}
finally {
    Pop-Location
}

if (-not $SkipIntegrationTests) {
    Write-Phase $(if ($FullModules) { "Phase 2 - FULL integration test suite (all IDE modules)" } else { "Phase 2 - Platform integration tests (filtered)" })
    $testFilter = "FullyQualifiedName~InternalEvalHarnessTests|FullyQualifiedName~AutonomousHostProfileTests|FullyQualifiedName~AgentModelRouterTests|FullyQualifiedName~AgentStack|FullyQualifiedName~FullPipelineStagesE2ETests|FullyQualifiedName~RustNativeBridgesSmokeTests|FullyQualifiedName~CppTreeSitterBridgeSmokeTests|FullyQualifiedName~CppOrtEpBridgeSmokeTests|FullyQualifiedName~CppLibClangBridgeSmokeTests|FullyQualifiedName~AgentRuntimeTests|FullyQualifiedName~ProviderCapabilityMatrixRoutingTests|FullyQualifiedName~RepoGraphBuilderTests|FullyQualifiedName~QdrantSyncTests|FullyQualifiedName~HermesMemoryManagerTests|FullyQualifiedName~ObscuraIntegrationTests|FullyQualifiedName~VerifyIntegrationTests|FullyQualifiedName~DelegationE2ETests|FullyQualifiedName~AutogenApiContractShapeTests"

    Push-Location $repoRoot
    try {
        if ($FullModules) {
            dotnet test "tests/Libr4.IntegrationTests/Libr4.IntegrationTests.csproj" --configuration Release --logger "console;verbosity=minimal"
        }
        else {
            dotnet test "tests/Libr4.IntegrationTests/Libr4.IntegrationTests.csproj" --configuration Release --no-build --filter $testFilter --logger "console;verbosity=minimal"
        }
        $results["IntegrationTests"] = ($LASTEXITCODE -eq 0)
    }
    catch {
        $results["IntegrationTests"] = $false
    }
    finally {
        Pop-Location
    }

    Write-Phase "Phase 2b - F# algorithms tests"
    Push-Location $repoRoot
    try {
        dotnet test "tests/Libr4.IDE.AutonomousAppGeneration.Algorithms.FSharp.Tests/Libr4.IDE.AutonomousAppGeneration.Algorithms.FSharp.Tests.fsproj" --configuration Release --logger "console;verbosity=minimal"
        $results["FSharpAlgorithmsTests"] = ($LASTEXITCODE -eq 0)
        dotnet test "tests/Libr4.IDE.Domain.Algorithms.Tests/Libr4.IDE.Domain.Algorithms.Tests.fsproj" --configuration Release --logger "console;verbosity=minimal"
        $results["FSharpDomainAlgorithmsTests"] = ($LASTEXITCODE -eq 0)
    }
    finally {
        Pop-Location
    }

    Write-Phase "Phase 2c - AI service unit tests"
    Push-Location $repoRoot
    try {
        dotnet test "src/Services/AI/Libr4.AI.Tests/Libr4.AI.Tests.csproj" --configuration Release --logger "console;verbosity=minimal"
        $results["AiUnitTests"] = ($LASTEXITCODE -eq 0)
    }
    catch { $results["AiUnitTests"] = $false }
    finally { Pop-Location }
}

if (-not $SkipDocker) {
    Write-Phase "Phase 3 - Agent stack (Docker profile)"
    Push-Location $repoRoot
    try {
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        docker compose --profile agent up -d qdrant rust-embeddings obscura shadow-sync sandbox-controller 2>&1 | Out-Host
        docker compose --profile agent up -d --no-deps security-scanner 2>&1 | Out-Host
        $ErrorActionPreference = $prevEap
        $results["DockerAgentStack"] = ($LASTEXITCODE -eq 0)
    }
    catch {
        $results["DockerAgentStack"] = $false
    }
    finally {
        Pop-Location
    }

    Write-Phase "Phase 4 - Agent stack health probes"
    $healthTargets = @(
        @{ Name = "obscura"; Url = "http://localhost:9222/json/version" },
        @{ Name = "shadow-sync"; Url = "http://localhost:8080/health" },
        @{ Name = "sandbox-controller"; Url = "http://localhost:9090/health" },
        @{ Name = "security-scanner"; Url = "http://localhost:7070/health" },
        @{ Name = "qdrant"; Url = "http://localhost:6333/healthz" },
        @{ Name = "rust-embeddings"; Url = "http://localhost:50061" }
    )

    $healthOk = $true
    foreach ($target in $healthTargets) {
        $probe = Test-HttpOk $target.Url 5
        if ($target.Name -eq "rust-embeddings") {
            # gRPC port - TCP open is enough
            $probe = try {
                $tcp = New-Object System.Net.Sockets.TcpClient
                $tcp.Connect("localhost", 50061)
                $tcp.Close()
                @{ Ok = $true }
            } catch { @{ Ok = $false; Error = $_.Exception.Message } }
        }
        if (-not $probe.Ok) { $healthOk = $false }
        Write-Host ("  [{0}] {1} {2}" -f $(if ($probe.Ok) { "OK" } else { "FAIL" }), $target.Name, $(if ($probe.Error) { $probe.Error } else { "" })) `
            -ForegroundColor $(if ($probe.Ok) { "Green" } else { "Yellow" })
    }
    $results["AgentStackHealth"] = $healthOk
}

$apiKey = Load-OpenRouterApiKey
if (-not $SkipLiveLlm) {
    Write-Phase "Phase 5 - OpenRouter DeepSeek v4 live ping"
    if (-not $apiKey) {
        Write-Host "  SKIP: OPENROUTER_API_KEY not set (export or add to .env)" -ForegroundColor Yellow
        $results["OpenRouterPing"] = $null
    }
    else {
        $env:OPENROUTER_API_KEY = $apiKey
        $body = @{
            model = $OpenRouterModel
            messages = @(@{ role = "user"; content = "Reply with exactly: LIBR4_OK" })
            max_tokens = 16
            temperature = 0
        } | ConvertTo-Json -Depth 5

        try {
            $resp = Invoke-RestMethod -Method Post `
                -Uri "https://openrouter.ai/api/v1/chat/completions" `
                -Headers @{
                    Authorization = "Bearer $apiKey"
                    "HTTP-Referer" = "https://libr4.local"
                    "X-Title" = "Libr4 Platform Smoke"
                } `
                -ContentType "application/json" `
                -Body $body `
                -TimeoutSec 60
            $text = $resp.choices[0].message.content
            if ([string]::IsNullOrWhiteSpace($text) -and $resp.choices[0].message.reasoning) {
                $text = $resp.choices[0].message.reasoning
            }
            Write-Host "  Model: $OpenRouterModel" -ForegroundColor Gray
            Write-Host "  Response: $text" -ForegroundColor Green
            $normalized = ($text -replace '\s', '').Trim()
            $results["OpenRouterPing"] = ($normalized -match 'LIBR4_OK')
            if (-not $results["OpenRouterPing"]) {
                Write-Host "  WARN: expected LIBR4_OK in response (reasoning models may wrap it)" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  OpenRouter ping failed: $_" -ForegroundColor Red
            $results["OpenRouterPing"] = $false
        }
    }
}

Write-Phase "Phase 6 - Autonomous Host (OpenRouter profile)"
$hostProc = $null
$hostLog = Join-Path $repoRoot ".logs/platform-smoke-host.log"
New-Item -ItemType Directory -Path (Split-Path $hostLog) -Force | Out-Null

if (-not $SkipLiveLlm -and $apiKey) {
    $hostProject = Join-Path $repoRoot "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/Libr4.IDE.AutonomousAppGeneration.Host.csproj"
    $env:LIBR4_HOST_PROFILE = "OpenRouter"
    $env:AutonomousAppGeneration__HostProfile__ActiveProfile = "OpenRouter"
    $env:AI__OpenRouter__ApiKey = $apiKey
    $env:AI__OpenRouter__DefaultModel = $OpenRouterModel
    $env:ProviderCapabilityMatrix__ApiModel = $OpenRouterModel
    $env:AutonomousAppGeneration__AgentStack__EnableQdrantGate = "false"
    $env:AutonomousAppGeneration__AgentStack__RequireHealthyBeforeRun = "false"
    $env:AutonomousAppGeneration__Budget__PerRunTokenCap = "2000000"
    $env:AutonomousAppGeneration__Budget__StageCaps__fixing__TokenCap = "800000"
    $env:AutonomousAppGeneration__Budget__StageCaps__generation__TokenCap = "800000"
    $env:AutonomousAppGeneration__MultiAgent__RequiredManifestCoveragePercent = "50"
    $env:AutonomousAppGeneration__BenchmarkMode__EnableBenchmarkMode = "true"
    $env:AutonomousAppGeneration__BenchmarkMode__SkipReviewGate2 = "true"
    $env:AutonomousAppGeneration__BenchmarkMode__DeferManifestCoverageGateFailure = "true"

    $hostOutLog = Join-Path $repoRoot ".logs/platform-smoke-host-out.log"
    $hostErrLog = Join-Path $repoRoot ".logs/platform-smoke-host-err.log"

    Stop-ProcessOnPort 5199

    $hostProc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run","--project",$hostProject,"--configuration","Release","--no-build","--launch-profile","OpenRouter" `
        -RedirectStandardOutput $hostOutLog `
        -RedirectStandardError $hostErrLog `
        -PassThru -WindowStyle Hidden

    Write-Host "  Host PID $($hostProc.Id), logs: $hostOutLog / $hostErrLog"
    Start-Sleep -Seconds 15

    $hostHealth = Test-HttpOk "$HostUrl/swagger/index.html" 15
    $agentStackHealth = Test-HttpOk "$HostUrl/health/agent-stack" 15
    Write-Host "  Host swagger: $($hostHealth.Ok)" -ForegroundColor $(if ($hostHealth.Ok) { "Green" } else { "Yellow" })
    Write-Host "  Agent-stack health: $($agentStackHealth.Ok)" -ForegroundColor $(if ($agentStackHealth.Ok) { "Green" } else { "Yellow" })
    $results["AutonomousHost"] = $hostHealth.Ok

    if ($hostHealth.Ok) {
        Write-Phase "Phase 7 - Live mini-generation (OpenRouter, maxIter=$GenerationMaxIterations)"
        $genBody = @{
            userRequest = "Create a minimal Python FastAPI app with one GET /health endpoint returning {status: ok}. Single file main.py only."
            maxIterations = $GenerationMaxIterations
            triggerSource = "platform-smoke"
        } | ConvertTo-Json

        try {
            $start = Invoke-RestMethod -Method Post `
                -Uri "$HostUrl/api/ide/app-generation/start" `
                -ContentType "application/json" `
                -Body $genBody `
                -TimeoutSec 30

            $runId = $start.id
            if (-not $runId) {
                Write-Host "  Run accepted async: $($start.message)" -ForegroundColor Yellow
                $resolveDeadline = (Get-Date).AddMinutes(3)
                do {
                    Start-Sleep -Seconds 5
                    $list = @(Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/list" -TimeoutSec 30)
                    if ($list.Count -gt 0) {
                        $runId = $list[0].id
                        if ($runId) { break }
                    }
                } while ((Get-Date) -lt $resolveDeadline)
            }

            if (-not $runId) {
                Write-Host "  Could not resolve run id from list" -ForegroundColor Red
                $results["LiveGeneration"] = $false
            }
            else {
                Write-Host "  Started run $runId" -ForegroundColor Green

                # First-run workspace trust blocks planning until the user resolves the prompt.
                try {
                    $trust = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$runId/workspace-trust" -TimeoutSec 15
                    if ($trust.awaitingPrompt -and $trust.pendingPrompt) {
                        Write-Host "  Resolving workspace trust (smoke auto-approve)..." -ForegroundColor Yellow
                        $trustBody = @{
                            promptId = $trust.pendingPrompt.promptId
                            sandboxPolicy = "Standard"
                            hostMode = "CloudAllowed"
                            rememberChoice = $true
                        } | ConvertTo-Json
                        Invoke-RestMethod -Method Post `
                            -Uri "$HostUrl/api/ide/app-generation/$runId/workspace-trust/resolve" `
                            -ContentType "application/json" `
                            -Body $trustBody `
                            -TimeoutSec 15 | Out-Null
                        Write-Host "  Workspace trust resolved" -ForegroundColor Green
                    }
                }
                catch {
                    Write-Host "  Workspace trust check skipped: $_" -ForegroundColor Gray
                }

                $deadline = (Get-Date).AddMinutes($GenerationTimeoutMinutes)
                $terminal = $false
                $lastStatus = ""
                do {
                    Start-Sleep -Seconds 20
                    $report = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$runId" -TimeoutSec 30
                    $runState = $null
                    try {
                        $runState = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$runId/state" -TimeoutSec 5
                    }
                    catch { }

                    if ($report.status -ne $lastStatus) {
                        Write-Host "  >> stage transition: $lastStatus -> $($report.status)" -ForegroundColor Magenta
                        $lastStatus = $report.status
                    }

                    $trustHint = ""
                    $trustGate = $report.qualityGates | Where-Object { $_.stage -eq "workspace_trust" -and -not $_.passed } | Select-Object -First 1
                    if ($trustGate -and ($trustGate.reasons -contains "awaiting_prompt")) {
                        Write-GenerationProgressLine $report $runState
                        Write-Host "    [awaiting workspace trust]" -ForegroundColor Yellow
                    }
                    else {
                        Write-GenerationProgressLine $report $runState
                    }

                    $terminal = $report.status -in @("Completed", "Failed", "Cancelled")
                } while (-not $terminal -and (Get-Date) -lt $deadline)

                Write-GenerationPipelineAudit -HostUrl $HostUrl -RunId $runId

                $results["LiveGeneration"] = ($report.status -eq "Completed")
                if ($report.status -eq "Completed") {
                    Write-Host "  Live generation COMPLETED ($($report.fileCount) files, $($report.iterations.Count) iterations)" -ForegroundColor Green
                }
                else {
                    Write-Host "  Final status: $($report.status) reason=$($report.failureReason)" -ForegroundColor Yellow
                    $reachedTesting = ($report.status -in @("Testing", "Fixing", "Completed")) -or
                        (@($report.iterations).Count -gt 0) -or
                        (@($report.manifest.commands).Count -gt 0)
                    if ($reachedTesting -and $report.fileCount -gt 0) {
                        Write-Host "  NOTE: pipeline exercised (gen/test/repair) but did not reach Completed" -ForegroundColor Yellow
                    }
                }
            }
        }
        catch {
            Write-Host "  Generation start failed: $_" -ForegroundColor Red
            $results["LiveGeneration"] = $false
        }

        Write-Phase "Phase 8 - Platform API surface (host modules)"
        $apiChecks = @(
            @{ Name = "health-agent-stack"; Url = "$HostUrl/health/agent-stack" },
            @{ Name = "health-obscura"; Url = "$HostUrl/health/obscura" },
            @{ Name = "host-profile"; Url = "$HostUrl/api/ide/app-generation/host-profile" },
            @{ Name = "agent-backends"; Url = "$HostUrl/api/ide/app-generation/agent-backends" },
            @{ Name = "extensions"; Url = "$HostUrl/api/ide/app-generation/extensions" },
            @{ Name = "slash-commands"; Url = "$HostUrl/api/ide/app-generation/slash-commands" },
            @{ Name = "runs-health"; Url = "$HostUrl/api/ide/app-generation/runs/health" },
            @{ Name = "runtime-diagnostics"; Url = "$HostUrl/api/ide/app-generation/runtime/diagnostics" },
            @{ Name = "dashboard-readiness"; Url = "$HostUrl/api/ide/app-generation/dashboard/readiness" },
            @{ Name = "eval-benchmarks"; Url = "$HostUrl/api/ide/app-generation/evaluation/benchmarks" },
            @{ Name = "eval-regression-gate"; Url = "$HostUrl/api/ide/app-generation/evaluation/regression-gate"; Method = "POST" },
            @{ Name = "runs-list"; Url = "$HostUrl/api/ide/app-generation/list" },
            @{ Name = "mcp-tools"; Url = "$HostUrl/api/ide/app-generation/mcp/tools" },
            @{ Name = "mcp-host-catalog"; Url = "$HostUrl/api/ide/app-generation/mcp/host/catalog" },
            @{ Name = "memory-consolidation-stats"; Url = "$HostUrl/api/ide/memory/consolidation/stats" }
        )
        $apiOk = $true
        foreach ($api in $apiChecks) {
            try {
                if ($api.Method -eq "POST") {
                    Invoke-RestMethod -Method Post -Uri $api.Url -TimeoutSec 30 | Out-Null
                }
                else {
                    Invoke-RestMethod -Uri $api.Url -TimeoutSec 30 | Out-Null
                }
                Write-Host "  [OK] $($api.Name)" -ForegroundColor Green
            }
            catch {
                $apiOk = $false
                Write-Host "  [FAIL] $($api.Name): $_" -ForegroundColor Red
            }
        }
        $results["PlatformApis"] = $apiOk
    }
}
else {
    Write-Host "  SKIP live host/generation (no OPENROUTER_API_KEY)" -ForegroundColor Yellow
    $results["AutonomousHost"] = $null
    $results["LiveGeneration"] = $null
    $results["PlatformApis"] = $null
}

if ($hostProc -and -not $hostProc.HasExited) {
    Write-Host "`nStopping autonomous host PID $($hostProc.Id)..." -ForegroundColor Gray
    Stop-Process -Id $hostProc.Id -Force -ErrorAction SilentlyContinue
}

Write-Phase "SUMMARY"
$fail = 0; $pass = 0; $skip = 0
foreach ($kv in $results.GetEnumerator()) {
    $icon = if ($null -eq $kv.Value) { "SKIP" } elseif ($kv.Value) { "PASS" } else { "FAIL" }
    $color = switch ($icon) { "PASS" { "Green" } "FAIL" { "Red" } default { "Yellow" } }
    Write-Host "  $icon $($kv.Key)" -ForegroundColor $color
    switch ($icon) {
        "PASS" { $pass++ }
        "FAIL" { $fail++ }
        "SKIP" { $skip++ }
    }
}

Write-Host "`nPassed: $pass | Failed: $fail | Skipped: $skip" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
exit 0
