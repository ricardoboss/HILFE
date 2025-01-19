using StepLang.Parsing.Nodes.Expressions;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Statements;

public sealed record IfStatementNode(
	Token IfKeywordToken,
	LinkedList<(ExpressionNode, StatementNode)> ConditionBodyMap) : StatementNode
{
	public override void Accept(IStatementVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override TokenLocation Location => IfKeywordToken.Location;
}
