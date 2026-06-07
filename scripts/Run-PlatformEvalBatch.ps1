param(
    [string]$HostUrl = "http://localhost:5199",
    [string]$ScenariosDir = "",
    [string]$ScenarioFilter = "",
    [string]$BatchId = "",
    [string]$OutputDir = "",
    [string]$OpenRouterModel = "deepseek/deepseek-v4-flash",
    [int]$RunsOverride = 0,
    [int]$GenerationTimeoutMinutes = 30,
    [int]$GenerationMaxIterations = 0,
    [switch]$SkipHostStart,
    [switch]$SkipBuild,
    [switch]$DryRun,
    [switch]$KeepHostRunning
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ScenariosDir) { $ScenariosDir = Join-Path $repoRoot "Evaluation/scenarios" }
if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot ".logs/platform-eval" }
if (-not $BatchId) { $BatchId = "eval-" + (Get-Date -Format "yyyyMMdd-HHmmss") }

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$DeterministicMechanisms = @(
    "DeterministicStructural",
    "DeterministicRuntime",
    "DeterministicCompile",
    "PatternRecovery",
    "DeepStackHandler"
)
$LlmMechanisms = @("Llm", "SurgicalLlm", "AgentToolLoop")

function Write-Phase([string]$Title) {
    Write-Host "`n=== $Title ===" -ForegroundColor Cyan
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
    foreach ($procId in ($conns | Select-Object -ExpandProperty OwningProcess -Unique)) {
        if ($procId -le 0) { continue }
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if (-not $proc) { continue }
        Write-Host "  Stopping $($proc.ProcessName) (PID $procId) on port $Port" -ForegroundColor Yellow
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }
    if ($conns.Count -gt 0) { Start-Sleep -Seconds 2 }
}

function Import-PlatformEvalScenario([string]$Path) {
    $text = Get-Content $Path -Raw
    $scenario = [ordered]@{
        id = $null
        displayName = $null
        runs = 10
        maxIterations = 10
        expectedRecipeId = $null
        tags = @()
        userRequest = $null
        sourceFile = (Split-Path $Path -Leaf)
    }

    $lines = $text -split "`r?`n"
    $i = 0
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        if ($line -match '^\s*id:\s*(.+)\s*$') { $scenario.id = $Matches[1].Trim(); $i++; continue }
        if ($line -match '^\s*displayName:\s*(.+)\s*$') { $scenario.displayName = $Matches[1].Trim(); $i++; continue }
        if ($line -match '^\s*runs:\s*(\d+)\s*$') { $scenario.runs = [int]$Matches[1]; $i++; continue }
        if ($line -match '^\s*maxIterations:\s*(\d+)\s*$') { $scenario.maxIterations = [int]$Matches[1]; $i++; continue }
        if ($line -match '^\s*expectedRecipeId:\s*(.+)\s*$') {
            $val = $Matches[1].Trim()
            if ($val -notin @("null", "~", "")) { $scenario.expectedRecipeId = $val }
            $i++; continue
        }
        if ($line -match '^\s*tags:\s*$') {
            $i++
            while ($i -lt $lines.Count -and $lines[$i] -match '^\s*-\s*(.+)\s*$') {
                $scenario.tags += $Matches[1].Trim()
                $i++
            }
            continue
        }
        if ($line -match '^\s*userRequest:\s*\|\s*$') {
            $i++
            $block = @()
            while ($i -lt $lines.Count) {
                if ($lines[$i] -match '^\S') { break }
                if ($lines[$i] -match '^\s{2,}(.*)$') { $block += $Matches[1] }
                elseif ([string]::IsNullOrWhiteSpace($lines[$i])) { $block += "" }
                $i++
            }
            $scenario.userRequest = ($block -join "`n").Trim()
            continue
        }
        $i++
    }

    if (-not $scenario.id -or -not $scenario.userRequest) {
        throw "Invalid scenario file (missing id or userRequest): $Path"
    }
    if (-not $scenario.displayName) { $scenario.displayName = $scenario.id }
    return [pscustomobject]$scenario
}

