using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;
using StepLang.CLI.Commands;

namespace StepLang.CLI;

internal static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(DefaultCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(RunCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(FormatCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(HighlightCommand))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicNestedTypes, typeof(LspCommand))]
    public static async Task<int> Main(string[] args)
    {
        const string slogan = "STEP - Simple Transition to Elevated Programming";

        var app = new CommandApp<DefaultCommand>()
                .WithDescription(slogan + Environment.NewLine + "Version: " + GitVersionInformation.FullSemVer)
            ;

        app.Configure(config =>
        {
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

            config.SetInterceptor(new OptionInterceptor());

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

            config.AddCommand<LspCommand>("lsp")
                .WithDescription("Start the LSP server.")
                .WithExample("lsp")
                ;
        });

        return await app.RunAsync(args);
    }
}