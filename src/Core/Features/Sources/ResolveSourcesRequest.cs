namespace Lathe.Core.Features.Sources;

public sealed record ResolveSourcesRequest(
    string WorkingDirectory,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Globs,
    IReadOnlyList<string> RollingDirectories);
