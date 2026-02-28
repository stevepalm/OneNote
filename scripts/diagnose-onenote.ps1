<#
.SYNOPSIS
    Comprehensive diagnostic for MDNote COM add-in loading failure.
    Run WITHOUT admin first. Checks all possible causes.
#>

[CmdletBinding()]
param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Continue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$dllPath = Join-Path $repoRoot "src\MDNote.AddIn\bin\$Configuration\MDNote.AddIn.dll"
$guid = '{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}'
$progId = 'MDNote.AddIn'

function Section($title) { Write-Host "`n=== $title ===" -ForegroundColor Cyan }
function OK($msg) { Write-Host "  [OK] $msg" -ForegroundColor Green }
function WARN($msg) { Write-Host "  [WARN] $msg" -ForegroundColor Yellow }
function FAIL($msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function INFO($msg) { Write-Host "  $msg" -ForegroundColor Gray }

# ============================================================
Section "1. OneNote Installation"
# ============================================================

# Find OneNote process or executable
$oneNotePaths = @(
    "${env:ProgramFiles}\Microsoft Office\root\Office16\ONENOTE.EXE",
    "${env:ProgramFiles(x86)}\Microsoft Office\root\Office16\ONENOTE.EXE",
    "${env:ProgramFiles}\Microsoft Office\Office16\ONENOTE.EXE",
    "${env:ProgramFiles(x86)}\Microsoft Office\Office16\ONENOTE.EXE"
)

$oneNoteExe = $null
foreach ($p in $oneNotePaths) {
    if (Test-Path $p) { $oneNoteExe = $p; break }
}

if ($oneNoteExe) {
    OK "Found: $oneNoteExe"

    # Check if C2R (path contains 'root')
    if ($oneNoteExe -match '\\root\\') {
        WARN "Click-to-Run (C2R) installation detected"
    } else {
        OK "MSI installation"
    }

    # Check bitness
    $peHeader = [System.IO.File]::ReadAllBytes($oneNoteExe)
    $peOffset = [BitConverter]::ToInt32($peHeader, 0x3C)
    $machine = [BitConverter]::ToUInt16($peHeader, $peOffset + 4)
    if ($machine -eq 0x8664) {
        OK "OneNote is 64-bit"
    } elseif ($machine -eq 0x14c) {
        WARN "OneNote is 32-bit! Registration may need WOW6432Node"
    } else {
        WARN "Unknown architecture: 0x$($machine.ToString('X4'))"
    }
} else {
    FAIL "ONENOTE.EXE not found in standard locations"
    # Check running process
    $proc = Get-Process ONENOTE -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc) {
        INFO "Running from: $($proc.Path)"
        if ($proc.Path -match '\\root\\') { WARN "C2R installation" }
    }
}

# Check if new OneNote (UWP) vs classic
$uwpOneNote = Get-Process "onenoteim" -ErrorAction SilentlyContinue
if ($uwpOneNote) {
    WARN "UWP OneNote (onenoteim) is running - this does NOT support COM add-ins!"
}

# ============================================================
Section "2. DLL and Assembly"
# ============================================================

if (Test-Path $dllPath) {
    OK "DLL exists: $dllPath"

    # Load and check assembly
    try {
        $asm = [Reflection.Assembly]::LoadFile($dllPath)
        OK "Assembly loads: $($asm.FullName)"

        # Extract public key token
        $actualToken = ($asm.FullName -split 'PublicKeyToken=')[1]
        $expectedToken = '0a081ffbc3ae00e0'
        if ($actualToken -eq $expectedToken) {
            OK "PublicKeyToken matches: $actualToken"
        } else {
            FAIL "PublicKeyToken MISMATCH! DLL=$actualToken, Registry expects=$expectedToken"
        }

        # Check COM-visible class
        $type = $asm.GetType('MDNote.AddIn')
        if ($type) {
            OK "Type 'MDNote.AddIn' found"
            $comVis = $type.GetCustomAttributes([System.Runtime.InteropServices.ComVisibleAttribute], $false)
            if ($comVis.Length -gt 0 -and $comVis[0].Value) {
                OK "Class is [ComVisible(true)]"
            } else {
                FAIL "Class is NOT ComVisible!"
            }
        } else {
            FAIL "Type 'MDNote.AddIn' NOT FOUND in assembly"
        }
    } catch {
        FAIL "Failed to load assembly: $_"
    }

    # Check dependencies in output dir
    $binDir = Split-Path $dllPath
    INFO "Checking dependencies in: $binDir"
    foreach ($dep in @('extensibility.dll', 'office.dll', 'Interop.Microsoft.Office.Interop.OneNote.dll', 'MDNote.OneNote.dll')) {
        if (Test-Path (Join-Path $binDir $dep)) {
            OK "$dep present"
        } else {
            WARN "$dep NOT in output directory"
        }
    }
} else {
    FAIL "DLL not found: $dllPath"
}

# ============================================================
Section "3. COM Activation Test"
# ============================================================

