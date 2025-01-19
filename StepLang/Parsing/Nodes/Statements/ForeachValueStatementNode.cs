using StepLang.Parsing.Nodes.Expressions;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Statements;

public sealed record ForeachValueStatementNode(
	Token ForeachKeywordToken,
	Token Identifier,
	ExpressionNode Collection,
	StatementNode Body) : StatementNode
{
	public override void Accept(IStatementVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override TokenLocation Location => ForeachKeywordToken.Location;
}
