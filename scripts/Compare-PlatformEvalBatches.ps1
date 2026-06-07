param(
    [string]$BaselineSummary = "",
    [string]$CandidateSummary = "",
    [string]$BaselineLabel = "baseline",
    [string]$CandidateLabel = "candidate"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $BaselineSummary) {
    $BaselineSummary = Join-Path $repoRoot ".logs/platform-eval/pilot-fastapi-crm-x2-summary.json"
}
if (-not $CandidateSummary) {
    throw "CandidateSummary required"
}

function Read-Summary([string]$Path) {
    if (-not (Test-Path $Path)) { throw "Summary not found: $Path" }
    return Get-Content $Path -Raw | ConvertFrom-Json
}

function Read-RunsJsonl([string]$SummaryPath) {
    $batchId = (Read-Summary $SummaryPath).batchId
    $jsonl = Join-Path (Split-Path $SummaryPath -Parent) "$batchId-runs.jsonl"
    if (-not (Test-Path $jsonl)) { return @() }
    $rows = @()
    foreach ($line in Get-Content $jsonl) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $rows += ($line | ConvertFrom-Json)
    }
    return $rows
}

function Summarize-Runs([object[]]$Runs) {
    $ok = @($Runs | Where-Object { $_.success -eq $true })
    $costs = @($Runs | Where-Object { $null -ne $_.costUsd } | ForEach-Object { [double]$_.costUsd })
    $iters = @($ok | ForEach-Object { [double]$_.iterationsToComplete })
    $regr = @($Runs | ForEach-Object { [double]$_.llmRegressionRate })
    $jitInj = 0
    $jitRes = 0
    foreach ($r in $Runs) {
        if ($null -ne $r.PSObject.Properties['jitInjectedCount']) { $jitInj += [int]$r.jitInjectedCount }
        if ($null -ne $r.PSObject.Properties['jitResolvedCount']) { $jitRes += [int]$r.jitResolvedCount }
    }

    return [ordered]@{
        runs = $Runs.Count
        success = $ok.Count
        successRate = if ($Runs.Count -gt 0) { [Math]::Round($ok.Count / $Runs.Count, 4) } else { 0 }
        meanCostUsd = if ($costs.Count -gt 0) { [Math]::Round(($costs | Measure-Object -Average).Average, 4) } else { $null }
        meanIterations = if ($iters.Count -gt 0) { [Math]::Round(($iters | Measure-Object -Average).Average, 2) } else { $null }
        meanLlmRegressionRate = if ($regr.Count -gt 0) { [Math]::Round(($regr | Measure-Object -Average).Average, 4) } else { 0 }
        totalJitInjected = [int]$jitInj
        totalJitResolved = [int]$jitRes
        jitPlaybooks = @($Runs | ForEach-Object { $_.jitPlaybooks } | ForEach-Object { $_ } | Select-Object -Unique)
    }
}

$baseline = Read-Summary $BaselineSummary
$candidate = Read-Summary $CandidateSummary
$baselineRuns = Read-RunsJsonl $BaselineSummary
$candidateRuns = Read-RunsJsonl $CandidateSummary
$b = Summarize-Runs $baselineRuns
$c = Summarize-Runs $candidateRuns

Write-Host "`n=== Platform Eval Comparison ===" -ForegroundColor Cyan
Write-Host ("{0,-28} {1,14} {2,14} {3,10}" -f "Metric", $BaselineLabel, $CandidateLabel, "Delta") -ForegroundColor White
Write-Host ("{0,-28} {1,14} {2,14} {3,10}" -f "BatchId", $baseline.batchId, $candidate.batchId, "") -ForegroundColor Gray

function Row([string]$Name, $Base, $Cand, [string]$Fmt = "{0}") {
    $delta = ""
    if ($Base -is [double] -or $Base -is [int]) {
        $d = [double]$Cand - [double]$Base
        $delta = if ($d -gt 0) { "+$d" } else { "$d" }
    }
    $bStr = if ($null -eq $Base) { "n/a" } else { $Fmt -f $Base }
    $cStr = if ($null -eq $Cand) { "n/a" } else { $Fmt -f $Cand }
    Write-Host ("{0,-28} {1,14} {2,14} {3,10}" -f $Name, $bStr, $cStr, $delta)
}

Row "Success rate" $b.successRate $c.successRate "{0:P0}"
Row "Completed runs" $b.success $c.success "{0}"
Row "Mean cost USD" $b.meanCostUsd $c.meanCostUsd "`${0:F4}"
Row "Mean iterations (ok)" $b.meanIterations $c.meanIterations "{0}"
Row "Mean LLM regression" $b.meanLlmRegressionRate $c.meanLlmRegressionRate "{0:P1}"
Row "JIT injections (total)" $b.totalJitInjected $c.totalJitInjected "{0}"
Row "JIT resolved (total)" $b.totalJitResolved $c.totalJitResolved "{0}"

Write-Host "`nPer-run candidate:" -ForegroundColor Cyan
foreach ($r in $candidateRuns) {
    $jit = if ($r.jitInjectedCount -gt 0) { "jit=$($r.jitInjectedCount)/$($r.jitResolvedCount) [$($r.jitPlaybooks -join ',')]" } else { "jit=0" }
    $pu = if ($r.platformUtilization) { ($r.platformUtilization -join ";") } else { "-" }
    Write-Host ("  run{0}: {1} iter={2} cost=`${3} {4} platform=[{5}]" -f `
        $r.runIndex, $r.status, $r.iterationsToComplete, $r.costUsd, $jit, $pu) -ForegroundColor $(if ($r.success) { "Green" } else { "Yellow" })
    if ($r.failureReason) { Write-Host "    fail: $($r.failureReason)" -ForegroundColor DarkYellow }
}

Write-Host "`nBaseline top errors:" -ForegroundColor Gray
foreach ($e in $baseline.scenarios[0].topRepeatedErrors | Select-Object -First 3) {
    Write-Host "  - $($e.signature.Substring(0, [Math]::Min(120, $e.signature.Length)))..." -ForegroundColor DarkGray
}