try {
    $type = [Type]::GetTypeFromProgID($progId)
    if ($type) {
        OK "CLSIDFromProgID('$progId') resolved"
        INFO "Type: $($type.GUID)"
    } else {
        FAIL "CLSIDFromProgID('$progId') returned null"
    }
} catch {
    FAIL "CLSIDFromProgID failed: $_"
}

try {
    $obj = [Activator]::CreateInstance($type)
    OK "CreateInstance succeeded: $($obj.GetType().FullName)"
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($obj) | Out-Null
} catch {
    FAIL "CreateInstance FAILED: $_"
}

# ============================================================
Section "4. Registry — HKCU CLSID"
# ============================================================

$hkcuClsid = "HKCU:\SOFTWARE\Classes\CLSID\$guid"
if (Test-Path $hkcuClsid) {
    OK "HKCU CLSID exists"
    $inproc = "$hkcuClsid\InprocServer32"
    if (Test-Path $inproc) {
        $props = Get-ItemProperty $inproc
        INFO "(default)  = $($props.'(default)')"
        INFO "Class      = $($props.Class)"
        INFO "Assembly   = $($props.Assembly)"
        INFO "Runtime    = $($props.RuntimeVersion)"
        INFO "CodeBase   = $($props.CodeBase)"
        INFO "Threading  = $($props.ThreadingModel)"

        if ($props.'(default)' -ne 'mscoree.dll') { FAIL "InprocServer32 default should be 'mscoree.dll'" }
        if ($props.Class -ne 'MDNote.AddIn') { FAIL "Class should be 'MDNote.AddIn'" }
    } else {
        FAIL "InprocServer32 subkey missing"
    }
} else {
    WARN "HKCU CLSID NOT registered"
}

# ============================================================
Section "5. Registry — HKLM CLSID (from regasm)"
# ============================================================

$hklmClsid = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\$guid"
if (Test-Path $hklmClsid) {
    WARN "HKLM CLSID EXISTS — may conflict with HKCU!"
    $inproc = "$hklmClsid\InprocServer32"
    if (Test-Path $inproc) {
        $props = Get-ItemProperty $inproc
        INFO "HKLM CodeBase = $($props.CodeBase)"
    }
} else {
    OK "HKLM CLSID not present (good — no conflict)"
}

# WOW6432Node
$wow = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Classes\CLSID\$guid"
if (Test-Path $wow) {
    WARN "WOW6432Node CLSID EXISTS — may conflict!"
} else {
    OK "WOW6432Node CLSID not present"
}

# ============================================================
Section "6. Registry — HKCR Merged View"
# ============================================================

$hkcrClsid = "Registry::HKEY_CLASSES_ROOT\CLSID\$guid"
if (Test-Path $hkcrClsid) {
    OK "HKCR CLSID resolves"
    $inproc = "$hkcrClsid\InprocServer32"
    if (Test-Path $inproc) {
        $props = Get-ItemProperty $inproc
        INFO "Merged CodeBase = $($props.CodeBase)"
    }
} else {
    FAIL "HKCR CLSID NOT found — COM activation will fail!"
}

# ProgId
$hkcrProgId = "Registry::HKEY_CLASSES_ROOT\$progId"
if (Test-Path $hkcrProgId) {
    $clsidSub = "$hkcrProgId\CLSID"
    if (Test-Path $clsidSub) {
        $clsidVal = (Get-ItemProperty $clsidSub).'(default)'
        if ($clsidVal -eq $guid) {
            OK "ProgId '$progId' -> CLSID $clsidVal (correct)"
        } else {
            FAIL "ProgId maps to wrong CLSID: $clsidVal (expected $guid)"
        }
    }
} else {
    FAIL "ProgId '$progId' NOT found in HKCR"
}

# ============================================================
Section "7. Registry — OneNote Add-in Keys"
# ============================================================

foreach ($path in @(
    'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn',
    'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns\MDNote.AddIn'
)) {
    if (Test-Path $path) {
        $props = Get-ItemProperty $path
        OK "$path"
        INFO "  FriendlyName  = $($props.FriendlyName)"
        INFO "  LoadBehavior  = $($props.LoadBehavior)"
        INFO "  CommandLineSafe = $($props.CommandLineSafe)"
        if ($props.LoadBehavior -ne 3) {
            WARN "  LoadBehavior is $($props.LoadBehavior), should be 3!"
        }
    } else {
        WARN "$path NOT found"
    }
}

# ============================================================
Section "8. Trust Center & Security Settings"
# ============================================================

# Check if add-ins are disabled by policy
foreach ($path in @(
    'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\Security',
    'HKCU:\SOFTWARE\Policies\Microsoft\Office\16.0\OneNote\Security',
    'HKLM:\SOFTWARE\Policies\Microsoft\Office\16.0\OneNote\Security'
)) {
    if (Test-Path $path) {
        $props = Get-ItemProperty $path -ErrorAction SilentlyContinue
        INFO "Found: $path"
        if ($props.DisableAllAddins -eq 1) { FAIL "  DisableAllAddins = 1 — ALL add-ins blocked!" }
        if ($props.RequireAddinSig -eq 1) { WARN "  RequireAddinSig = 1 — only signed add-ins allowed" }
    }
}

