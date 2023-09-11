using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using Pastel;

namespace StepLang.CLI;

[ExcludeFromCodeCoverage]
internal static class ErrorHandler
{
    public static void HandleException(Exception e, InvocationContext c)
    {
        c.Console.Error.WriteLine(FormatError(e));
        c.Console.Error.WriteLine();
    }

    private static string FormatError(Exception e)
    {
        return e switch
        {
            StepLangException sle => FormatStepLangException(sle),
            _ => FormatGeneralException(e),
        };
    }

    private static string FormatError(string type, string message) => ("! " + type + ": ").Pastel(Color.OrangeRed) + message;

    private static string FormatGeneralException(Exception e) => FormatError(e.GetType().Name, e.Message + Environment.NewLine + e.StackTrace.Pastel(Color.DarkGray));

    private static string FormatStepLangException(StepLangException e)
    {
        const int contextLineCount = 4;

        IEnumerable<string> outputLines;

        var exceptionName = (" " + e.ErrorCode + ": " + e.GetType().Name + " ")
            .Pastel(ConsoleColor.White)
            .PastelBg(ConsoleColor.Red);

        var message = Environment.NewLine + "\t" + e.Message + Environment.NewLine;

        if (e.Location is { } location)
        {
            var sourceCode = location.File.Exists ? File.ReadAllText(location.File.FullName) : "";
            var lines = sourceCode.ReplaceLineEndings().Split(Environment.NewLine);
            var contextStartLine = Math.Max(0, location.Line - 1 - contextLineCount);
            var contextEndLine = Math.Min(lines.Length, location.Line + contextLineCount);
            var lineNumber = contextStartLine;
            var lineNumberWidth = contextEndLine.ToString(CultureInfo.InvariantCulture).Length;
            var contextLines = lines[contextStartLine..contextEndLine].Select(l =>
            {
                var prefix = lineNumber == location.Line - 1 ? $"{">".Pastel(ConsoleColor.Red)} " : "  ";

                var displayLineNumber = (lineNumber + 1).ToString(CultureInfo.InvariantCulture);
                var line = prefix + $"{(displayLineNumber.PadLeft(lineNumberWidth) + "|").Pastel(ConsoleColor.Gray)} {l}";

                lineNumber++;

                return line;
            });

            var locationString = $"at {location.File.FullName.Pastel(ConsoleColor.Green)}:{location.Line}";

            outputLines = contextLines.Prepend(locationString);
        }
        else
            outputLines = new[] { (e.StackTrace ?? string.Empty).Pastel(ConsoleColor.Gray) };

        outputLines = outputLines.Prepend(message).Prepend(exceptionName);

        if (e.HelpText is { } helpText)
            outputLines = outputLines.Append(Environment.NewLine + "Tip: ".Pastel(ConsoleColor.DarkCyan) + helpText);

        if (e.HelpLink is { } helpLink)
            outputLines = outputLines.Append(Environment.NewLine + "See also: ".Pastel(ConsoleColor.DarkCyan) + helpLink);

        return Environment.NewLine + string.Join(Environment.NewLine, outputLines);
    }
}