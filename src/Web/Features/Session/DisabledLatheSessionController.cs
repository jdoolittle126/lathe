namespace Lathe.Web.Features.Session;

public sealed class DisabledLatheSessionController : ILatheSessionController
{
    public static DisabledLatheSessionController Instance { get; } = new();

    public bool CanAddSources => false;

    public Task<IReadOnlyList<string>> AddInputsAsync(
        IReadOnlyList<string> files,
        IReadOnlyList<string> globs,
        IReadOnlyList<string> rollingDirectories,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(["Adding sources is only available from the hosted Lathe tool."]);
}
