$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'

# Re-add AppID value on CLSID
$clsidPath = "HKCU:\SOFTWARE\Classes\CLSID\$guid"
if (Test-Path $clsidPath) {
    Set-ItemProperty -Path $clsidPath -Name 'AppID' -Value $guid -Type String
    Write-Host "CLSID -> AppID link restored." -ForegroundColor Green
} else {
    Write-Host "CLSID key not found at $clsidPath" -ForegroundColor Red
}

# Re-create AppID key with DllSurrogate
$appIdPath = "HKCU:\SOFTWARE\Classes\AppID\$guid"
New-Item -Path $appIdPath -Force | Out-Null
Set-ItemProperty -Path $appIdPath -Name 'DllSurrogate' -Value '' -Type String
Write-Host "AppID DllSurrogate restored." -ForegroundColor Green

# Clear resiliency
Remove-Item 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\Resiliency' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\Resiliency' -Recurse -Force -ErrorAction SilentlyContinue

# Reset LoadBehavior
foreach ($p in @(
    'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn',
    'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\MDNote.AddIn'
)) {
    if (Test-Path $p) {
        Set-ItemProperty -Path $p -Name 'LoadBehavior' -Value 3 -Type DWord
        Write-Host "LoadBehavior=3 at $p" -ForegroundColor Green
    }
}

Write-Host "`nDone. Start OneNote." -ForegroundColor Cyan
