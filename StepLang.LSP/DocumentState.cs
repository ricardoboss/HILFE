using StepLang.LSP.Diagnostics;
using StepLang.Parsing.Nodes;
using StepLang.Tokenizing;

namespace StepLang.LSP;

internal sealed class DocumentState
{
	public required Uri DocumentUri { get; init; }

	public required string Text { get; init; }

	public required int Version { get; init; }

	public TokenCollection? Tokens { get; set; }

	public RootNode? Ast { get; set; }

	public FileSymbols? Symbols { get; set; }

	public override string ToString()
	{
		return $"DocumentState: (v{Version}) {DocumentUri}";
	}
}
