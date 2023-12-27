using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace StepLang.CLI;

internal sealed class ServiceCollectionRegistrar : ITypeRegistrar
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <inheritdoc />
    public void Register(Type service, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementation)
    {
        Services.AddSingleton(service, implementation);
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation)
    {
        Services.AddSingleton(service, implementation);
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        Services.AddTransient(service, _ => factory());
    }

    /// <inheritdoc />
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don't ignore created IDisposable")]
    public ITypeResolver Build()
    {
        return new ServiceProviderResolver(Services.BuildServiceProvider());
    }
}
