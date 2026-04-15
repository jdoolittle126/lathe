namespace Lathe.Core.Domain;

public sealed record LogEventRecord(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? Exception,
    string SourceAlias,
    string SourcePath,
    string Raw)
{
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
