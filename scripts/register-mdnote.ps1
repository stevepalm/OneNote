<#
.SYNOPSIS
    Registers the MDNote COM add-in for OneNote Desktop.
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

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$dllPath = Join-Path $repoRoot "src\MDNote.AddIn\bin\$Configuration\MDNote.AddIn.dll"

if (-not (Test-Path $dllPath)) {
    Write-Error "DLL not found at: $dllPath`nBuild the solution first."
    exit 1
}

Write-Host "Registering MDNote COM add-in..." -ForegroundColor Cyan
Write-Host "DLL: $dllPath" -ForegroundColor Gray

# 1. COM registration via regasm (64-bit for 64-bit OneNote)
$regasm = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

if (-not (Test-Path $regasm)) {
    Write-Error "RegAsm.exe not found at: $regasm"
    exit 1
}

Write-Host "`nRunning regasm /codebase..." -ForegroundColor Yellow
& $regasm $dllPath /codebase
if ($LASTEXITCODE -ne 0) {
    Write-Error "regasm failed with exit code $LASTEXITCODE"
    exit 1
}
Write-Host "regasm completed successfully." -ForegroundColor Green

# 2. AppID linkage (critical — OneNote loads add-ins via dllhost.exe surrogate)
$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'
Write-Host "`nSetting AppID linkage for DllSurrogate..." -ForegroundColor Yellow

# Link CLSID -> AppID
$hklmClsid = "HKLM:\SOFTWARE\Classes\CLSID\$guid"
if (Test-Path $hklmClsid) {
    Set-ItemProperty -Path $hklmClsid -Name 'AppID' -Value $guid -Type String
    Write-Host "  CLSID -> AppID link set." -ForegroundColor Green
}

# Create AppID with DllSurrogate=""
$appIdPath = "HKLM:\SOFTWARE\Classes\AppID\$guid"
New-Item -Path $appIdPath -Force | Out-Null
Set-ItemProperty -Path $appIdPath -Name 'DllSurrogate' -Value '' -Type String
Write-Host "  AppID DllSurrogate set." -ForegroundColor Green

# 3. OneNote add-in registry key
$addinKey = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'

Write-Host "`nCreating registry key: $addinKey" -ForegroundColor Yellow

if (-not (Test-Path $addinKey)) {
    New-Item -Path $addinKey -Force | Out-Null
}

Set-ItemProperty -Path $addinKey -Name 'FriendlyName' -Value 'MD Note' -Type String
Set-ItemProperty -Path $addinKey -Name 'Description' -Value 'Markdown rendering for OneNote' -Type String
Set-ItemProperty -Path $addinKey -Name 'LoadBehavior' -Value 3 -Type DWord
Set-ItemProperty -Path $addinKey -Name 'CommandLineSafe' -Value 1 -Type DWord

Write-Host "Registry key created." -ForegroundColor Green
Write-Host "`nRegistration complete. Restart OneNote to load the add-in." -ForegroundColor Cyan