function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) { return $null }
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 1) { return $sorted[0] }
    $rank = ($Percentile / 100.0) * ($sorted.Count - 1)
    $low = [Math]::Floor($rank)
    $high = [Math]::Ceiling($rank)
    if ($low -eq $high) { return $sorted[$low] }
    $weight = $rank - $low
    return $sorted[$low] * (1 - $weight) + $sorted[$high] * $weight
}

function Get-ErrorSignatureKey($event) {
    if ($event.errorSignature) { return "$($event.errorSignature)".Trim().ToLowerInvariant() }
    if ($event.primaryErrorClass) { return "class:$($event.primaryErrorClass)".ToLowerInvariant() }
    return $null
}

function Measure-LlmRegression([object[]]$RecoveryEvents) {
    $regressions = @()
    if (-not $RecoveryEvents -or $RecoveryEvents.Count -eq 0) {
        return @{
            Count = 0
            Rate = 0.0
            Events = @()
            DeterministicFixCount = 0
        }
    }

    $ordered = @($RecoveryEvents | Sort-Object iteration, attemptedAtUtc)
    $deterministicFixes = @()

    for ($i = 0; $i -lt $ordered.Count; $i++) {
        $evt = $ordered[$i]
        $mechanism = "$($evt.recoveredBy)"
        if ($DeterministicMechanisms -notcontains $mechanism) { continue }

        $sig = Get-ErrorSignatureKey $evt
        if (-not $sig) { continue }

        $deterministicFixes += $evt
        for ($j = $i + 1; $j -lt $ordered.Count; $j++) {
            $next = $ordered[$j]
            if ($LlmMechanisms -notcontains "$($next.recoveredBy)") { continue }
            if ($next.iteration -lt $evt.iteration) { continue }

            $nextSig = Get-ErrorSignatureKey $next
            if ($nextSig -and $nextSig -eq $sig) {
                $regressions += [ordered]@{
                    deterministicIteration = $evt.iteration
                    llmIteration = $next.iteration
                    signature = $sig
                    deterministicMechanism = $mechanism
                    llmMechanism = "$($next.recoveredBy)"
                }
                break
            }
        }
    }

    $denom = [Math]::Max(1, $deterministicFixes.Count)
    return @{
        Count = $regressions.Count
        Rate = [Math]::Round($regressions.Count / $denom, 4)
        Events = $regressions
        DeterministicFixCount = $deterministicFixes.Count
    }
}

function Test-VerifyPassed($qualityGates) {
    if (-not $qualityGates) { return $false }
    $verifyGates = @($qualityGates | Where-Object { "$($_.stage)".ToLowerInvariant() -like "*verify*" })
    if ($verifyGates.Count -eq 0) { return $null }
    return ($verifyGates | Where-Object { $_.passed -eq $true }).Count -gt 0
}

function Get-ModelAssistedShare($recovery) {
    if (-not $recovery) { return 0.0 }
    if ($recovery.PSObject.Properties.Name -contains "modelAssistedAttemptShare") {
        return [double]$recovery.modelAssistedAttemptShare
    }
    if ($recovery.PSObject.Properties.Name -contains "llmAttemptShare") {
        return [double]$recovery.llmAttemptShare
    }
    return 0.0
}

function Get-PlatformJitStats([string]$RunId) {
    $candidates = @(
        (Join-Path $repoRoot ".logs/runs/$RunId/platform-jit-audit.jsonl"),
        (Join-Path $repoRoot "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/.logs/runs/$RunId/platform-jit-audit.jsonl")
    )
    $path = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $path) {
        return @{
            jitInjectedCount = 0
            jitResolvedCount = 0
            jitResolvedWithinNext = 0
            jitPlaybooks = @()
            jitResolveRate = $null
        }
    }

    $events = @()
    foreach ($line in Get-Content $path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try { $events += ($line | ConvertFrom-Json) } catch { }
    }

    $injected = @($events | Where-Object { "$($_.event)" -eq "injected" })
    $resolved = @($events | Where-Object { "$($_.event)" -eq "resolved" })
    $withinNext = @($resolved | Where-Object { $_.resolvedWithinNextIteration -eq $true })

    return @{
        jitInjectedCount = $injected.Count
        jitResolvedCount = $resolved.Count
        jitResolvedWithinNext = $withinNext.Count
        jitPlaybooks = @($injected | ForEach-Object { "$($_.playbookId)" } | Select-Object -Unique)
        jitResolveRate = if ($injected.Count -gt 0) {
            [Math]::Round($resolved.Count / $injected.Count, 4)
        } else { $null }
    }
}

