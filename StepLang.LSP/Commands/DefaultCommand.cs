using Lunet.Extensions.Logging.SpectreConsole;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using StepLang.Tooling.CLI;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
		var options = new ServerOptions
		{
			Host = settings.Host ?? "127.0.0.1", Port = settings.Port ?? 14246, UseStandardIO = settings.Stdio,
		};

		SpectreConsoleLoggerProvider? loggerProvider = null;
		if (!settings.Stdio)
		{
			// only when NOT using stdio, we add a logger that can use stdio for logging
			var loggerOptions = new SpectreConsoleLoggerOptions
			{
				SingleLine = true,
				IncludeNewLineBeforeMessage = false,
				LogLevelFormatter = LogLevelFormatter,
				CategoryFormatter = CategoryFormatter,
			};

			loggerProvider = new SpectreConsoleLoggerProvider(loggerOptions);

			options.LoggerFactory = new LoggerFactory([loggerProvider]);
		}

		var server = new ServerManager(options);

		var exitCode = await server.RunAsync();

		loggerProvider?.Dispose();

		return exitCode;
	}

	private static void CategoryFormatter(SpectreConsoleLoggerOptions _, StringBuilder s, string c)
	{
		var className = c.Split('.').Last();
		s.Append(className);
		s.Append(": ");
	}

	private static void LogLevelFormatter(SpectreConsoleLoggerOptions _, StringBuilder s, LogLevel l)
	{
		s.Append("[[");
		s.Append(GetLogLevelMarkup(l));
		s.Append("]] ");
	}

	private static string GetLogLevelMarkup(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace => "[silver]trce[/]",
			LogLevel.Debug => "[bold silver]dbug[/]",
			LogLevel.Information => "[green]info[/]",
			LogLevel.Warning => "[yellow]warn[/]",
			LogLevel.Error => "[black on maroon]fail[/]",
			LogLevel.Critical => "[white on maroon]crit[/]",
			LogLevel.None => "[silver]none[/]",
			_ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
		};
	}
}
