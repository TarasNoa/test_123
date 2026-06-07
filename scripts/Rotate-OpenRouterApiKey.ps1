param(
    [string]$OldKeyPrefix = "sk-or-v1-b9bc66b0",
    [string]$NewKeyName = "libr4-platform-rotated",
    [switch]$DeleteOld,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot ".env"

function Get-ManagementKey {
    if ($env:OPENROUTER_MANAGEMENT_API_KEY) { return $env:OPENROUTER_MANAGEMENT_API_KEY.Trim() }
    if (Test-Path $envFile) {
        foreach ($line in Get-Content $envFile) {
            if ($line -match '^\s*OPENROUTER_MANAGEMENT_API_KEY\s*=\s*(.+)\s*$') {
                $k = $Matches[1].Trim().Trim('"').Trim("'")
                if ($k) { return $k }
            }
        }
    }
    return $null
}

function Invoke-OpenRouterManagement {
    param([string]$Method, [string]$Path, [object]$Body = $null)
    $mgmt = Get-ManagementKey
    if (-not $mgmt) {
        throw @"
OPENROUTER_MANAGEMENT_API_KEY not set.
1. Open https://openrouter.ai/settings/management-keys
2. Create a Management API key
3. Set: `$env:OPENROUTER_MANAGEMENT_API_KEY = 'sk-or-mgmt-...'
   Or add OPENROUTER_MANAGEMENT_API_KEY=... to libr4/.env
4. Re-run: powershell -ExecutionPolicy Bypass -File scripts\Rotate-OpenRouterApiKey.ps1 -DeleteOld
"@
    }

    $headers = @{
        Authorization = "Bearer $mgmt"
        "Content-Type" = "application/json"
    }
    $uri = "https://openrouter.ai/api/v1/keys$Path"
    if ($Body) {
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -Body ($Body | ConvertTo-Json) -TimeoutSec 60
    }
    return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -TimeoutSec 60
}

function Update-EnvFile {
    param([string]$KeyName, [string]$KeyValue)
    $lines = @()
    $found = $false
    if (Test-Path $envFile) {
        foreach ($line in Get-Content $envFile) {
            if ($line -match '^\s*OPENROUTER_API_KEY\s*=') {
                $lines += "$KeyName=$KeyValue"
                $found = $true
            }
            else { $lines += $line }
        }
    }
    if (-not $found) {
        if ($lines.Count -gt 0) { $lines += "" }
        $lines += "# Rotated $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')"
        $lines += "$KeyName=$KeyValue"
    }
    if ($WhatIf) {
        Write-Host "[WhatIf] Would write OPENROUTER_API_KEY to $envFile" -ForegroundColor Yellow
    }
    else {
        Set-Content -Path $envFile -Value $lines -Encoding UTF8
        Write-Host "Updated $envFile" -ForegroundColor Green
    }
}

Write-Host "OpenRouter key rotation" -ForegroundColor Cyan

$list = Invoke-OpenRouterManagement -Method GET -Path ""
$keys = @($list.data)
Write-Host "Found $($keys.Count) existing key(s)" -ForegroundColor Gray

$oldCandidates = $keys | Where-Object { $_.name -like "*libr4*" -or $_.label -like "*libr4*" }
if ($OldKeyPrefix) {
    Write-Host "Looking for keys to replace (prefix match in name/label or manual revoke for: $OldKeyPrefix...)" -ForegroundColor Gray
}

if ($WhatIf) {
    Write-Host "[WhatIf] Would create key: $NewKeyName" -ForegroundColor Yellow
    exit 0
}

$created = Invoke-OpenRouterManagement -Method POST -Path "" -Body @{ name = $NewKeyName }
$newSecret = $created.key
if (-not $newSecret) { throw "Create key response missing .key field" }

Write-Host "Created new key: $NewKeyName (hash=$($created.hash))" -ForegroundColor Green
Update-EnvFile -KeyName "OPENROUTER_API_KEY" -KeyValue $newSecret

# Verify new key works
$pingBody = @{
    model = "deepseek/deepseek-v4-flash"
    messages = @(@{ role = "user"; content = "Reply with exactly: LIBR4_OK" })
    max_tokens = 8
    temperature = 0
} | ConvertTo-Json -Depth 5

$ping = Invoke-RestMethod -Method Post `
    -Uri "https://openrouter.ai/api/v1/chat/completions" `
    -Headers @{
        Authorization = "Bearer $newSecret"
        "HTTP-Referer" = "https://libr4.local"
        "X-Title" = "Libr4 Key Rotation"
    } `
    -ContentType "application/json" `
    -Body $pingBody `
    -TimeoutSec 60

$content = $ping.choices[0].message.content
if ($content -notmatch "LIBR4_OK") { throw "New key ping failed: $content" }
Write-Host "New key verified (OpenRouter ping OK)" -ForegroundColor Green

$env:OPENROUTER_API_KEY = $newSecret

if ($DeleteOld) {
    foreach ($old in $oldCandidates) {
        if ($old.hash -eq $created.hash) { continue }
        Write-Host "Deleting old key: $($old.name) hash=$($old.hash)" -ForegroundColor Yellow
        Invoke-OpenRouterManagement -Method DELETE -Path "/$($old.hash)" | Out-Null
    }
    Write-Host @"

IMPORTANT: Revoke the leaked terminal key manually if it is not in the list above:
  https://openrouter.ai/keys  -> delete key starting with $OldKeyPrefix
"@ -ForegroundColor Yellow
}

Write-Host @"

Rotation complete.
  New key stored in: $envFile
  Export for this session: `$env:OPENROUTER_API_KEY = (Get-Content .env | Where-Object { `$_ -match '^OPENROUTER_API_KEY=' } | ForEach-Object { (`$_ -split '=',2)[1] })
"@ -ForegroundColor Cyan