function Export-RunMetrics {
    param(
        [string]$ScenarioId,
        [string]$ScenarioName,
        [int]$RunIndex,
        [string]$RunId,
        [datetime]$StartedAt,
        [object]$Report,
        [object]$Dashboard,
        [object]$Usage
    )

    $completedAt = if ($Report.completedAt) { [datetime]$Report.completedAt } else { Get-Date }
    $durationMin = if ($Report.startedAt) {
        ([datetime]$Report.completedAt - [datetime]$Report.startedAt).TotalMinutes
    } else {
        ($completedAt - $StartedAt).TotalMinutes
    }

    $iterations = @($Report.iterations)
    $iterCount = $iterations.Count
    $recovery = $Dashboard.recoveryEfficiency
    $recoveryEvents = @()
    if ($recovery -and $recovery.events) { $recoveryEvents = @($recovery.events) }

    $llmRegression = Measure-LlmRegression $recoveryEvents
    $verifyPassed = Test-VerifyPassed $Report.qualityGates
    $jitStats = Get-PlatformJitStats $RunId
    $platformUtilization = $null
    if ($Report.qualityGates) {
        $puGate = @($Report.qualityGates | Where-Object { "$($_.stage)" -eq "platform_utilization" } | Select-Object -First 1)
        if ($puGate) { $platformUtilization = @($puGate.reasons) }
    }
    $modelAssistedShare = Get-ModelAssistedShare $recovery
    $repairRequired = ($iterCount -gt 1) -or ($recoveryEvents.Count -gt 0) -or
        ($iterations | Where-Object { $_.appliedFixes -and $_.appliedFixes.Count -gt 0 }).Count -gt 0

    $passedGates = 0
    $totalGates = 0
    if ($Report.qualityGates) {
        $totalGates = @($Report.qualityGates).Count
        $passedGates = (@($Report.qualityGates | Where-Object { $_.passed -eq $true })).Count
    }
    if ($Dashboard.summary) {
        if ($Dashboard.summary.totalGates -gt 0) {
            $totalGates = $Dashboard.summary.totalGates
            $passedGates = $Dashboard.summary.passedGates
        }
    }

    return [ordered]@{
        batchId = $BatchId
        scenarioId = $ScenarioId
        scenarioName = $ScenarioName
        runIndex = $RunIndex
        runId = $RunId
        startedAtUtc = $StartedAt.ToUniversalTime().ToString("o")
        completedAtUtc = $completedAt.ToUniversalTime().ToString("o")
        durationMinutes = [Math]::Round($durationMin, 2)
        status = "$($Report.status)"
        success = ($Report.status -eq "Completed")
        failureReason = $Report.failureReason
        iterationsToComplete = $iterCount
        maxIterations = if ($Report.plan) { $Report.plan.maxIterations } else { $null }
        repairRequired = $repairRequired
        verifyPassed = $verifyPassed
        qualityGatesPassed = $passedGates
        qualityGatesTotal = $totalGates
        fileCount = $Report.fileCount
        detectedStack = if ($Dashboard.summary) { $Dashboard.summary.detectedStack } else { $null }
        pipelineStageReached = if ($recovery) { $recovery.pipelineStageReached } else { $null }
        recoveryMeasurementEligible = if ($recovery) { $recovery.recoveryMeasurementEligible } else { $false }
        recoveryAttempts = if ($recovery) { $recovery.totalAttempts } else { 0 }
        deterministicAttemptShare = if ($recovery) { $recovery.deterministicAttemptShare } else { 0 }
        llmAttemptShare = if ($recovery) { $recovery.llmAttemptShare } else { 0 }
        modelAssistedAttemptShare = $modelAssistedShare
        recoveryByMechanism = if ($recovery) { $recovery.byMechanism } else { @() }
        llmRecoverySuccessRate = if ($recovery -and $recovery.llmStats) { $recovery.llmStats.successRate } else { $null }
        llmRegressionCount = $llmRegression.Count
        llmRegressionRate = $llmRegression.Rate
        llmRegressionEvents = $llmRegression.Events
        deterministicFixCount = $llmRegression.DeterministicFixCount
        repeatedErrors = if ($recovery) { $recovery.repeatedErrors } else { @() }
        totalTokens = if ($Usage) { $Usage.totalTokens } else { $null }
        costUsd = if ($Usage) { $Usage.costUsd } else { $null }
        llmRequestCount = if ($Usage) { $Usage.llmRequestCount } else { $null }
        jitInjectedCount = $jitStats.jitInjectedCount
        jitResolvedCount = $jitStats.jitResolvedCount
        jitResolvedWithinNext = $jitStats.jitResolvedWithinNext
        jitPlaybooks = $jitStats.jitPlaybooks
        jitResolveRate = $jitStats.jitResolveRate
        platformUtilization = $platformUtilization
    }
}

