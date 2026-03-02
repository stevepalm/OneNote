<#
.SYNOPSIS
    Nuclear clean + fresh HKCU-only registration.
    Run as ADMIN to clean HKLM entries from previous regasm runs.
    If not admin, HKLM cleanup is skipped (warned).
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
    Write-Error "DLL not found: $dllPath`nBuild first: dotnet build"
    exit 1
}

$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'
$progId = 'MDNote.AddIn'
$className = 'MDNote.AddIn'

# Load the DLL to get the REAL assembly full name and public key token
$asm = [Reflection.Assembly]::LoadFile($dllPath)
$asmFullName = $asm.FullName
$codeBase = "file:///$($dllPath -replace '\\','/')"

Write-Host "=== MDNote Nuclear Clean + Register ===" -ForegroundColor Cyan
Write-Host "DLL:      $dllPath" -ForegroundColor Gray
Write-Host "Assembly: $asmFullName" -ForegroundColor Gray
Write-Host "CodeBase: $codeBase" -ForegroundColor Gray

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    Write-Host "Running as ADMIN — will clean HKLM too" -ForegroundColor Green
} else {
    Write-Host "NOT admin — will skip HKLM cleanup (run as Admin if regasm was used before)" -ForegroundColor Yellow
}

# ============================================================
Write-Host "`n[1/7] Killing OneNote..." -ForegroundColor Yellow
# ============================================================
$proc = Get-Process ONENOTE -ErrorAction SilentlyContinue
if ($proc) {
    Stop-Process -Name ONENOTE -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "  OneNote terminated." -ForegroundColor Green
} else {
    Write-Host "  OneNote not running." -ForegroundColor Gray
}

# ============================================================
Write-Host "`n[2/7] Cleaning HKLM (regasm entries)..." -ForegroundColor Yellow
# ============================================================
if ($isAdmin) {
    # regasm /unregister
    $regasm = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
    if (Test-Path $regasm) {
        Write-Host "  Running regasm /unregister..."
        & $regasm $dllPath /unregister 2>&1 | Out-Null
        Write-Host "  regasm /unregister done."
    }

    # Manual HKLM cleanup
    foreach ($path in @(
        "HKLM:\SOFTWARE\Classes\CLSID\$guid",
        "HKLM:\SOFTWARE\Classes\$progId",
        "HKLM:\SOFTWARE\WOW6432Node\Classes\CLSID\$guid",
        "HKLM:\SOFTWARE\WOW6432Node\Classes\$progId",
        "HKLM:\SOFTWARE\Classes\AppID\$guid"
    )) {
        if (Test-Path $path) {
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  Removed: $path"
        }
    }
    Write-Host "  HKLM clean." -ForegroundColor Green
} else {
    Write-Host "  SKIPPED (not admin)" -ForegroundColor Yellow
}

# ============================================================
Write-Host "`n[3/7] Cleaning HKCU (manual entries)..." -ForegroundColor Yellow
# ============================================================
foreach ($path in @(
    "HKCU:\SOFTWARE\Classes\CLSID\$guid",
    "HKCU:\SOFTWARE\Classes\$progId",
    "HKCU:\SOFTWARE\Classes\AppID\$guid",
    "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\MDNote.AddIn.dll",
    "HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\$progId",
    "HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\$progId"
)) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $path"
    }
}
Write-Host "  HKCU clean." -ForegroundColor Green

