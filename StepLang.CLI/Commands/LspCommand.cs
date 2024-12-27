using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;
using StepLang.LSP;

namespace StepLang.CLI.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes")]
internal sealed class LspCommand : AsyncCommand<LspCommand.Settings>
{
    public sealed class Settings : HiddenGlobalCommandSettings
    {
        [CommandOption("-h|--host")]
        [Description("The host to bind to.")]
        [DefaultValue("127.0.0.1")]
        public string? Host { get; init; }

        [CommandOption("-p|--port")]
        [Description("The port to bind to.")]
        [DefaultValue(14246)]
        public int? Port { get; init; }

        [CommandOption("-s|--stdio")]
        [Description("Use stdio for input/output.")]
        public bool Stdio { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var options = new ServerOptions
        {
            Host = settings.Host ?? "127.0.0.1",
            Port = settings.Port ?? 14246,
            UseStandardIO = settings.Stdio,
        };

        var server = new ServerManager();

        var exitCode = await server.RunAsync(options);

        return exitCode;
    }
}