function Resolve-WorkspaceTrust([string]$BaseHostUrl, [string]$RunId) {
    try {
        $trust = Invoke-RestMethod -Uri "$BaseHostUrl/api/ide/app-generation/$RunId/workspace-trust" -TimeoutSec 15
        if ($trust.awaitingPrompt -and $trust.pendingPrompt) {
            Write-Host "    Resolving workspace trust..." -ForegroundColor Yellow
            $trustBody = @{
                promptId = $trust.pendingPrompt.promptId
                sandboxPolicy = "Standard"
                hostMode = "CloudAllowed"
                rememberChoice = $true
            } | ConvertTo-Json
            Invoke-RestMethod -Method Post `
                -Uri "$BaseHostUrl/api/ide/app-generation/$RunId/workspace-trust/resolve" `
                -ContentType "application/json" `
                -Body $trustBody `
                -TimeoutSec 15 | Out-Null
        }
    }
    catch {
        Write-Host "    Workspace trust skipped: $_" -ForegroundColor Gray
    }
}

function Wait-GenerationRun {
    param(
        [string]$BaseHostUrl,
        [string]$RunId,
        [int]$TimeoutMinutes
    )

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    $lastStatus = ""
    do {
        Start-Sleep -Seconds 20
        $report = Invoke-RestMethod -Uri "$BaseHostUrl/api/ide/app-generation/$RunId" -TimeoutSec 30
        if ($report.status -ne $lastStatus) {
            Write-Host "    status: $lastStatus -> $($report.status)" -ForegroundColor Magenta
            $lastStatus = $report.status
        }
        $iterCount = @($report.iterations).Count
        Write-Host "    $($report.status) files=$($report.fileCount) iter=$iterCount" -ForegroundColor DarkGray
        $terminal = $report.status -in @("Completed", "Failed", "Cancelled")
    } while (-not $terminal -and (Get-Date) -lt $deadline)

    if (-not $terminal) {
        Write-Host "    TIMEOUT after $TimeoutMinutes min (last=$lastStatus)" -ForegroundColor Red
    }
    return $report
}

function Start-AutonomousHostForEval([string]$ApiKey) {
    $hostProject = Join-Path $repoRoot "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/Libr4.IDE.AutonomousAppGeneration.Host.csproj"
    if (-not $SkipBuild) {
        Write-Host "  Building autonomous host (Release)..." -ForegroundColor Gray
        dotnet build $hostProject -c Release --verbosity quiet | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Host build failed" }
    }

    $env:LIBR4_HOST_PROFILE = "OpenRouter"
    $env:AutonomousAppGeneration__HostProfile__ActiveProfile = "OpenRouter"
    $env:AI__OpenRouter__ApiKey = $ApiKey
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
    # Scoped briefing + orchestrator JIT without disabling benchmark repair path (fair A/B vs pilot).
    $env:AutonomousAppGeneration__PlatformUtilization__EnableFullPlatformUtilization = "false"
    $env:AutonomousAppGeneration__PlatformUtilization__InjectCapabilityBriefing = "true"
    $env:AutonomousAppGeneration__PlatformUtilization__CapabilityBriefingMode = "Scoped"
    $env:AutonomousAppGeneration__PlatformUtilization__EnableOrchestratorJitInjection = "true"
    $env:AutonomousAppGeneration__PlatformUtilization__EnableOrchestratorJitLearnedPlaybook = "true"

    $hostOutLog = Join-Path $OutputDir "$BatchId-host-out.log"
    $hostErrLog = Join-Path $OutputDir "$BatchId-host-err.log"
    Stop-ProcessOnPort 5199

    $proc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", $hostProject, "--configuration", "Release", "--no-build", "--launch-profile", "OpenRouter" `
        -RedirectStandardOutput $hostOutLog `
        -RedirectStandardError $hostErrLog `
        -PassThru -WindowStyle Hidden

    Write-Host "  Host PID $($proc.Id) logs: $hostOutLog"
    Start-Sleep -Seconds 15

    $deadline = (Get-Date).AddMinutes(3)
    do {
        try {
            $resp = Invoke-WebRequest -Uri "$HostUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 10
            if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 300) { return $proc }
        }
        catch { Start-Sleep -Seconds 5 }
    } while ((Get-Date) -lt $deadline)

    throw "Autonomous host failed to become healthy at $HostUrl"
}

