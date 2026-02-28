try {
    $asm = [System.Reflection.Assembly]::LoadFrom('D:\GitHub\OneNote\src\MDNote.AddIn\bin\Debug\MDNote.AddIn.dll')
    Write-Host "Assembly loaded OK"
    $types = $asm.GetExportedTypes()
    foreach ($t in $types) {
        Write-Host "  Type: $($t.FullName)"
        $interfaces = $t.GetInterfaces()
        foreach ($i in $interfaces) {
            Write-Host "    Implements: $($i.FullName)"
        }
    }
    # Try to instantiate the AddIn class
    $addin = [System.Activator]::CreateInstance($t)
    Write-Host "Instantiation OK: $addin"
} catch {
    Write-Host "ERROR: $_"
    Write-Host $_.Exception.ToString()
}
