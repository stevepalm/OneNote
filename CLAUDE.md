# CLAUDE.md — MD Note Project Instructions

## Project Overview
COM add-in for OneNote Desktop (Microsoft 365) that renders Markdown inside OneNote pages.
.NET Framework 4.8, SDK-style csproj, AnyCPU. Modeled after the OneMore add-in.

## Project Structure
```
src/MDNote.AddIn/           — COM add-in (IDTExtensibility2 + IRibbonExtensibility)
src/MDNote.OneNote/         — OneNote COM interop wrapper + page XML builder/parser
src/MDNote.Core/            — Markdown conversion engine (netstandard2.0)
src/MDNote.Setup/           — WiX v4 MSI installer
tests/MDNote.Core.Tests/    — net10.0 xUnit tests for Core (289 tests)
tests/MDNote.OneNote.Tests/ — net48 xUnit tests for OneNote XML (87 tests)
scripts/                    — Registration, diagnostic, & installer build scripts
docs/                       — deploy-guide.md, quick-start.md
```

## Build & Test Commands
```bash
dotnet clean --configuration Debug
dotnet build --configuration Debug
dotnet test --configuration Debug
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

## Key Build Notes
- **COM Surrogate lock**: `dotnet build` on the full solution will fail file-copy when OneNote is running (dllhost.exe locks AddIn DLLs). Close OneNote first, or build individual projects (`src/MDNote.Core`, `src/MDNote.OneNote`) which always succeed.
- **`dotnet build` caching**: May skip recompilation if it thinks sources are up-to-date. Use `dotnet clean` before `dotnet build` to force a full rebuild.
- **VSInstallDir**: `MDNote.AddIn.csproj` defaults to `C:\Program Files\Microsoft Visual Studio\18\Insiders\`. Change to match your VS edition if build fails with assembly resolution errors.
- **net48 C# constraint**: `MDNote.OneNote.Tests` targets net48 (C# 7.3) — no file-scoped namespaces, no `using` declarations, need explicit `using System;`.

## COM Registration
- **COM GUID**: `{A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D}`
- **ProgId**: `MDNote.AddIn`
- **HKCU-only dev registration** via `scripts/nuclear-register.ps1`
- **HKLM registration** only via MSI installer (enterprise deployment)
- **HKCU always wins over HKLM** — to test MSI version, remove HKCU COM entries first
- **fix-dll-loading.ps1**: Run as Admin to uninstall stale MSIs, clean HKLM, rebuild, and re-register

## OneNote XML Constraints
- **Element order**: Title → Meta* → Outline* (strict, violations cause COM errors)
- **CDATA CSS**: OneNote ignores `margin-top`, `margin-bottom`, and other margin CSS. Use actual OE paragraph elements for spacing.
- **Native lists**: `<one:Number fontSize="9.0" numberFormat="##.">` for numbered lists, `<one:Bullet>` for bullets. List items nested in OEChildren for indentation.
- **Native tables**: HTML `<table>` tags not supported in CDATA — must convert to `<one:Table>` XML with `<one:Row>`/`<one:Cell>`.
- **Rendered outline marker**: `<!-- md-note-rendered -->` comment in CDATA identifies MD Note outlines for selective clearing.

## Architecture Patterns
- **PageXmlBuilder**: Fluent API for building OneNote XML
- **PageXmlParser**: Parses OneNote page XML
- **MarkdownSourceStorage**: Base64 encode/decode, stores source in page meta
- **HtmlToOneNoteConverter**: Converts Markdig HTML output → OneNote-compatible CDATA
- **SyntaxHighlighter**: ColorCode-based, cached HtmlFormatter per instance
- **All render call sites** pass `SettingsManager.Current.ToConversionOptions()`

## Testing
- **376 total tests** (289 Core + 87 OneNote)
- **FluentAssertions 8.x API**: Use `BeGreaterThanOrEqualTo` (not `BeGreaterOrEqualTo`)
- **InternalsVisibleTo**: MDNote.OneNote → MDNote.OneNote.Tests
- **FakeOneNoteInterop**: Manual mock in tests/Helpers, Dictionary-backed page store

## Git & SSH
- **SSH config**: Host `github-personal` using `~/.ssh/id_personal` key
- **Origin**: `git@github-personal:stevepalm/OneNote.git`
- Push workflow includes: build → test → MSI build → commit → push
