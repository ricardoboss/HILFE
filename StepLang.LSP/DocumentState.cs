using StepLang.LSP.Diagnostics;
using StepLang.Parsing;
using StepLang.Tokenizing;
using System.Diagnostics;

namespace StepLang.LSP;

public class DocumentState
{
	public required Uri DocumentUri { get; init; }

	public required string Text { get; init; }

	public required int Version { get; init; }

	public TokenCollection? Tokens { get; set; }

	public RootNode? Ast { get; set; }

	public FileSymbols? Symbols { get; set; }
}
