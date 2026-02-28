<#
.SYNOPSIS
    Registers MDNote COM add-in using HKCU registry (for Click-to-Run Office).
    Does NOT require Administrator.
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

if (-not (Test-Path $dllPath)) {
    Write-Error "DLL not found at: $dllPath`nBuild the solution first."
    exit 1
}

$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'
$className = 'MDNote.AddIn'
$asmFullName = 'MDNote.AddIn, Version=0.1.0.0, Culture=neutral, PublicKeyToken=0a081ffbc3ae00e0'

Write-Host "Registering MDNote COM add-in (HKCU)..." -ForegroundColor Cyan
Write-Host "DLL: $dllPath" -ForegroundColor Gray

# 1. ProgId -> CLSID mapping
$progIdPath = "HKCU:\SOFTWARE\Classes\$className"
New-Item -Path "$progIdPath\CLSID" -Force | Out-Null
Set-ItemProperty -Path $progIdPath -Name '(default)' -Value $className
Set-ItemProperty -Path "$progIdPath\CLSID" -Name '(default)' -Value $guid

# 2. CLSID -> InprocServer32 (mscoree.dll for .NET COM hosting)
$clsidPath = "HKCU:\SOFTWARE\Classes\CLSID\$guid"
New-Item -Path "$clsidPath\InprocServer32" -Force | Out-Null
New-Item -Path "$clsidPath\InprocServer32\0.1.0.0" -Force | Out-Null
New-Item -Path "$clsidPath\ProgId" -Force | Out-Null
New-Item -Path "$clsidPath\Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}" -Force | Out-Null

Set-ItemProperty -Path $clsidPath -Name '(default)' -Value $className
Set-ItemProperty -Path "$clsidPath\InprocServer32" -Name '(default)' -Value 'mscoree.dll'
Set-ItemProperty -Path "$clsidPath\InprocServer32" -Name 'ThreadingModel' -Value 'Both'
Set-ItemProperty -Path "$clsidPath\InprocServer32" -Name 'Class' -Value $className
Set-ItemProperty -Path "$clsidPath\InprocServer32" -Name 'Assembly' -Value $asmFullName
Set-ItemProperty -Path "$clsidPath\InprocServer32" -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path "$clsidPath\InprocServer32" -Name 'CodeBase' -Value "file:///$($dllPath -replace '\\','/')"

# Version-specific key (same values)
Set-ItemProperty -Path "$clsidPath\InprocServer32\0.1.0.0" -Name 'Class' -Value $className
Set-ItemProperty -Path "$clsidPath\InprocServer32\0.1.0.0" -Name 'Assembly' -Value $asmFullName
Set-ItemProperty -Path "$clsidPath\InprocServer32\0.1.0.0" -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path "$clsidPath\InprocServer32\0.1.0.0" -Name 'CodeBase' -Value "file:///$($dllPath -replace '\\','/')"

Set-ItemProperty -Path "$clsidPath\ProgId" -Name '(default)' -Value $className

Write-Host "HKCU COM registration done." -ForegroundColor Green

# 3. OneNote add-in key
$addinKey = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'
if (-not (Test-Path $addinKey)) {
    New-Item -Path $addinKey -Force | Out-Null
}
Set-ItemProperty -Path $addinKey -Name 'FriendlyName' -Value 'MD Note' -Type String
Set-ItemProperty -Path $addinKey -Name 'Description' -Value 'Markdown rendering for OneNote' -Type String
Set-ItemProperty -Path $addinKey -Name 'LoadBehavior' -Value 3 -Type DWord
Set-ItemProperty -Path $addinKey -Name 'CommandLineSafe' -Value 1 -Type DWord

Write-Host "Add-in key set (LoadBehavior=3)." -ForegroundColor Green
Write-Host "`nRestart OneNote to load the add-in." -ForegroundColor Cyan
