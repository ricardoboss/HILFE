using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Leap.Client;
using Spectre.Console.Cli;

namespace StepLang.CLI.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes")]
internal sealed class AddCommand(LeapApiClient apiClient) : AsyncCommand<AddCommand.Settings>
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public sealed class Settings : HiddenGlobalCommandSettings
    {
        [CommandArgument(0, "<name>")]
        [Description("The name of the library to add.")]
        public string? Name { get; init; }

        [CommandArgument(1, "[version]")]
        [Description("The version of the library to add.")]
        [DefaultValue(null)]
        public string? Version { get; init; }
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Name is not { } libraryName)
            throw new ArgumentNullException("Name is required");

        var nameParts = libraryName.Split('/');

        var author = nameParts[0];
        var name = nameParts[1];

        var library = await apiClient.GetLibraryAsync(author, name);

        Console.WriteLine("Found library: " + library?.Version);

        return 0;
    }
}