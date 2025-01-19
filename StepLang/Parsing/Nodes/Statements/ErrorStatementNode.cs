using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Statements;

public record ErrorStatementNode(string Description, IReadOnlyCollection<Token?> Tokens) : StatementNode
{
	public override void Accept(IStatementVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override TokenLocation Location => Tokens.FirstOrDefault()?.Location ?? new TokenLocation();
}
