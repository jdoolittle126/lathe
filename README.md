# Lathe

Lathe is a .NET tool for tailing, stitching, and exploring newline-delimited JSON logs from local files, globs, and rolling directories.

## Quick Start

```bash
lathe tail --glob "sample-logs/*.json" -n 20
lathe serve --glob "sample-logs/*.json" -n 100
lathe ui --glob "sample-logs/*.json" -n 100
```

`lathe serve` hosts a localhost-only web workbench with a merged timeline, filters, metrics, and an event inspector.
`lathe ui` opens the native desktop workbench on Windows and falls back to the browser-hosted workbench when the desktop shell is not available.
