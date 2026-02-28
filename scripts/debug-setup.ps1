<#
.SYNOPSIS
    Build + register for the MDNote dev loop.
    Optionally launches OneNote after registration.
.PARAMETER Launch
    If specified, launches OneNote after registration.
#>

[CmdletBinding()]
param (
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "=== MDNote Debug Setup ===" -ForegroundColor Cyan

# 1. Build
Write-Host "`n[1/3] Building solution..." -ForegroundColor Yellow
Push-Location $repoRoot
dotnet build src/MDNote.AddIn/MDNote.AddIn.csproj -c Debug
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "Build failed."
    exit 1
}
Pop-Location
Write-Host "Build succeeded." -ForegroundColor Green

# 2. Register
Write-Host "`n[2/3] Registering add-in..." -ForegroundColor Yellow
& "$scriptDir\register-mdnote.ps1" -Configuration Debug

# 3. Optionally launch OneNote
if ($Launch) {
    Write-Host "`n[3/3] Launching OneNote..." -ForegroundColor Yellow
    $onenote = "C:\Program Files\Microsoft Office\root\Office16\ONENOTE.EXE"
    if (Test-Path $onenote) {
        Start-Process $onenote
        Write-Host "OneNote launched." -ForegroundColor Green
    } else {
        Write-Warning "OneNote not found at expected path. Launch manually."
    }
} else {
    Write-Host "`n[3/3] Skipping OneNote launch (use -Launch to auto-start)." -ForegroundColor Gray
}

Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
