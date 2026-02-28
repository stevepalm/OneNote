$cutoff = (Get-Date).AddMinutes(-30)
Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=$cutoff} -MaxEvents 50 -ErrorAction SilentlyContinue |
    Where-Object { $_.Message -match 'MDNote|OneNote|mscoree|\.NET Runtime|CLR' } |
    Format-List TimeCreated, ProviderName, Id, Message
