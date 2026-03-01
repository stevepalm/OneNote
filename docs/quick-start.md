# MD Note — Quick Start Guide

Write Markdown in OneNote and render it beautifully with one keypress.

## Getting Started

After installation, restart OneNote. You'll see a new **MD Note** tab in the ribbon.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **F5** | Render the current page (Markdown to formatted OneNote) |
| **F8** | Export to clipboard (copy the Markdown source) |
| **Ctrl+,** | Toggle between rendered view and Markdown source |

These hotkeys work whenever OneNote is the foreground window.

## Core Workflows

### Write and render

1. Type or paste Markdown into a OneNote page
2. Press **F5** to render
3. Your Markdown is converted to rich formatting: headings, code blocks with syntax highlighting, tables, lists, and more

### Copy from ChatGPT (or any AI)

1. Copy a ChatGPT/Claude/Copilot response (it's already Markdown)
2. Switch to OneNote — if paste detection is on, MD Note auto-renders it
3. Or paste normally and press **F5**

### Toggle source view

Press **Ctrl+,** to flip between:
- **Rendered view** — formatted headings, tables, highlighted code
- **Source view** — raw Markdown in monospace font

This is great for editing and then re-rendering.

### Export Markdown

- **F8** — copies the Markdown source to clipboard (for sharing, pasting into docs, etc.)
- **Ribbon > Export File** — saves as a `.md` file with optional images

### Import a Markdown file

Ribbon > **Import MD** — opens a `.md` file, creates a new OneNote page, and renders it.

### Paste and render

Ribbon > **Paste & Render** — grabs Markdown from clipboard, pastes into the current page, and renders in one step.

## Paste Detection

MD Note can auto-detect when you paste Markdown and offer to render it.

**Settings** (ribbon > Settings > Behavior tab):
- **Prompt** (default) — shows a small popup: Render / Ignore / Always
- **Auto** — renders pasted Markdown automatically
- **Off** — standard paste behavior

## Live Mode

Toggle **Live Mode** in the ribbon to auto-render as you type. Useful for drafting with instant preview. Configurable delay in Settings (default: 1500ms).

## Table of Contents

Ribbon > **Insert TOC** — re-renders the page with an auto-generated table of contents from your headings.

## What Markdown Features Are Supported?

- Headings (H1–H6)
- Bold, italic, strikethrough, inline code
- Fenced code blocks with syntax highlighting (30+ languages)
- Tables with alignment
- Ordered and unordered lists (including nested)
- Task lists (checkboxes)
- Blockquotes (including nested, color-coded)
- Links and images
- Horizontal rules
- Footnotes
- Definition lists
- Math expressions (LaTeX notation)
- Front matter (YAML-style metadata)
- Mermaid diagram blocks (rendered as labeled placeholders)

## Settings

Open via ribbon > **Settings** or the Settings tab:

| Tab | What you can configure |
|-----|----------------------|
| Rendering | Theme (dark/light), syntax highlighting, TOC, font family/size |
| Behavior | Paste mode, live mode toggle, live mode delay |
| Export | Default export path, include images |

## Tips and Tricks

- **Re-render anytime** — press F5 again after editing. MD Note stores the original Markdown and re-renders from it.
- **Works with existing pages** — paste Markdown into any page, even one with existing content. MD Note replaces the content with the rendered version.
- **Settings roam** — your preferences sync across machines via `%APPDATA%`.
- **Non-destructive** — original Markdown source is always stored as page metadata. You never lose the source.
- **Selection rendering** — select specific text and use "Render Selection" from the ribbon to render just that portion.

## Troubleshooting

| Problem | Fix |
|---------|-----|
| MD Note tab not visible | Restart OneNote; check File > Options > Add-ins |
| F5 doesn't work | Make sure OneNote is the foreground window |
| Paste detection not working | Check Settings > Behavior > Paste Mode is not "Off" |
| Code blocks not highlighted | Check Settings > Rendering > "Enable syntax highlighting" is checked |
| Rendering looks wrong | Try Ctrl+, twice (toggle source, then re-render) to force a fresh render |
