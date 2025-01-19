using StepLang.Parsing.Nodes.Expressions;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Statements;

public sealed record CallStatementNode(ExpressionNode CallExpression) : StatementNode
{
	public override void Accept(IStatementVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override TokenLocation Location => CallExpression.Location;
}
