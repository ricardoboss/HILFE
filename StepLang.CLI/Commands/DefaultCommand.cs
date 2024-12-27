using Spectre.Console;
using Spectre.Console.Cli;
using StepLang.Tooling.CLI;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.CLI.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
{
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	public sealed class Settings : VisibleGlobalCommandSettings
	{
		[CommandArgument(0, "[file]")]
		[Description("The path to a .step-file to run.")]
		[DefaultValue(null)]
		public string? File { get; init; }
	}

	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		if (settings.File == null)
		{
			AnsiConsole.MarkupLine("[red]No file specified.[/]");

			return 1;
		}

		var runCommand = new RunCommand();

		return await runCommand.ExecuteAsync(context, new RunCommand.Settings { File = settings.File });
	}
}
