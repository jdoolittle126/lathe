using Spectre.Console.Cli;

namespace Lathe.Tool.Infrastructure.Spectre;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

    public void Dispose() => (provider as IDisposable)?.Dispose();
}
