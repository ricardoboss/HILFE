using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace StepLang.CLI;

public class ServiceProviderResolver(ServiceProvider serviceProvider) : ITypeResolver
{
    /// <inheritdoc />
    public object? Resolve(Type? type) => type is null ? null : serviceProvider.GetService(type);
}
