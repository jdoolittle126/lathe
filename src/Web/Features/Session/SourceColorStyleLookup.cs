using Lathe.Core.Domain;

namespace Lathe.Web.Features.Session;

public sealed class SourceColorStyleLookup
{
    private readonly IReadOnlyDictionary<string, string> _pathStyles;
    private readonly IReadOnlyList<RollingSourceStyle> _rollingSourceStyles;

    private SourceColorStyleLookup(
        IReadOnlyDictionary<string, string> pathStyles,
        IReadOnlyList<RollingSourceStyle> rollingSourceStyles)
    {
        _pathStyles = pathStyles;
        _rollingSourceStyles = rollingSourceStyles;
    }

    public static SourceColorStyleLookup Create(IReadOnlyList<ResolvedLogSource> sources)
    {
        var pathStyles = new Dictionary<string, string>(PathComparer());
        var rollingSourceStyles = new List<RollingSourceStyle>();

        foreach (var source in sources)
        {
            var style = CreateStyle(GetSourceColorKey(source));
            pathStyles[source.Path] = style;

            if (source.Kind == LogSourceKind.RollingDirectory &&
                !string.IsNullOrWhiteSpace(source.RollingDirectoryPath))
            {
                pathStyles[source.RollingDirectoryPath] = style;
                rollingSourceStyles.Add(new RollingSourceStyle(source.RollingDirectoryPath, style));
            }
        }

        return new SourceColorStyleLookup(pathStyles, rollingSourceStyles);
    }

    public string GetStyle(ResolvedLogSource source) =>
        _pathStyles.TryGetValue(source.Path, out var style)
            ? style
            : CreateStyle(GetSourceColorKey(source));

    public string GetStyle(LogEventRecord logEvent)
    {
        if (_pathStyles.TryGetValue(logEvent.SourcePath, out var style))
        {
            return style;
        }

        foreach (var rollingSourceStyle in _rollingSourceStyles)
        {
            if (IsPathInDirectory(logEvent.SourcePath, rollingSourceStyle.DirectoryPath))
            {
                return rollingSourceStyle.Style;
            }
        }

        return CreateStyle($"file:{logEvent.SourcePath}");
    }

    private static string GetSourceColorKey(ResolvedLogSource source) =>
        source.Kind == LogSourceKind.RollingDirectory
            ? $"rolling:{source.RollingDirectoryPath ?? source.Path}"
            : $"file:{source.Path}";

    private static string CreateStyle(string key)
    {
        var hue = GetStableHue(key);
        return $"--source-accent: hsl({hue}, 72%, 62%); --source-tint: hsla({hue}, 72%, 58%, 0.10);";
    }

    private static int GetStableHue(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;

        foreach (var character in value)
        {
            hash ^= char.ToUpperInvariant(character);
            hash *= prime;
        }

        return (int)(hash % 360);
    }

    private static bool IsPathInDirectory(string path, string directoryPath)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectoryPath = EnsureTrailingDirectorySeparator(Path.GetFullPath(directoryPath));

        return fullPath.StartsWith(fullDirectoryPath, PathComparison());
    }

    private static string EnsureTrailingDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : $"{path}{Path.DirectorySeparatorChar}";

    private static StringComparer PathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static StringComparison PathComparison() =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private sealed record RollingSourceStyle(string DirectoryPath, string Style);
}
