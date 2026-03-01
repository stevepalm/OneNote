# MD Note

A COM add-in for OneNote Desktop (Microsoft 365) that renders Markdown inside OneNote pages. Write or paste Markdown, press F5, and get formatted headings, syntax-highlighted code blocks, tables, task lists, and more.

## Features

- **Full Markdown rendering** — headings, bold/italic, code blocks, tables, lists, blockquotes, links, images, footnotes, definition lists, math expressions
- **Syntax highlighting** — 30+ languages via ColorCode
- **Keyboard shortcuts** — F5 (render), F8 (export), Ctrl+, (toggle source)
- **Auto-detect pasted Markdown** — copies from ChatGPT/Claude render automatically
- **Live mode** — auto-renders as you type
- **Round-trip** — toggle between rendered and source views without losing anything
- **Export/Import** — clipboard, `.md` files, with optional image extraction
- **Table of contents** — auto-generated from headings
- **Settings** — theme, paste behavior, live mode delay, export path, all roaming

## Prerequisites

- Windows 10/11
- .NET Framework 4.8
- OneNote Desktop (Microsoft 365, 64-bit)
- .NET SDK 10+ (for building from source)

## Install (MSI)

Download the MSI from [Releases](../../releases) and run it, or use silent install:

```
msiexec /i MDNote-x64.msi /quiet
```

Then restart OneNote. The **MD Note** tab appears in the ribbon.

See [docs/deploy-guide.md](docs/deploy-guide.md) for Group Policy, Intune, and SCCM deployment.

## Build from Source

```
dotnet build MDNote.sln
```

### Run tests

```
dotnet test MDNote.sln
```

317 tests (242 Core + 75 OneNote) verify the conversion pipeline, XML builder, settings, and edge cases.

### Build the MSI installer

```powershell
.\scripts\build-installer.ps1
```

Builds both x64 and x86 MSIs to `artifacts/`.

## Developer Registration

For development without the MSI, use the PowerShell scripts:

```powershell
# Build, register, and launch OneNote
.\scripts\debug-setup.ps1 -Launch

# Or register manually (run as Admin)
.\scripts\nuclear-register.ps1

# Unregister
.\scripts\unregister-mdnote.ps1
```

## Project Structure

| Project | Target | Purpose |
|---------|--------|---------|
| `MDNote.AddIn` | .NET Fx 4.8 | COM add-in (ribbon, commands, hotkeys, clipboard monitor) |
| `MDNote.Core` | .NET Standard 2.0 | Markdown conversion engine (Markdig, ColorCode, settings) |
| `MDNote.OneNote` | .NET Fx 4.8 | OneNote COM API wrapper (page XML, reader/writer) |
| `MDNote.Setup` | WiX v4 | MSI installer (COM registration, registry keys) |
| `MDNote.Core.Tests` | .NET 10 | 242 xUnit tests for Core |
| `MDNote.OneNote.Tests` | .NET Fx 4.8 | 75 xUnit tests for OneNote XML |

## Documentation

- [Quick Start Guide](docs/quick-start.md) — end-user guide with shortcuts and workflows
- [Deployment Guide](docs/deploy-guide.md) — IT admin guide for enterprise rollout

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| F5 | Render page |
| F8 | Export to clipboard |
| Ctrl+, | Toggle source/rendered view |
