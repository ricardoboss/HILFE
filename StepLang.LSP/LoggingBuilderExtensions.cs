using Lunet.Extensions.Logging.SpectreConsole;
using Microsoft.Extensions.Logging;
using System.Text;

namespace StepLang.LSP;

internal static class LoggingBuilderExtensions
{
	public static ILoggingBuilder AddSimpleSpectreConsole(this ILoggingBuilder builder)
	{
		return builder.AddSpectreConsole(new SpectreConsoleLoggerOptions
		{
			SingleLine = true,
			IncludeNewLineBeforeMessage = false,
			LogLevelFormatter = LogLevelFormatter,
			CategoryFormatter = CategoryFormatter,
		});
	}

	private static void CategoryFormatter(SpectreConsoleLoggerOptions _, StringBuilder s, string c)
	{
		var className = c.Split('.').Last();
		s.Append("[dim]");
		s.Append(className);
		s.Append("[/]: ");
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
