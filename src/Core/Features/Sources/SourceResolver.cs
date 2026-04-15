using Lathe.Core.Domain;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Lathe.Core.Features.Sources;

public sealed class SourceResolver
{
    public IReadOnlyList<ResolvedLogSource> Resolve(ResolveSourcesRequest request)
    {
        var sources = new Dictionary<string, ResolvedLogSource>(PathComparer());
        var errors = new List<string>();

        foreach (var file in request.Files)
        {
            var fullPath = Path.GetFullPath(file, request.WorkingDirectory);

            if (!File.Exists(fullPath))
            {
                errors.Add($"File not found: {fullPath}");
                continue;
            }

            sources[$"file:{fullPath}"] = new ResolvedLogSource(Path.GetFileName(fullPath), fullPath, LogSourceKind.File);
        }

        foreach (var pattern in request.Globs)
        {
            var matchedFiles = ExpandGlob(pattern, request.WorkingDirectory).ToList();

            if (matchedFiles.Count == 0)
            {
                errors.Add($"Glob matched no files: {pattern}");
                continue;
            }

            foreach (var fullPath in matchedFiles)
            {
                sources[$"file:{fullPath}"] = new ResolvedLogSource(Path.GetFileName(fullPath), fullPath, LogSourceKind.File);
            }
        }

        foreach (var directory in request.RollingDirectories)
        {
            var fullPath = Path.GetFullPath(directory, request.WorkingDirectory);

            if (!Directory.Exists(fullPath))
            {
                errors.Add($"Rolling directory not found: {fullPath}");
                continue;
            }

            var latestFile = GetLatestFile(fullPath);

            if (latestFile is null)
            {
                errors.Add($"Rolling directory has no files to tail yet: {fullPath}");
                continue;
            }

            sources[$"rolling:{fullPath}"] = new ResolvedLogSource(GetRollingLabel(fullPath), latestFile, LogSourceKind.RollingDirectory, fullPath);
        }

        if (errors.Count > 0)
        {
            throw new SourceResolutionException(errors);
        }

        return sources.Values.ToList();
    }

    public static string? GetLatestFile(string directory) => Directory
        .EnumerateFiles(directory)
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .ThenByDescending(path => path, PathComparer())
        .FirstOrDefault();

    private static IEnumerable<string> ExpandGlob(string pattern, string workingDirectory)
    {
        var split = SplitPattern(pattern, workingDirectory);

        if (!Directory.Exists(split.BaseDirectory))
        {
            return [];
        }

        var matcher = new Matcher(StringComparisonFromPlatform());
        matcher.AddInclude(split.Pattern);

        var results = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(split.BaseDirectory)));
        return results.Files
            .Select(match => Path.GetFullPath(Path.Combine(split.BaseDirectory, match.Path)))
            .Where(File.Exists)
            .Distinct(PathComparer())
            .OrderBy(path => path, PathComparer());
    }

    private static (string BaseDirectory, string Pattern) SplitPattern(string pattern, string workingDirectory)
    {
        var fullPattern = Path.GetFullPath(pattern, workingDirectory);
        var root = Path.GetPathRoot(fullPattern) ?? workingDirectory;
        var remainder = fullPattern[root.Length..]
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var segments = remainder.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        var baseSegments = new List<string>();
        var patternSegments = new List<string>();
        var sawWildcard = false;

        foreach (var segment in segments)
        {
            if (!sawWildcard && !segment.Contains('*') && !segment.Contains('?'))
            {
                baseSegments.Add(segment);
                continue;
            }

            sawWildcard = true;
            patternSegments.Add(segment);
        }

        if (!sawWildcard)
        {
            var exactDirectory = Path.GetDirectoryName(fullPattern) ?? workingDirectory;
            var exactName = Path.GetFileName(fullPattern);
            return (exactDirectory, exactName);
        }

        var baseDirectory = baseSegments.Count == 0
            ? root
            : Path.Combine([root, .. baseSegments]);

        return (baseDirectory, string.Join('/', patternSegments));
    }

    private static string GetRollingLabel(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);

        return string.IsNullOrWhiteSpace(name) ? trimmed : name;
    }

    private static StringComparer PathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static StringComparison StringComparisonFromPlatform() =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
