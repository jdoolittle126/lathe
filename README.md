# Lathe

Lathe is a .NET tool for tailing, stitching, and exploring newline-delimited JSON logs from local files, globs, and rolling directories.

## Quick Start

```bash
lathe tail --glob "sample-logs/*.json" -n 20
lathe serve --glob "sample-logs/*.json" -n 100
```

`lathe serve` hosts a localhost-only web workbench with a merged timeline, filters, metrics, and an event inspector.

## Desktop Preview

Lathe also includes an early Windows desktop preview in `src/Desktop`.

The desktop app reuses the existing Blazor workbench by hosting Lathe locally and displaying it inside a WebView2 window. You can drag and drop log files onto the window to add them to the active session.

```bash
dotnet run --project src/Desktop/Desktop.csproj
```

Notes:

- This is intentionally separate from `Lathe.slnx` so the existing CLI tool publish pipeline stays portable.
- Use `Desktop.slnx` when working on the desktop app.
- The current preview accepts dropped files. Glob and rolling-directory picker UX can come later.
