namespace Lathe.Core.Features.Sources;

public sealed class SourceResolutionException(IReadOnlyList<string> errors)
    : Exception("One or more log sources could not be resolved.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