function Write-FleetSummary([object[]]$RunMetrics, [string]$SummaryPath) {
    $groups = $RunMetrics | Group-Object scenarioId
    $scenarioSummaries = @()
    foreach ($group in $groups) {
        $rows = @($group.Group)
        $successes = @($rows | Where-Object { $_.success -eq $true })
        $iterations = @($successes | ForEach-Object { [double]$_.iterationsToComplete } | Where-Object { $_ -gt 0 })
        $verifyKnown = @($rows | Where-Object { $null -ne $_.verifyPassed })
        $verifyPassed = @($verifyKnown | Where-Object { $_.verifyPassed -eq $true })
        $regressionRates = @($rows | ForEach-Object { [double]$_.llmRegressionRate })
        $costs = @($rows | Where-Object { $null -ne $_.costUsd } | ForEach-Object { [double]$_.costUsd })

        $scenarioSummaries += [ordered]@{
            scenarioId = $group.Name
            scenarioName = ($rows | Select-Object -First 1).scenarioName
            runs = $rows.Count
            successCount = $successes.Count
            successRate = if ($rows.Count -gt 0) { [Math]::Round($successes.Count / $rows.Count, 4) } else { 0 }
            repairRate = if ($rows.Count -gt 0) {
                [Math]::Round((@($rows | Where-Object { $_.repairRequired -eq $true })).Count / $rows.Count, 4)
            } else { 0 }
            verifyPassRate = if ($verifyKnown.Count -gt 0) {
                [Math]::Round($verifyPassed.Count / $verifyKnown.Count, 4)
            } else { $null }
            meanIterationsToComplete = if ($iterations.Count -gt 0) {
                [Math]::Round(($iterations | Measure-Object -Average).Average, 2)
            } else { $null }
            p95IterationsToComplete = Get-Percentile $iterations 95
            meanLlmRegressionRate = if ($regressionRates.Count -gt 0) {
                [Math]::Round(($regressionRates | Measure-Object -Average).Average, 4)
            } else { 0 }
            meanCostUsd = if ($costs.Count -gt 0) {
                [Math]::Round(($costs | Measure-Object -Average).Average, 4)
            } else { $null }
            topRepeatedErrors = @(
                $rows |
                    ForEach-Object { $_.repeatedErrors } |
                    ForEach-Object { $_ } |
                    Group-Object signature |
                    Sort-Object Count -Descending |
                    Select-Object -First 5 |
                    ForEach-Object { @{ signature = $_.Name; count = $_.Count } }
            )
        }
    }

    $allSuccess = @($RunMetrics | Where-Object { $_.success -eq $true })
    $fleet = [ordered]@{
        batchId = $BatchId
        evaluatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        totalRuns = $RunMetrics.Count
        fleetSuccessRate = if ($RunMetrics.Count -gt 0) {
            [Math]::Round($allSuccess.Count / $RunMetrics.Count, 4)
        } else { 0 }
        scenarios = $scenarioSummaries
    }

    $fleet | ConvertTo-Json -Depth 8 | Set-Content -Path $SummaryPath -Encoding UTF8

    Write-Phase "FLEET SUMMARY ($BatchId)"
    Write-Host ("{0,-16} {1,5} {2,7} {3,7} {4,7} {5,8} {6,8}" -f `
        "Scenario", "Runs", "Success", "Repair", "Verify", "MeanIter", "LLMRegr") -ForegroundColor White
    foreach ($s in $scenarioSummaries) {
        $verify = if ($null -eq $s.verifyPassRate) { "n/a" } else { "{0:P0}" -f $s.verifyPassRate }
        Write-Host ("{0,-16} {1,5} {2,7} {3,7} {4,7} {5,8} {6,8}" -f `
            $s.scenarioId,
            $s.runs,
            ("{0:P0}" -f $s.successRate),
            ("{0:P0}" -f $s.repairRate),
            $verify,
            ($(if ($null -eq $s.meanIterationsToComplete) { "n/a" } else { $s.meanIterationsToComplete })),
            ("{0:P1}" -f $s.meanLlmRegressionRate)) -ForegroundColor Cyan
    }
    Write-Host "`nSummary JSON: $SummaryPath" -ForegroundColor Gray
    return $fleet
}