# ============================================================
Write-Host "`n[4/7] Clearing ALL resiliency data..." -ForegroundColor Yellow
# ============================================================
foreach ($ver in @('', '16.0\')) {
    $resBase = "HKCU:\SOFTWARE\Microsoft\Office\${ver}OneNote\Resiliency"
    if (Test-Path $resBase) {
        Remove-Item $resBase -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleared: $resBase"
    }
}
Write-Host "  Resiliency cleared." -ForegroundColor Green

# ============================================================
Write-Host "`n[5/7] Registering CLSID + ProgId (HKCU only)..." -ForegroundColor Yellow
# ============================================================

# ProgId -> CLSID
$hkcuProgId = "HKCU:\SOFTWARE\Classes\$progId"
New-Item -Path "$hkcuProgId\CLSID" -Force | Out-Null
Set-ItemProperty -Path $hkcuProgId -Name '(default)' -Value $progId
Set-ItemProperty -Path "$hkcuProgId\CLSID" -Name '(default)' -Value $guid
Write-Host "  ProgId created."

# CLSID with full InprocServer32
$hkcuClsid = "HKCU:\SOFTWARE\Classes\CLSID\$guid"
New-Item -Path "$hkcuClsid\InprocServer32" -Force | Out-Null
$asmVersion = $asm.GetName().Version.ToString()
New-Item -Path "$hkcuClsid\InprocServer32\$asmVersion" -Force | Out-Null
New-Item -Path "$hkcuClsid\ProgId" -Force | Out-Null
New-Item -Path "$hkcuClsid\Implemented Categories\{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}" -Force | Out-Null
New-Item -Path "$hkcuClsid\Programmable" -Force | Out-Null

Set-ItemProperty -Path $hkcuClsid -Name '(default)' -Value $className
Set-ItemProperty -Path $hkcuClsid -Name 'AppID' -Value $guid -Type String
Write-Host "  AppID linkage set on CLSID (critical for dllhost.exe surrogate)."

# AppID key with DllSurrogate (tells COM to host via dllhost.exe)
$appIdPath = "HKCU:\SOFTWARE\Classes\AppID\$guid"
New-Item -Path $appIdPath -Force | Out-Null
Set-ItemProperty -Path $appIdPath -Name 'DllSurrogate' -Value '' -Type String
Write-Host "  AppID with DllSurrogate created."

Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name '(default)' -Value 'mscoree.dll'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'ThreadingModel' -Value 'Both'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'Class' -Value $className
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'Assembly' -Value $asmFullName
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32" -Name 'CodeBase' -Value $codeBase

# Version-specific subkey
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\$asmVersion" -Name 'Class' -Value $className
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\$asmVersion" -Name 'Assembly' -Value $asmFullName
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\$asmVersion" -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path "$hkcuClsid\InprocServer32\$asmVersion" -Name 'CodeBase' -Value $codeBase

Set-ItemProperty -Path "$hkcuClsid\ProgId" -Name '(default)' -Value $progId
Write-Host "  CLSID registered." -ForegroundColor Green

# ============================================================
Write-Host "`n[6/7] Registering add-in keys..." -ForegroundColor Yellow
# ============================================================
foreach ($path in @(
    "HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\$progId",
    "HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\$progId"
)) {
    New-Item -Path $path -Force | Out-Null
    Set-ItemProperty -Path $path -Name 'FriendlyName' -Value 'MD Note' -Type String
    Set-ItemProperty -Path $path -Name 'Description' -Value 'Markdown rendering for OneNote' -Type String
    Set-ItemProperty -Path $path -Name 'LoadBehavior' -Value 3 -Type DWord
    Set-ItemProperty -Path $path -Name 'CommandLineSafe' -Value 1 -Type DWord
    Write-Host "  Created: $path"
}

# ============================================================
Write-Host "`n[7/7] Setting DoNotDisableAddinList..." -ForegroundColor Yellow
# ============================================================
foreach ($ver in @('', '16.0\')) {
    $dndPath = "HKCU:\SOFTWARE\Microsoft\Office\${ver}OneNote\Resiliency\DoNotDisableAddinList"
    if (-not (Test-Path $dndPath)) {
        New-Item -Path $dndPath -Force | Out-Null
    }
    # Value name is the ProgId, value is DWORD 1
    Set-ItemProperty -Path $dndPath -Name $progId -Value 1 -Type DWord
    Write-Host "  Set: $dndPath\$progId = 1"
}
Write-Host "  Done." -ForegroundColor Green

# ============================================================
Write-Host "`n=== Verification ===" -ForegroundColor Cyan
# ============================================================

# Quick verification
$verifyClsid = "HKCU:\SOFTWARE\Classes\CLSID\$guid"
if (Test-Path $verifyClsid) {
    $clsidProps = Get-ItemProperty $verifyClsid
    if ($clsidProps.AppID -eq $guid) {
        Write-Host "  CLSID -> AppID linkage: OK" -ForegroundColor Green
    } else {
        Write-Host "  CLSID -> AppID linkage: MISSING!" -ForegroundColor Red
    }
}

$verifyAppId = "HKCU:\SOFTWARE\Classes\AppID\$guid"
if (Test-Path $verifyAppId) {
    $appIdProps = Get-ItemProperty $verifyAppId
    Write-Host "  AppID DllSurrogate: '$($appIdProps.DllSurrogate)' (empty=default surrogate)" -ForegroundColor Green
} else {
    Write-Host "  AppID key: MISSING!" -ForegroundColor Red
}

$verify = "HKCU:\SOFTWARE\Classes\CLSID\$guid\InprocServer32"
if (Test-Path $verify) {
    $p = Get-ItemProperty $verify
    Write-Host "  CLSID InprocServer32 OK" -ForegroundColor Green
    Write-Host "    Assembly: $($p.Assembly)" -ForegroundColor Gray
    Write-Host "    CodeBase: $($p.CodeBase)" -ForegroundColor Gray
}

# Test COM activation
try {
    $type = [Type]::GetTypeFromProgID($progId)
    $obj = [Activator]::CreateInstance($type)
    Write-Host "  COM activation test: PASSED" -ForegroundColor Green
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($obj) | Out-Null
} catch {
    Write-Host "  COM activation test: FAILED - $_" -ForegroundColor Red
}

Write-Host "`n=== Done. Start OneNote and check. ===" -ForegroundColor Cyan
Write-Host "If it still doesn't work, run diagnose-onenote.ps1 and share the output." -ForegroundColor Yellow
