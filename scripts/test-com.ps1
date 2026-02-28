# Test COM activation of MDNote.AddIn
Write-Host "Testing COM activation..." -ForegroundColor Cyan

# Check LoadBehavior
$key = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'
$lb = (Get-ItemProperty $key -ErrorAction SilentlyContinue).LoadBehavior
Write-Host "Current LoadBehavior: $lb"

# Try to create the COM object via ProgId
try {
    $type = [Type]::GetTypeFromProgID("MDNote.AddIn")
    if ($type -eq $null) {
        Write-Host "ERROR: GetTypeFromProgID returned null" -ForegroundColor Red
    } else {
        Write-Host "Type found: $($type.FullName)" -ForegroundColor Green
        $obj = [Activator]::CreateInstance($type)
        Write-Host "COM object created OK: $obj" -ForegroundColor Green
        Write-Host "Type: $($obj.GetType().FullName)"
    }
} catch {
    Write-Host "ERROR creating COM object: $_" -ForegroundColor Red
    Write-Host $_.Exception.ToString()
}
