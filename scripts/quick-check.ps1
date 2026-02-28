$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'

Write-Host "=== Quick Registry Check ===" -ForegroundColor Cyan

# LoadBehavior
foreach ($p in @(
  'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn',
  'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\MDNote.AddIn'
)) {
  if (Test-Path $p) {
    $lb = (Get-ItemProperty $p).LoadBehavior
    Write-Host "LoadBehavior=$lb at $p" -ForegroundColor $(if ($lb -eq 3) {'Green'} else {'Red'})
  } else { Write-Host "MISSING: $p" -ForegroundColor Red }
}

# HKLM CLSID
if (Test-Path "HKLM:\SOFTWARE\Classes\CLSID\$guid") {
  Write-Host "HKLM CLSID EXISTS - potential conflict!" -ForegroundColor Red
} else { Write-Host "HKLM CLSID: clean" -ForegroundColor Green }

# HKCU CLSID
$hkcu = "HKCU:\SOFTWARE\Classes\CLSID\$guid"
if (Test-Path $hkcu) {
  $p = Get-ItemProperty $hkcu
  Write-Host "HKCU CLSID: exists, AppID=$($p.AppID)" -ForegroundColor Green
  $ip = Get-ItemProperty "$hkcu\InprocServer32"
  Write-Host "  Assembly: $($ip.Assembly)"
  Write-Host "  CodeBase: $($ip.CodeBase)"
} else { Write-Host "HKCU CLSID: MISSING!" -ForegroundColor Red }

# AppID
$appId = "HKCU:\SOFTWARE\Classes\AppID\$guid"
if (Test-Path $appId) {
  $ds = (Get-ItemProperty $appId).DllSurrogate
  Write-Host "AppID DllSurrogate='$ds'" -ForegroundColor Green
} else { Write-Host "AppID: MISSING!" -ForegroundColor Red }

# Resiliency
foreach ($v in @('', '16.0\')) {
  $r = "HKCU:\SOFTWARE\Microsoft\Office\${v}OneNote\Resiliency"
  if (Test-Path $r) {
    Get-ChildItem $r -Recurse | ForEach-Object {
      $name = $_.Name -replace 'HKEY_CURRENT_USER','HKCU:'
      Write-Host "Resiliency: $name" -ForegroundColor Yellow
      $_.GetValueNames() | ForEach-Object {
        Write-Host "  $_"
      }
    }
  }
}

# DLL exists?
$dll = 'D:\GitHub\OneNote\src\MDNote.AddIn\bin\Debug\MDNote.AddIn.dll'
if (Test-Path $dll) { Write-Host "DLL exists: $dll" -ForegroundColor Green }
else { Write-Host "DLL MISSING: $dll" -ForegroundColor Red }
