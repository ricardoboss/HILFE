using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP;

public static class StepTextDocumentSelector
{
	public static TextDocumentSelector Instance { get; } = new(
		TextDocumentFilter.ForLanguage("STEP"),
		TextDocumentFilter.ForPattern("**/*.step")
	);
}
