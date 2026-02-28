<#
.SYNOPSIS
    Unregisters the MDNote COM add-in from OneNote Desktop.
    Must be run as Administrator.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug.
#>

[CmdletBinding()]
param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$dllPath = Join-Path $repoRoot "src\MDNote.AddIn\bin\$Configuration\MDNote.AddIn.dll"

Write-Host "Unregistering MDNote COM add-in..." -ForegroundColor Cyan

# 1. Remove COM registration
$regasm = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

if (Test-Path $dllPath) {
    Write-Host "Running regasm /unregister..." -ForegroundColor Yellow
    & $regasm $dllPath /unregister
    Write-Host "regasm unregister completed." -ForegroundColor Green
} else {
    Write-Warning "DLL not found at: $dllPath - skipping regasm."
}

# 2. Remove OneNote add-in registry key
$addinKey = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'

if (Test-Path $addinKey) {
    Remove-Item -Path $addinKey -Recurse -Force
    Write-Host "Registry key removed: $addinKey" -ForegroundColor Green
} else {
    Write-Host "Registry key not found (already removed)." -ForegroundColor Gray
}

Write-Host "`nUnregistration complete. Restart OneNote." -ForegroundColor Cyan
