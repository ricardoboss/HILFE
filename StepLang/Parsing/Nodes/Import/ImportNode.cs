using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Import;

public sealed record ImportNode(Token PathToken) : IImportNode
{
	public void Accept(IImportNodeVisitor visitor)
	{
		visitor.Visit(this);
	}

	public TokenLocation Location => PathToken.Location;
}
