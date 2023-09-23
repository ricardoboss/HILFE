using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;
using StepLang.Interpreting;
using StepLang.Statements;
using StepLang.Tokenizing;

namespace StepLang.CLI.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes")]
internal sealed class RunCommand : AsyncCommand<RunCommand.Settings>
{
    public sealed class Settings : HiddenGlobalCommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The path to a .step-file to run.")]
        public string File { get; init; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var scriptFile = new FileInfo(settings.File);

        var chars = await File.ReadAllTextAsync(scriptFile.FullName);

        var tokenizer = new Tokenizer();
        tokenizer.UpdateFile(scriptFile);
        tokenizer.Add(chars);
        var tokens = tokenizer.Tokenize();

        var parser = new StatementParser();
        await parser.AddAsync(tokens.ToAsyncEnumerable());

        var interpreter = new Interpreter(Console.Out, Console.Error, Console.In);
        var statements = parser.ParseAsync();

        await interpreter.InterpretAsync(statements);

        return interpreter.ExitCode;
    }
}