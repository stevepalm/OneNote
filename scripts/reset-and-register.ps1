$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$dllPath = Join-Path $repoRoot "src\MDNote.AddIn\bin\Debug\MDNote.AddIn.dll"
$regasm = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

# Re-register COM
& $regasm $dllPath /codebase
Write-Host "regasm OK" -ForegroundColor Green

# Reset LoadBehavior to 3
$key = 'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn'
Set-ItemProperty -Path $key -Name 'LoadBehavior' -Value 3 -Type DWord
Write-Host "LoadBehavior reset to 3" -ForegroundColor Green

# Clear the log
$logPath = Join-Path $env:LOCALAPPDATA "MDNote\addin.log"
if (Test-Path $logPath) { Remove-Item $logPath }
Write-Host "Log cleared. Now restart OneNote and check:" -ForegroundColor Cyan
Write-Host "  $logPath" -ForegroundColor Gray
