using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using StepLang.Tooling.CLI;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.LSP.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes")]
internal sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
{
	internal sealed class Settings : VisibleGlobalCommandSettings
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
		var services = new ServiceCollection();

		_ = services
			.AddOptions<ServerOptions>()
			.Configure(o =>
			{
				o.Host = settings.Host ?? "127.0.0.1";
				o.Port = settings.Port ?? 14246;
				o.UseStandardIO = settings.Stdio;
			});

		services.AddLogging(
			b =>
			{
				b
					.SetMinimumLevel(LogLevel.Trace)
					.ClearProviders();

				if (settings.Stdio)
					return;

				b.AddSimpleSpectreConsole();
			});

		services.AddSingleton<ServerManager>();

		await using var provider = services.BuildServiceProvider();

		var server = provider.GetRequiredService<ServerManager>();

		var exitCode = await server.RunAsync();

		return exitCode;
	}
}
