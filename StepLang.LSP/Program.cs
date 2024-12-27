using Spectre.Console;
using Spectre.Console.Cli;
using StepLang;
using StepLang.LSP;
using StepLang.LSP.Commands;
using StepLang.Tooling.CLI;
using StepLang.Tooling.Meta;

const string slogan = "STEP LSP - LSP Server for STEP";

var app = new CommandApp<DefaultCommand>()
		.WithDescription(slogan + Environment.NewLine + "Version: " + GitVersionInformation.FullSemVer)
	;

app.Configure(config =>
{
	config
		.SetApplicationName("step-lsp")
		.SetApplicationVersion(GitVersionInformation.FullSemVer)
		.CaseSensitivity(CaseSensitivity.None)
		.UseStrictParsing()
#if DEBUG
		.ValidateExamples()
#endif
		;

	var interceptor = new OptionInterceptor(
		config.Settings.Console ?? AnsiConsole.Console,
		LspMetadataProvider.Instance,
		new Dictionary<string, IMetadataProvider>
		{
			{ "Core", CoreMetadataProvider.Instance },
			{ "LSP Server", LspMetadataProvider.Instance },
		}
	);

	config.SetInterceptor(interceptor);

	config.AddExample("--stdio");
	config.AddExample("--port=12345");
	config.AddExample("--version");

});

return await app.RunAsync(args);
