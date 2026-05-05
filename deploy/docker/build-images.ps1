#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build Libr4 development environment Docker images
.DESCRIPTION
    Builds all environment images with proper tagging and pushes to registry (optional)
.PARAMETER Registry
    Docker registry URL (e.g., "ghcr.io/libr4" or "docker.io/libr4")
.PARAMETER Push
    Push images to registry after build
.PARAMETER Image
    Specific image to build (dotnet, python, jvm, universal, or "all")
.EXAMPLE
    .\build-images.ps1 -Registry "ghcr.io/libr4" -Push
    .\build-images.ps1 -Image dotnet
#>
param(
    [string]$Registry = "",
    [switch]$Push,
    [ValidateSet("all", "dotnet", "python", "jvm", "universal", "node")]
    [string]$Image = "all"
)

$ErrorActionPreference = "Stop"
$images = @{
    "dotnet" = @{ Tag = "libr4-env:dotnet"; Path = "./environments/dotnet"; Dockerfile = "Dockerfile" }
    "python" = @{ Tag = "libr4-env:python"; Path = "./environments/python"; Dockerfile = "Dockerfile" }
    "jvm" = @{ Tag = "libr4-env:jvm"; Path = "./environments/jvm"; Dockerfile = "Dockerfile" }
    "universal" = @{ Tag = "libr4-env:universal"; Path = "./environments/universal"; Dockerfile = "Dockerfile" }
}

function Build-Image($name, $config) {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Building: $name" -ForegroundColor Cyan
    Write-Host "Tag: $($config.Tag)" -ForegroundColor Cyan
    Write-Host "Path: $($config.Path)" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    $tag = if ($Registry) { "$Registry/$($config.Tag)" } else { $config.Tag }
    $dockerfile = Join-Path $config.Path $config.Dockerfile
    
    try {
        docker build -t $tag -f $dockerfile $config.Path
        
        if ($LASTEXITCODE -ne 0) {
            throw "Docker build failed for $name"
        }

        Write-Host "✅ Successfully built: $tag" -ForegroundColor Green

        if ($Push -and $Registry) {
            Write-Host "Pushing: $tag" -ForegroundColor Yellow
            docker push $tag
            
            if ($LASTEXITCODE -ne 0) {
                throw "Docker push failed for $name"
            }
            
            Write-Host "✅ Successfully pushed: $tag" -ForegroundColor Green
        }

        return $tag
    }
    catch {
        Write-Host "❌ Failed to build $name`: $_" -ForegroundColor Red
        throw
    }
}

# Main execution
$startTime = Get-Date
$builtImages = @()
$imagesToBuild = if ($Image -eq "all") { $images.Keys } else { @($Image) }

Write-Host "`n🚀 Starting Libr4 Docker Image Build" -ForegroundColor Cyan
Write-Host "Registry: $(if ($Registry) { $Registry } else { 'local' })" -ForegroundColor Gray
Write-Host "Push: $Push" -ForegroundColor Gray
Write-Host "Images to build: $($imagesToBuild -join ', ')" -ForegroundColor Gray

foreach ($img in $imagesToBuild) {
    if ($images.ContainsKey($img)) {
        $builtTag = Build-Image $img $images[$img]
        $builtImages += $builtTag
    }
}

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "Duration: $($duration.ToString('hh\:mm\:ss'))" -ForegroundColor Green
Write-Host "Images built:" -ForegroundColor Green
$builtImages | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
Write-Host "========================================" -ForegroundColor Green
