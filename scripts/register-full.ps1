<#
.SYNOPSIS
    Full registration matching OneMore add-in pattern.
    Includes AppID, App Paths, WOW6432Node, and resiliency cleanup.
    Run without Admin for HKCU, or with Admin for HKLM.
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
$progId = 'MDNote.AddIn'
$className = 'MDNote.AddIn'
$asmFullName = 'MDNote.AddIn, Version=0.1.0.0, Culture=neutral, PublicKeyToken=0a081ffbc3ae00e0'
$codeBase = "file:///$($dllPath -replace '\\','/')"

Write-Host "=== MDNote Full Registration ===" -ForegroundColor Cyan
Write-Host "DLL: $dllPath" -ForegroundColor Gray

# --- 1. Clear any resiliency data ---
Write-Host "`n[1] Clearing resiliency data..." -ForegroundColor Yellow
foreach ($ver in @('', '16.0\', '15.0\')) {
    $resPath = "HKCU:\SOFTWARE\Microsoft\Office\${ver}OneNote\Resiliency"
    if (Test-Path $resPath) {
        Remove-Item $resPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleared: $resPath"
    }
}
# Also clear CrashingAddinList and DisabledItems
foreach ($ver in @('', '16.0\', '15.0\')) {
    foreach ($sub in @('Resiliency\CrashingAddinList', 'Resiliency\DisabledItems', 'Resiliency\DoNotDisableAddinList',
                       'Resiliency\NotificationReminderAddinData', 'Resiliency\AddinList')) {
        $p = "HKCU:\SOFTWARE\Microsoft\Office\${ver}OneNote\$sub"
        if (Test-Path $p) {
            Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  Cleared: $p"
        }
    }
}
Write-Host "  Done." -ForegroundColor Green

# --- 2. HKCU ProgId ---
Write-Host "`n[2] Registering ProgId (HKCU)..." -ForegroundColor Yellow
$hkcuProgId = "HKCU:\SOFTWARE\Classes\$progId"
New-Item -Path "$hkcuProgId\CLSID" -Force | Out-Null
Set-ItemProperty -Path $hkcuProgId -Name '(default)' -Value $progId
Set-ItemProperty -Path "$hkcuProgId\CLSID" -Name '(default)' -Value $guid
Write-Host "  Done." -ForegroundColor Green

# --- 3. HKCU CLSID ---
Write-Host "`n[3] Registering CLSID (HKCU)..." -ForegroundColor Yellow
$hkcuClsid = "HKCU:\SOFTWARE\Classes\CLSID\$guid"

# InprocServer32
New-Item -Path "$hkcuClsid\InprocServer32" -Force | Out-Null
New-Item -Path "$hkcuClsid\InprocServer32\0.1.0.0" -Force | Out-Null
New-Item -Path "$hkcuClsid\ProgId" -Force | Out-Null
New-Item -Path "$hkcuClsid\Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}" -Force | Out-Null
New-Item -Path "$hkcuClsid\Programmable" -Force | Out-Null

Set-ItemProperty -Path $hkcuClsid -Name '(default)' -Value $className
Set-ItemProperty -Path $hkcuClsid -Name 'AppID' -Value $guid -Type String
Write-Host "  AppID linkage set on CLSID."
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name '(default)' -Value 'mscoree.dll'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'ThreadingModel' -Value 'Both'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'Class' -Value $className
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'Assembly' -Value $asmFullName
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'CodeBase' -Value $codeBase
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\0.1.0.0" -Name 'Class' -Value $className
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\0.1.0.0" -Name 'Assembly' -Value $asmFullName
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\0.1.0.0" -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\0.1.0.0" -Name 'CodeBase' -Value $codeBase
Set-ItemProperty -Path "$hkcuClsid\ProgId" -Name '(default)' -Value $progId
Write-Host "  Done." -ForegroundColor Green

# --- 4. AppID (DllSurrogate) ---
Write-Host "`n[4] Registering AppID..." -ForegroundColor Yellow
$appIdPath = "HKCU:\SOFTWARE\Classes\AppID\$guid"
New-Item -Path $appIdPath -Force | Out-Null
Set-ItemProperty -Path $appIdPath -Name 'DllSurrogate' -Value '' -Type String
Write-Host "  Done." -ForegroundColor Green

# --- 5. App Paths ---
Write-Host "`n[5] Registering App Paths..." -ForegroundColor Yellow
$appPaths = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\MDNote.AddIn.dll"
New-Item -Path $appPaths -Force | Out-Null
Set-ItemProperty -Path $appPaths -Name '(default)' -Value $dllPath
Write-Host "  Done." -ForegroundColor Green

# --- 6. OneNote add-in key (both versioned and unversioned) ---
Write-Host "`n[6] Registering add-in keys..." -ForegroundColor Yellow
foreach ($path in @(
    "HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\$progId",
    "HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\$progId"
)) {
    if (-not (Test-Path $path)) {
        New-Item -Path $path -Force | Out-Null
    }
    Set-ItemProperty -Path $path -Name 'FriendlyName' -Value 'MD Note' -Type String
    Set-ItemProperty -Path $path -Name 'Description' -Value 'Markdown rendering for OneNote' -Type String
    Set-ItemProperty -Path $path -Name 'LoadBehavior' -Value 3 -Type DWord
    Set-ItemProperty -Path $path -Name 'CommandLineSafe' -Value 1 -Type DWord
    Write-Host "  Created: $path"
}
Write-Host "  Done." -ForegroundColor Green

Write-Host "`n=== Registration complete. Restart OneNote. ===" -ForegroundColor Cyan
