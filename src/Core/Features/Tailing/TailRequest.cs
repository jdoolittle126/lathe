using Lathe.Core.Domain;

namespace Lathe.Core.Features.Tailing;

public sealed record TailRequest(
    IReadOnlyList<ResolvedLogSource> Sources,
    int Count,
    bool Follow);
