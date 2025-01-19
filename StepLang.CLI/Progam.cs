using Spectre.Console;
using Spectre.Console.Cli;
using StepLang.CLI.Commands;
using StepLang.Tooling.CLI;
using StepLang.Tooling.Meta;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.CLI;

internal static class Program
{
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
		DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(DefaultCommand))]
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
		DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(RunCommand))]
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
		DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(FormatCommand))]
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
		DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(HighlightCommand))]
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
		DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(ParseCommand))]
	[DynamicDependency(
		DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
		DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(AnalyzeCommand))]
	public static async Task<int> Main(string[] args)
	{
		const string slogan = "STEP - Simple Transition to Elevated Programming";

		var app = new CommandApp<DefaultCommand>()
				.WithDescription(slogan + Environment.NewLine + "Version: " + GitVersionInformation.FullSemVer)
			;

		app.Configure(config =>
		{
			// ensure we are using the full width of the console
			if (Console.LargestWindowWidth > 0)
			{
				var console = AnsiConsole.Console;
				console.Profile.Width = Console.LargestWindowWidth;
				config.ConfigureConsole(console);
			}

			config
				.SetApplicationName("step")
				.SetApplicationVersion(GitVersionInformation.FullSemVer)
				.CaseSensitivity(CaseSensitivity.None)
				.UseStrictParsing()
#if DEBUG
				.ValidateExamples()
#endif
				.SetExceptionHandler(ErrorHandler.HandleException)
				;

			var interceptor = new OptionInterceptor(
				config.Settings.Console ?? AnsiConsole.Console,
				CliMetadataProvider.Instance,
				new Dictionary<string, IMetadataProvider>
				{
					{ "Core", CoreMetadataProvider.Instance },
					{ "Command Line Interface", CliMetadataProvider.Instance },
				}
			);

			config.SetInterceptor(interceptor);

			config.AddExample("script.step");
			config.AddExample("format script.step");
			config.AddExample("--version");

			config.AddCommand<RunCommand>("run")
				.WithDescription("Run a .step-file.")
				.WithExample("run my-script.step")
				;

			config.AddCommand<FormatCommand>("format")
				.WithDescription("Format one or more .step-files.")
				.WithExample("format my-script.step")
				.WithExample("format . --dry-run")
				.WithExample("format foo/bar main.step --set-exit-code")
				;

			config.AddCommand<HighlightCommand>("highlight")
				.WithDescription("Highlight a .step-file and print it to the console.")
				.WithExample("highlight my-script.step")
				.WithExample("highlight my-script.step -t mono")
				.WithExample("highlight --list-themes")
				;

			config.AddCommand<ParseCommand>("parse")
				.WithDescription("Parse a .step-file and print the AST to the console.")
				.WithExample("parse my-script.step")
				;

			config.AddCommand<AnalyzeCommand>("analyze")
				.WithAlias("analyse")
				.WithDescription("Analyze the current folder or a .step-file and print the diagnostics to the console.")
				.WithExample("analyze")
				.WithExample("analyze my-script.step")
				;
		});

		return await app.RunAsync(args);
	}
}
