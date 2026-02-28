$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'

Write-Host "=== Checking all registry locations ===" -ForegroundColor Cyan

# HKCR (merged view)
$p = "Registry::HKEY_CLASSES_ROOT\CLSID\$guid"
Write-Host "`nHKCR\CLSID\$guid" -ForegroundColor Yellow
if (Test-Path $p) { Write-Host "  EXISTS"; Get-ChildItem $p -Recurse | ForEach-Object { Write-Host "  $($_.Name)"; $_.GetValueNames() | ForEach-Object { Write-Host "    $_ = $((Get-ItemProperty $__.PSPath).$_)" -ErrorAction SilentlyContinue } } } else { Write-Host "  NOT FOUND" -ForegroundColor Red }

# HKLM native
$p = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\$guid"
Write-Host "`nHKLM\SOFTWARE\Classes\CLSID\$guid" -ForegroundColor Yellow
if (Test-Path $p) { Write-Host "  EXISTS" -ForegroundColor Green } else { Write-Host "  NOT FOUND" -ForegroundColor Red }

# HKLM WOW6432Node
$p = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Classes\CLSID\$guid"
Write-Host "`nHKLM\SOFTWARE\WOW6432Node\Classes\CLSID\$guid" -ForegroundColor Yellow
if (Test-Path $p) { Write-Host "  EXISTS" -ForegroundColor Green } else { Write-Host "  NOT FOUND" -ForegroundColor Red }

# HKCU
$p = "Registry::HKEY_CURRENT_USER\SOFTWARE\Classes\CLSID\$guid"
Write-Host "`nHKCU\SOFTWARE\Classes\CLSID\$guid" -ForegroundColor Yellow
if (Test-Path $p) { Write-Host "  EXISTS" -ForegroundColor Green } else { Write-Host "  NOT FOUND" -ForegroundColor Red }

# ProgId locations
Write-Host "`n=== ProgId MDNote.AddIn ===" -ForegroundColor Cyan
foreach ($root in @("HKEY_CLASSES_ROOT", "HKEY_LOCAL_MACHINE\SOFTWARE\Classes", "HKEY_CURRENT_USER\SOFTWARE\Classes")) {
    $p = "Registry::$root\MDNote.AddIn"
    if (Test-Path $p) {
        Write-Host "$root\MDNote.AddIn EXISTS" -ForegroundColor Green
    } else {
        Write-Host "$root\MDNote.AddIn NOT FOUND" -ForegroundColor Red
    }
}

# Check what OneNote version is looking for
Write-Host "`n=== OneNote add-in registry ===" -ForegroundColor Cyan
$addInKey = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'
if (Test-Path $addInKey) {
    Get-ItemProperty $addInKey | Select-Object FriendlyName, LoadBehavior, CommandLineSafe | Format-List
}

# Also check 16.0 path
$addInKey16 = 'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\MDNote.AddIn'
Write-Host "16.0 path:" -ForegroundColor Yellow
if (Test-Path $addInKey16) { Write-Host "  EXISTS" } else { Write-Host "  NOT FOUND (might need this instead)" -ForegroundColor Red }
