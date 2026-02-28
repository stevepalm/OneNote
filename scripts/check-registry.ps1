# Check OneNote add-in key
Write-Host "=== HKCU Add-in Key ===" -ForegroundColor Cyan
$key = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'
if (Test-Path $key) {
    Get-ItemProperty $key | Format-List
} else {
    Write-Host "NOT FOUND" -ForegroundColor Red
}

# Check COM registration
Write-Host "=== HKCR ProgId ===" -ForegroundColor Cyan
$progId = 'HKCR:\MDNote.AddIn'
if (Test-Path "Registry::HKEY_CLASSES_ROOT\MDNote.AddIn") {
    Get-ItemProperty "Registry::HKEY_CLASSES_ROOT\MDNote.AddIn" | Format-List
    $clsid = (Get-ItemProperty "Registry::HKEY_CLASSES_ROOT\MDNote.AddIn\CLSID").'(default)'
    Write-Host "CLSID: $clsid"
} else {
    Write-Host "NOT FOUND" -ForegroundColor Red
}

# Check CLSID InprocServer32
Write-Host "`n=== HKCR CLSID ===" -ForegroundColor Cyan
$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'
$clsidPath = "Registry::HKEY_CLASSES_ROOT\CLSID\$guid\InprocServer32"
if (Test-Path $clsidPath) {
    Get-ItemProperty $clsidPath | Format-List
} else {
    Write-Host "NOT FOUND at $clsidPath" -ForegroundColor Red
    # Try WOW64 node
    $wow = "Registry::HKEY_CLASSES_ROOT\WOW6432Node\CLSID\$guid\InprocServer32"
    if (Test-Path $wow) {
        Write-Host "Found in WOW6432Node:" -ForegroundColor Yellow
        Get-ItemProperty $wow | Format-List
    }
}

# Check resiliency (disabled add-ins)
Write-Host "=== Resiliency (disabled add-ins) ===" -ForegroundColor Cyan
$res = 'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\Resiliency'
if (Test-Path $res) {
    Get-ChildItem $res -Recurse | ForEach-Object {
        Write-Host $_.Name
        $_.GetValueNames() | ForEach-Object { Write-Host "  $_" }
    }
} else {
    Write-Host "No resiliency key found" -ForegroundColor Gray
}