# --- Main ---

Write-Phase "Platform Eval Batch ($BatchId)"
Write-Host "Scenarios dir: $ScenariosDir"
Write-Host "Output dir:    $OutputDir"

$scenarioFiles = @(Get-ChildItem -Path $ScenariosDir -Filter "*.yaml" | Sort-Object Name)
if ($scenarioFiles.Count -eq 0) { throw "No scenario YAML files in $ScenariosDir" }

$scenarios = @($scenarioFiles | ForEach-Object { Import-PlatformEvalScenario $_.FullName })
if ($ScenarioFilter) {
    $filterIds = @($ScenarioFilter.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $scenarios = @($scenarios | Where-Object { $filterIds -contains $_.id })
}
if ($scenarios.Count -eq 0) { throw "No scenarios matched filter: $ScenarioFilter" }

$runPlan = @()
foreach ($scenario in $scenarios) {
    $runs = if ($RunsOverride -gt 0) { $RunsOverride } else { $scenario.runs }
    $maxIter = if ($GenerationMaxIterations -gt 0) { $GenerationMaxIterations } else { $scenario.maxIterations }
    for ($r = 1; $r -le $runs; $r++) {
        $runPlan += [pscustomobject]@{
            Scenario = $scenario
            RunIndex = $r
            TotalRuns = $runs
            MaxIterations = $maxIter
        }
    }
}

Write-Host "Planned runs: $($runPlan.Count) across $($scenarios.Count) scenario(s)" -ForegroundColor Green
foreach ($s in $scenarios) {
    $runs = if ($RunsOverride -gt 0) { $RunsOverride } else { $s.runs }
    Write-Host "  - $($s.id): $runs runs" -ForegroundColor Gray
}

if ($DryRun) {
    Write-Host "`nDryRun: exiting without execution." -ForegroundColor Yellow
    $runPlan | Select-Object @{n="scenarioId";e={$_.Scenario.id}}, RunIndex, MaxIterations | Format-Table
    exit 0
}

$apiKey = Load-OpenRouterApiKey
if (-not $apiKey) { throw "OPENROUTER_API_KEY not set (.env or environment)" }
$env:OPENROUTER_API_KEY = $apiKey

$jsonlPath = Join-Path $OutputDir "$BatchId-runs.jsonl"
$summaryPath = Join-Path $OutputDir "$BatchId-summary.json"
$logPath = Join-Path $OutputDir "$BatchId.log"
Start-Transcript -Path $logPath -Append | Out-Null

$hostProc = $null
if (-not $SkipHostStart) {
    Write-Phase "Starting autonomous host"
    $hostProc = Start-AutonomousHostForEval $apiKey
}
else {
    Write-Host "SkipHostStart: assuming host already at $HostUrl" -ForegroundColor Yellow
}

$allMetrics = @()
$failedRuns = 0

try {
    Write-Phase "Executing $($runPlan.Count) runs"
    foreach ($item in $runPlan) {
        $scenario = $item.Scenario
        Write-Host "`n[$($scenario.id)] run $($item.RunIndex)/$($item.TotalRuns)" -ForegroundColor Cyan

        $genBody = @{
            userRequest = $scenario.userRequest
            maxIterations = $item.MaxIterations
            triggerSource = "platform-eval-batch"
        } | ConvertTo-Json -Depth 4

        $startedAt = Get-Date
        try {
            $start = Invoke-RestMethod -Method Post `
                -Uri "$HostUrl/api/ide/app-generation/start" `
                -ContentType "application/json" `
                -Body $genBody `
                -TimeoutSec 60

            $runId = $start.id
            if (-not $runId) {
                Write-Host "  Async start, resolving run id..." -ForegroundColor Yellow
                $resolveDeadline = (Get-Date).AddMinutes(3)
                do {
                    Start-Sleep -Seconds 5
                    $list = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/list" -TimeoutSec 30
                    $latest = @($list | Sort-Object { [datetime]$_.startedAt } -Descending | Select-Object -First 1)
                    if ($latest -and $latest[0].id) { $runId = $latest[0].id }
                } while (-not $runId -and (Get-Date) -lt $resolveDeadline)
            }

            if (-not $runId) { throw "Could not resolve run id after start" }
            Write-Host "  RunId: $runId" -ForegroundColor Green

            Resolve-WorkspaceTrust $HostUrl $runId
            $report = Wait-GenerationRun -BaseHostUrl $HostUrl -RunId $runId -TimeoutMinutes $GenerationTimeoutMinutes

            $dashboard = $null
            $usage = $null
            try { $dashboard = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$runId/dashboard/build" -TimeoutSec 30 }
            catch { Write-Host "  Dashboard fetch failed: $_" -ForegroundColor Yellow }
            try { $usage = Invoke-RestMethod -Uri "$HostUrl/api/ide/app-generation/$runId/usage" -TimeoutSec 15 }
            catch { Write-Host "  Usage fetch failed: $_" -ForegroundColor Yellow }

            $metrics = Export-RunMetrics `
                -ScenarioId $scenario.id `
                -ScenarioName $scenario.displayName `
                -RunIndex $item.RunIndex `
                -RunId $runId `
                -StartedAt $startedAt `
                -Report $report `
                -Dashboard $dashboard `
                -Usage $usage

            $metrics | ConvertTo-Json -Depth 8 -Compress | Add-Content -Path $jsonlPath -Encoding UTF8
            $allMetrics += [pscustomobject]$metrics

            $icon = if ($metrics.success) { "PASS" } else { "FAIL" }
            $color = if ($metrics.success) { "Green" } else { "Red" }
            Write-Host "  $icon status=$($metrics.status) iter=$($metrics.iterationsToComplete) llmRegr=$($metrics.llmRegressionCount)" -ForegroundColor $color
            if (-not $metrics.success) { $failedRuns++ }
        }
        catch {
            Write-Host "  RUN ERROR: $_" -ForegroundColor Red
            $failedRuns++
            $errRecord = [ordered]@{
                batchId = $BatchId
                scenarioId = $scenario.id
                runIndex = $item.RunIndex
                success = $false
                error = "$_"
                startedAtUtc = $startedAt.ToUniversalTime().ToString("o")
            }
            $errRecord | ConvertTo-Json -Compress | Add-Content -Path $jsonlPath -Encoding UTF8
            $allMetrics += [pscustomobject]$errRecord
        }
    }
}
finally {
    Stop-Transcript | Out-Null
    if ($hostProc -and -not $KeepHostRunning -and -not $hostProc.HasExited) {
        Write-Host "`nStopping host PID $($hostProc.Id)..." -ForegroundColor Gray
        Stop-Process -Id $hostProc.Id -Force -ErrorAction SilentlyContinue
    }
}

if ($allMetrics.Count -gt 0) {
    Write-FleetSummary -RunMetrics $allMetrics -SummaryPath $summaryPath | Out-Null
}

Write-Host "`nJSONL: $jsonlPath" -ForegroundColor Cyan
Write-Host "Log:   $logPath" -ForegroundColor Cyan
Write-Host "Failed runs (incl. errors): $failedRuns / $($runPlan.Count)" -ForegroundColor $(if ($failedRuns -gt 0) { "Yellow" } else { "Green" })

if ($failedRuns -gt 0) { exit 1 }
exit 0
