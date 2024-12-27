using Spectre.Console;
using Spectre.Console.Cli;
using StepLang.Tooling.CLI.Widgets;
using StepLang.Tooling.Meta;
using System.Runtime.InteropServices;

namespace StepLang.Tooling.CLI;

public sealed class OptionInterceptor(
	IAnsiConsole console,
	IMetadataProvider executingAssemblyMetadataProvider,
	IReadOnlyDictionary<string, IMetadataProvider> componentMetadataProviders,
	Action onHandled
) : ICommandInterceptor
{
	public void Intercept(CommandContext context, CommandSettings settings)
	{
		if (settings is not IGlobalCommandSettings globalSettings)
			return;

		if (globalSettings.Version)
		{
			HandleVersionOption();

			onHandled();
		}
		else if (globalSettings.Info)
		{
			HandleInfoOption();

			onHandled();
		}
	}

	private void HandleVersionOption()
	{
		console.WriteLine(executingAssemblyMetadataProvider.FullSemVer);
	}

	private void HandleInfoOption()
	{
		var definitionList = new DefinitionList();

		foreach (var (componentName, metadataProvider) in componentMetadataProviders)
		{
			var componentHeader = new Markup($"[dim]{componentName}[/]");
			var componentGrid = BuildComponentInformation(metadataProvider);

			definitionList.Items.Add(new DefinitionListItem(componentHeader, componentGrid));
		}

		var environmentHeader = new Markup("[dim]Environment[/]");
		var environmentGrid = BuildEnvironmentInformation();
		definitionList.Items.Add(new DefinitionListItem(environmentHeader, environmentGrid));

		console.Write(definitionList);
	}

	private static Grid BuildComponentInformation(IMetadataProvider metadataProvider)
	{
		var data = new Dictionary<string, string>
		{
			{ "Build Date", $"{metadataProvider.BuildTime:yyyy-MM-dd HH:mm:ss} UTC" },
			{ "Version", $"{metadataProvider.Sha} ({metadataProvider.FullSemVer})" },
			{ "Branch", metadataProvider.BranchName },
		};

		return BuildGrid(data);
	}

	private static Grid BuildEnvironmentInformation()
	{
		var data = new Dictionary<string, string>
		{
			{ "CLR Version", Environment.Version.ToString() },
			{ "OS Version", $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})" },
		};

		return BuildGrid(data);
	}

	private static Grid BuildGrid(Dictionary<string, string> data)
	{
		var infoGrid = new Grid().AddColumns(2);

		var headerStyle = new Style(decoration: Decoration.Bold);

		foreach (var (name, value) in data)
		{
			var header = new Text(name, headerStyle).RightJustified();
			var content = new Text(value);

			_ = infoGrid.AddRow(header, content);
		}

		return infoGrid;
	}
}
