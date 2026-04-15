namespace Lathe.Core.Domain;

public sealed record ResolvedLogSource(
    string DisplayName,
    string Path,
    LogSourceKind Kind,
    string? RollingDirectoryPath = null);

public enum LogSourceKind
{
    File,
    RollingDirectory,
}
