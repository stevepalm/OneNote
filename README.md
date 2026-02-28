# MD Note

A COM add-in for OneNote Desktop (Microsoft 365) that provides Markdown rendering inside OneNote pages.

## Prerequisites

- Windows 10/11
- .NET Framework 4.8
- OneNote Desktop (Microsoft 365, 64-bit)
- .NET SDK 10+ (for building)

## Build

```
dotnet build MDNote.sln
```

## Register

Run PowerShell as **Administrator**:

```powershell
.\scripts\register-mdnote.ps1
```

Then restart OneNote. The **MD Note** tab will appear in the ribbon.

## Unregister

```powershell
.\scripts\unregister-mdnote.ps1
```

## Dev Loop

Build, register, and optionally launch OneNote in one step:

```powershell
.\scripts\debug-setup.ps1 -Launch
```

## Project Structure

| Project | Target | Purpose |
|---------|--------|---------|
| `MDNote.AddIn` | .NET Fx 4.8 | COM add-in (IDTExtensibility2 + IRibbonExtensibility) |
| `MDNote.Core` | .NET Standard 2.0 | Markdown conversion engine |
| `MDNote.OneNote` | .NET Fx 4.8 | OneNote COM API wrapper |
| `MDNote.Core.Tests` | .NET 10 | Unit tests (xUnit) |

## Status

**Session 1** — Solution scaffold and proof-of-life. Click "Render Page" to see the current page title in a MessageBox.
