namespace Lathe.Web.Features.Session;

public interface ILatheSessionController
{
    bool CanAddSources { get; }

    Task<IReadOnlyList<string>> AddInputsAsync(
        IReadOnlyList<string> files,
        IReadOnlyList<string> globs,
        IReadOnlyList<string> rollingDirectories,
        CancellationToken cancellationToken);
}
