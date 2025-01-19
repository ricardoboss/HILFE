using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Import;

public sealed record ErrorImportNode(string Description, IReadOnlyCollection<Token?> Tokens) : IImportNode
{
	public void Accept(IImportNodeVisitor visitor)
	{
		visitor.Visit(this);
	}

	public TokenLocation Location => Tokens.FirstOrDefault()?.Location ?? new TokenLocation();
}
