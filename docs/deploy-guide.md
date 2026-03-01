# MD Note — Deployment Guide

IT admin guide for deploying the MD Note COM add-in across your organization.

## Prerequisites

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 1903+ / Windows 11 |
| .NET Framework | 4.8 (pre-installed on Win 10 1903+) |
| OneNote | OneNote Desktop (Microsoft 365, Click-to-Run) |
| Permissions | Local admin (for HKLM install) or HKCU-only for per-user |

## Installation

### Interactive install

```
MDNote-x64.msi
```

Double-click the MSI. Installs to `C:\Program Files\MD Note\` and registers the COM add-in for all users.

### Silent install

```
msiexec /i MDNote-x64.msi /quiet /norestart /log install.log
```

For 32-bit OneNote (rare on Microsoft 365):

```
msiexec /i MDNote-x86.msi /quiet /norestart /log install.log
```

### Per-machine vs per-user

The MSI installs per-machine (HKLM) which is standard for enterprise deployment. For per-user dev installs, use the PowerShell registration scripts in `scripts/` instead.

## Group Policy Deployment

### Via Software Installation (GPO)

1. Copy the MSI to a network share accessible by target machines (e.g., `\\server\share\MDNote\MDNote-x64.msi`)
2. Open **Group Policy Management Console**
3. Create or edit a GPO linked to the target OU
4. Navigate to **Computer Configuration > Policies > Software Settings > Software Installation**
5. Right-click > **New > Package**
6. Browse to the UNC path of the MSI
7. Select **Assigned** deployment method
8. The add-in installs at next Group Policy refresh or reboot

### Via Intune / Endpoint Manager

1. Package the MSI as a Win32 app (`.intunewin`) using the Content Prep Tool
2. Upload to **Intune > Apps > Windows > Add**
3. Install command: `msiexec /i MDNote-x64.msi /quiet /norestart`
4. Uninstall command: `msiexec /x {B7E3F1A2-9C4D-4E5F-8A6B-1D2E3F4A5B6C} /quiet`
5. Detection rule: File exists `C:\Program Files\MD Note\MDNote.AddIn.dll`
6. Requirement: Windows 10 1903+ / 64-bit

### Via SCCM / ConfigMgr

1. Create a new Application in the Software Library
2. Deployment type: Windows Installer (MSI)
3. Content location: network share with the MSI
4. Install program: `msiexec /i MDNote-x64.msi /quiet /norestart`
5. Detection method: Registry key `HKLM\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn` exists

## Upgrade

The MSI uses a major upgrade strategy. Installing a newer version automatically removes the prior version. User settings are preserved (stored in `%APPDATA%\MDNote\settings.json`).

```
msiexec /i MDNote-x64-v1.1.msi /quiet /norestart
```

## Uninstall

### Interactive

Control Panel > Programs and Features > MD Note > Uninstall

### Silent

```
msiexec /x {B7E3F1A2-9C4D-4E5F-8A6B-1D2E3F4A5B6C} /quiet /norestart
```

### What gets removed

- Files in `C:\Program Files\MD Note\`
- COM registration (CLSID, ProgId, AppID)
- OneNote add-in registry keys
- Resiliency/DoNotDisable entries

### What is preserved

- User settings: `%APPDATA%\MDNote\settings.json`
- Log files: `%LOCALAPPDATA%\MDNote\logs\`

To remove settings too, delete those folders after uninstall.

## Troubleshooting

### Add-in not visible in OneNote ribbon

1. **Restart OneNote** — the add-in loads on startup, not mid-session
2. **Check load state** — in OneNote, go to File > Options > Add-ins. Look for "MD Note":
   - **Active**: loaded, ribbon should be visible
   - **Inactive**: loaded but not connected — click "Manage: COM Add-ins > Go" and check the box
   - **Disabled**: OneNote disabled it — see "Add-in disabled by OneNote" below
   - **Not listed**: COM registration failed — see "COM registration errors" below
3. **Check architecture** — if OneNote is 32-bit, install the x86 MSI instead

### Add-in disabled by OneNote

OneNote disables add-ins it considers slow or crashing.

1. Open OneNote > File > Options > Add-ins
2. At the bottom, change dropdown to **Disabled Items** > Go
3. Select "MD Note" and click **Enable**
4. Restart OneNote

The installer sets `DoNotDisableAddinList` to prevent this, but OneNote can still disable in extreme cases.

### COM registration errors

Symptoms: "Class not registered" (0x80040154) or the add-in doesn't appear.

1. Verify the DLL exists: `dir "C:\Program Files\MD Note\MDNote.AddIn.dll"`
2. Check CLSID registration:
   ```powershell
   Get-ItemProperty "HKLM:\SOFTWARE\Classes\CLSID\{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}"
   ```
3. Check add-in key:
   ```powershell
   Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn"
   ```
4. If keys are missing, repair the install:
   ```
   msiexec /fa MDNote-x64.msi /quiet
   ```

### Permission errors (0x80070005)

The MSI writes to HKLM, which requires admin. If deploying via GPO (computer policy), this runs as SYSTEM which has the necessary access. If a user tries to install manually without admin, the MSI will prompt for elevation.

### Conflicting HKCU registrations

If developers previously used the PowerShell registration scripts (HKCU), those entries can conflict with the MSI's HKLM entries. Clean them up:

```powershell
# Run the nuclear-register script to clean up, then uninstall
.\scripts\nuclear-register.ps1

# Then uninstall and reinstall via MSI
```

Or manually remove:
```powershell
Remove-Item "HKCU:\SOFTWARE\Classes\CLSID\{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}" -Recurse -Force
Remove-Item "HKCU:\SOFTWARE\Classes\MDNote.AddIn" -Recurse -Force
Remove-Item "HKCU:\SOFTWARE\Classes\AppID\{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}" -Recurse -Force
Remove-Item "HKCU:\SOFTWARE\Microsoft\Office\OneNote\AddIns\MDNote.AddIn" -Recurse -Force
```

### Logs

- **Installer log**: `msiexec /i MDNote.msi /quiet /log C:\temp\mdnote-install.log`
- **Add-in log**: `%LOCALAPPDATA%\MDNote\logs\` (dated files)
- **Windows Event Log**: Application log, source "ONENOTE" or ".NET Runtime"

## Architecture Notes

- The add-in DLL is AnyCPU and runs in the OneNote process via COM surrogate (`dllhost.exe`)
- All files install to a single `C:\Program Files\MD Note\` directory
- No GAC installation required — the MSI uses `CodeBase` registry entries for assembly resolution
- User settings roam via `%APPDATA%` for multi-machine users
