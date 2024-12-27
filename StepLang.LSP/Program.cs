using Spectre.Console;
using Spectre.Console.Cli;
using StepLang.LSP.Commands;
using StepLang.Tooling.CLI;

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
		gitVersionProvider: new GitVersionProvider(),
		buildTimeProvider: new BuildTimeProvider()
	);

	config.SetInterceptor(interceptor);

	config.AddExample("--stdio");
	config.AddExample("--port 12345");
	config.AddExample("--version");

});

return await app.RunAsync(args);

file sealed class GitVersionProvider : IGitVersionProvider
{
	public string FullSemVer => GitVersionInformation.FullSemVer;
	public string Sha => GitVersionInformation.Sha;
	public string BranchName => GitVersionInformation.BranchName;
}

file sealed class BuildTimeProvider : IBuildTimeProvider
{
	public DateTimeOffset BuildTimeUtc => DateTimeOffset.UtcNow;
}
