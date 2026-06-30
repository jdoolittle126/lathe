# Lathe

Lathe is a .NET tool for tailing, stitching, and exploring newline-delimited JSON logs from local files, globs, and rolling directories.

## Quick Start

```bash
lathe tail --glob "sample-logs/*.json" -n 20
lathe serve --glob "sample-logs/*.json" -n 100
```

`lathe serve` hosts a localhost-only web workbench with a merged timeline, filters, metrics, and an event inspector.
