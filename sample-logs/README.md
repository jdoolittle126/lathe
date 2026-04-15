# Sample Logs

These are newline-delimited JSON log files you can use with `lathe tail` and `lathe serve`.

Examples:

```powershell
lathe tail --file sample-logs/web-01.json -n 20
lathe tail --glob "sample-logs/*.json" -n 50
lathe serve --glob "sample-logs/*.json" -n 100
lathe tail --rolling sample-logs/rolling --follow -n 20
```
