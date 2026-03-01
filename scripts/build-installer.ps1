<#
.SYNOPSIS
    Builds the MD Note MSI installer for x64 and/or x86.

.PARAMETER Platform
    Target platform: x64, x86, or Both (default: Both).

.PARAMETER Configuration
    Build configuration (default: Release).

.EXAMPLE
    .\scripts\build-installer.ps1
    .\scripts\build-installer.ps1 -Platform x64
    .\scripts\build-installer.ps1 -Platform x86 -Configuration Debug
#>

[CmdletBinding()]
param (
    [ValidateSet('x64', 'x86', 'Both')]
    [string]$Platform = 'Both',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$setupProj = Join-Path $repoRoot 'src\MDNote.Setup\MDNote.Setup.wixproj'
$outDir = Join-Path $repoRoot 'artifacts'

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

Write-Host "=== MD Note Installer Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Platform:      $Platform" -ForegroundColor Gray

# Ensure WiX toolset is available
$wixCheck = dotnet tool list -g 2>&1
if ($wixCheck -notmatch 'wix') {
    Write-Host "Installing WiX Toolset CLI..." -ForegroundColor Yellow
    dotnet tool install -g wix
}

$platforms = if ($Platform -eq 'Both') { @('x64', 'x86') } else { @($Platform) }

foreach ($plat in $platforms) {
    Write-Host "`nBuilding $plat MSI..." -ForegroundColor Yellow

    dotnet build $setupProj `
        -c $Configuration `
        -p:InstallerPlatform=$plat `
        -o "$outDir\$plat"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $plat"
        exit 1
    }

    $msi = Get-ChildItem "$outDir\$plat\*.msi" | Select-Object -First 1
    if ($msi) {
        Write-Host "  Output: $($msi.FullName)" -ForegroundColor Green
        Write-Host "  Size:   $([math]::Round($msi.Length / 1MB, 2)) MB" -ForegroundColor Gray
    }
}

Write-Host "`n=== Build complete ===" -ForegroundColor Cyan
Write-Host "Artifacts in: $outDir" -ForegroundColor Gray
