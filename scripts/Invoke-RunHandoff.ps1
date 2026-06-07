param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("export", "import", "cleanup")]
    [string] $Command,
    [string] $RunId,
    [string] $Bundle,
    [string] $Output,
    [string] $ApiBase,
    [string] $RunsRoot = ".logs/runs",
    [string] $ExportRoot = ".logs/run-exports",
    [string] $SessionDb = ".logs/agent-sessions.db",
    [int] $RetentionDays = 7
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$cliProject = Join-Path $repoRoot "src/Tools/Libr4.RunHandoff.Cli/Libr4.RunHandoff.Cli.csproj"

if (-not (Test-Path $cliProject)) {
    throw "CLI project not found: $cliProject"
}

$argsList = @($Command)
if ($RunId) { $argsList += @("--run-id", $RunId) }
if ($Bundle) { $argsList += @("--bundle", $Bundle) }
if ($Output) { $argsList += @("--output", $Output) }
if ($ApiBase) { $argsList += @("--api-base", $ApiBase) }
if ($RunsRoot) { $argsList += @("--runs-root", $RunsRoot) }
if ($ExportRoot) { $argsList += @("--export-root", $ExportRoot) }
if ($SessionDb) { $argsList += @("--session-db", $SessionDb) }
if ($RetentionDays -gt 0) { $argsList += @("--retention-days", "$RetentionDays") }

Push-Location $repoRoot
try {
    dotnet run --project $cliProject -- @argsList
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
