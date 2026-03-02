<#
.SYNOPSIS
    Fixes the DLL loading issue by uninstalling MSI, cleaning registry, and re-registering.
    MUST be run as Administrator.
.DESCRIPTION
    The MSI installer puts DLLs in Program Files and registers HKLM entries.
    These stale entries can override the dev HKCU registration, causing
    dllhost.exe to load old DLLs from Program Files instead of bin\Debug.
    This script:
      1. Kills OneNote and dllhost
      2. Uninstalls any installed MSI products
      3. Cleans HKLM registry entries
      4. Deletes stale Program Files directory
      5. Rebuilds the solution
      6. Re-registers via nuclear-register.ps1
#>

$ErrorActionPreference = 'Stop'

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell → Run as administrator" -ForegroundColor Yellow
    exit 1
}

Write-Host "=== MDNote DLL Loading Fix ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Kill processes
Write-Host "[1/6] Killing OneNote and dllhost..." -ForegroundColor Yellow
Stop-Process -Name ONENOTE -Force -ErrorAction SilentlyContinue
Stop-Process -Name dllhost -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "  Done." -ForegroundColor Green

# Step 2: Uninstall MSI products
Write-Host "[2/6] Uninstalling MSI products..." -ForegroundColor Yellow
$products = Get-CimInstance Win32_Product | Where-Object { $_.Name -like '*MD*Note*' -or $_.Name -like '*MDNote*' }
if ($products) {
    foreach ($p in $products) {
        Write-Host "  Uninstalling: $($p.Name) $($p.Version) [$($p.IdentifyingNumber)]"
        msiexec /x $p.IdentifyingNumber /qn | Out-Null
        Write-Host "  Uninstalled." -ForegroundColor Green
    }
} else {
    Write-Host "  No MSI products found." -ForegroundColor Gray
}

# Step 3: Clean HKLM registry
Write-Host "[3/6] Cleaning HKLM registry entries..." -ForegroundColor Yellow
$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'
$progId = 'MDNote.AddIn'
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

# Step 4: Delete stale Program Files directory
Write-Host "[4/6] Removing stale Program Files installation..." -ForegroundColor Yellow
$pfDir = "C:\Program Files\MD Note"
if (Test-Path $pfDir) {
    Remove-Item $pfDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Removed: $pfDir" -ForegroundColor Green
} else {
    Write-Host "  Not found (already clean)." -ForegroundColor Gray
}
$pfDir86 = "C:\Program Files (x86)\MD Note"
if (Test-Path $pfDir86) {
    Remove-Item $pfDir86 -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Removed: $pfDir86" -ForegroundColor Green
}

# Step 5: Rebuild
Write-Host "[5/6] Rebuilding solution..." -ForegroundColor Yellow
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Push-Location $repoRoot
try {
    dotnet build --configuration Debug 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Build failed! Run 'dotnet build' manually to see errors." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Build succeeded." -ForegroundColor Green
} finally {
    Pop-Location
}

# Step 6: Re-register
Write-Host "[6/6] Re-registering (HKCU only)..." -ForegroundColor Yellow
$registerScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "nuclear-register.ps1"
& $registerScript -Configuration Debug

Write-Host ""
Write-Host "=== Done! Start OneNote and test Import. ===" -ForegroundColor Cyan
