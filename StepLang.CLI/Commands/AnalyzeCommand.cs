using Spectre.Console;
using Spectre.Console.Cli;
using StepLang.Diagnostics;
using StepLang.Parsing;
using StepLang.Tokenizing;
using StepLang.Tooling.CLI;
using StepLang.Tooling.CLI.Widgets;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.CLI.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class AnalyzeCommand : AsyncCommand<AnalyzeCommand.Settings>
{
	public sealed class Settings : HiddenGlobalCommandSettings
	{
		[CommandArgument(0, "[fileOrDir]")]
		[DefaultValue(null)]
		[Description("The path to a directory or a .step-file to analyze.")]
		public string? FileOrDir { get; init; }

		[CommandOption("-e|--set-exit-code")]
		[DefaultValue(false)]
		[Description("Set the exit code to be non-zero if any files contain errors.")]
		public bool SetExitCode { get; init; }
	}

	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		var fileCount = 0;
		var severityCounts = Enum.GetValues<Severity>().ToDictionary(severity => severity, _ => 0);

		var diagnostics = new DiagnosticCollection();
		diagnostics.CollectionChanged += (_, e) =>
		{
			if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null })
			{
				ReportDiagnostics(e.NewItems);
			}
		};

		await AnsiConsole.Status()
			.AutoRefresh(true)
			.StartAsync("Analyzing files...", async ctx =>
			{
				var files = CollectFiles(settings.FileOrDir);

				foreach (var file in files)
				{
					fileCount++;

					diagnostics.Clear();

					_ = ctx.Status($"[dim]Analyzing [default]{file.FullName}[/]...[/]");

					try
					{
						var source = await CharacterSource.FromFileAsync(file);
						var tokenizer = new Tokenizer(source, diagnostics);
						var tokens = tokenizer.Tokenize();
						var parser = new Parser(tokens, diagnostics);
						_ = parser.ParseRoot();

						foreach (var d in diagnostics)
						{
							severityCounts[d.Severity]++;
						}
					}
#pragma warning disable CA1031
					catch (Exception e)
#pragma warning restore CA1031
					{
						AnsiConsole.MarkupLine($"[red]Failed to analyze [bold]{file.FullName}[/][/]: {e.Message}");

						severityCounts[Severity.Error]++;
					}
				}
			});

		AnsiConsole.WriteLine();

		AnsiConsole.MarkupLine($"[dim][default bold]{fileCount} file{(fileCount == 1 ? "" : "s")}[/] analyzed.[/]");
		AnsiConsole.Markup(
			$"[dim][bold red]{severityCounts[Severity.Error]}[/] error{(severityCounts[Severity.Error] == 1 ? "" : "s")}, [yellow]{severityCounts[Severity.Warning]}[/] warning{(severityCounts[Severity.Warning] == 1 ? "" : "s")}, [green]{severityCounts[Severity.Suggestion]}[/] suggestion{(severityCounts[Severity.Suggestion] == 1 ? "" : "s")} and [aqua]{severityCounts[Severity.Hint]}[/] hint{(severityCounts[Severity.Hint] == 1 ? "" : "s")}.[/]");

		if (settings.SetExitCode)
		{
			return severityCounts[Severity.Error] > 0 ? 1 : 0;
		}

		return 0;
	}

	private static IEnumerable<FileInfo> CollectFiles(string? fileOrDir)
	{
		var searchDirectory = ".";

		if (fileOrDir is not null)
		{
			var node = new FileInfo(fileOrDir);
			if (node.Exists)
			{
				yield return node;

				yield break;
			}

			searchDirectory = fileOrDir;
		}

		foreach (var file in Directory.EnumerateFiles(searchDirectory, "*.step", SearchOption.AllDirectories))
		{
			yield return new FileInfo(file);
		}
	}

	private static void ReportDiagnostics(IReadOnlyList<Diagnostic> eNewItems)
	{
		foreach (var diagnostic in eNewItems)
		{
			var line = new DiagnosticLine(diagnostic);

			AnsiConsole.Write(line);
		}
	}
}