# Also check the common Office-wide policy
foreach ($path in @(
    'HKCU:\SOFTWARE\Microsoft\Office\16.0\Common\Security',
    'HKCU:\SOFTWARE\Policies\Microsoft\Office\16.0\Common\Security',
    'HKLM:\SOFTWARE\Policies\Microsoft\Office\16.0\Common\Security'
)) {
    if (Test-Path $path) {
        $props = Get-ItemProperty $path -ErrorAction SilentlyContinue
        if ($props.DisableAllAddins -eq 1) { FAIL "  Office-wide DisableAllAddins = 1!" }
    }
}

# ============================================================
Section "9. OneNote Resiliency"
# ============================================================

foreach ($ver in @('', '16.0\')) {
    foreach ($sub in @('Resiliency', 'Resiliency\CrashingAddinList', 'Resiliency\DisabledItems',
                       'Resiliency\DoNotDisableAddinList', 'Resiliency\AddinList')) {
        $p = "HKCU:\SOFTWARE\Microsoft\Office\${ver}OneNote\$sub"
        if (Test-Path $p) {
            WARN "Resiliency key exists: $p"
            try {
                $items = Get-ItemProperty $p -ErrorAction SilentlyContinue
                $items.PSObject.Properties | Where-Object { $_.Name -notmatch '^PS' } | ForEach-Object {
                    INFO "    $($_.Name) = $($_.Value)"
                }
            } catch {}
        }
    }
}

# ============================================================
Section "10. Click-to-Run Virtual Registry"
# ============================================================

$c2rBase = "HKLM:\SOFTWARE\Microsoft\Office\ClickToRun"
if (Test-Path $c2rBase) {
    OK "C2R base key exists"

    # Check if our CLSID is shadowed in C2R virtual registry
    $c2rClsid = "HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\REGISTRY\MACHINE\Software\Classes\CLSID\$guid"
    if (Test-Path $c2rClsid) {
        FAIL "Our CLSID is in C2R virtual registry — may shadow real registry!"
        try {
            $props = Get-ItemProperty "$c2rClsid\InprocServer32" -ErrorAction SilentlyContinue
            INFO "  C2R CodeBase = $($props.CodeBase)"
        } catch {}
    } else {
        OK "CLSID not in C2R virtual registry (good)"
    }

    # Check if there's an integration key
    $c2rIntegration = "HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\REGISTRY\MACHINE\Software\Classes"
    if (Test-Path $c2rIntegration) {
        INFO "C2R virtual registry classes exist"
    }
} else {
    INFO "No C2R installation found"
}

# ============================================================
Section "11. .NET Framework 4.8"
# ============================================================

$ndpKey = 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full'
if (Test-Path $ndpKey) {
    $release = (Get-ItemProperty $ndpKey).Release
    if ($release -ge 528040) {
        OK ".NET Framework 4.8+ installed (release $release)"
    } else {
        WARN ".NET Framework version release $release (4.8 requires 528040+)"
    }
} else {
    FAIL ".NET Framework 4.x not found!"
}

# Check mscoree.dll
$mscoree = "$env:SystemRoot\System32\mscoree.dll"
if (Test-Path $mscoree) {
    OK "mscoree.dll exists: $mscoree"
} else {
    FAIL "mscoree.dll NOT found!"
}

# ============================================================
Section "12. Other COM Add-ins Registered"
# ============================================================

$otherAddins = @(
    'HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns',
    'HKCU:\SOFTWARE\Microsoft\Office\16.0\OneNote\AddIns'
)
foreach ($base in $otherAddins) {
    if (Test-Path $base) {
        Get-ChildItem $base | ForEach-Object {
            $name = Split-Path $_.Name -Leaf
            $lb = (Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue).LoadBehavior
            if ($name -eq 'MDNote.AddIn') {
                INFO "$name (LoadBehavior=$lb) <-- OUR ADD-IN"
            } else {
                INFO "$name (LoadBehavior=$lb)"
            }
        }
    }
}

# ============================================================
Section "13. Summary & Recommendations"
# ============================================================

Write-Host ""
Write-Host "Key things to check:" -ForegroundColor White
Write-Host "  1. If HKLM CLSID exists alongside HKCU — remove HKLM (regasm /unregister as Admin)" -ForegroundColor White
Write-Host "  2. If LoadBehavior != 3 — reset it" -ForegroundColor White
Write-Host "  3. If Trust Center blocks add-ins — change settings in OneNote" -ForegroundColor White
Write-Host "  4. If OneNote is 32-bit — need 32-bit registration" -ForegroundColor White
Write-Host "  5. If resiliency keys exist — clear them" -ForegroundColor White
Write-Host ""
