# Enable Fusion (assembly binding) logging to diagnose load failures
$fusionKey = 'HKLM:\SOFTWARE\Microsoft\Fusion'
if (-not (Test-Path $fusionKey)) {
    New-Item -Path $fusionKey -Force | Out-Null
}

$logDir = "$env:LOCALAPPDATA\MDNote\FusionLog"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

Set-ItemProperty -Path $fusionKey -Name 'EnableLog' -Value 1 -Type DWord
Set-ItemProperty -Path $fusionKey -Name 'ForceLog' -Value 1 -Type DWord
Set-ItemProperty -Path $fusionKey -Name 'LogFailures' -Value 1 -Type DWord
Set-ItemProperty -Path $fusionKey -Name 'LogPath' -Value "$logDir\" -Type String

Write-Host "Fusion logging enabled. Log path: $logDir" -ForegroundColor Green
Write-Host "Now reset LoadBehavior and restart OneNote." -ForegroundColor Yellow

# Also reset LoadBehavior
$key = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'
Set-ItemProperty -Path $key -Name 'LoadBehavior' -Value 3 -Type DWord
Write-Host "LoadBehavior reset to 3" -ForegroundColor Green

# Re-register
$dllPath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "src\MDNote.AddIn\bin\Debug\MDNote.AddIn.dll"
$regasm = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
& $regasm $dllPath /codebase 2>&1 | Out-Null
Write-Host "regasm done" -ForegroundColor Green
