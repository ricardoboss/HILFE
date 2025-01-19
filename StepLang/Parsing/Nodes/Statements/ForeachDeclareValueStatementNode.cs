using StepLang.Parsing.Nodes.Expressions;
using StepLang.Parsing.Nodes.VariableDeclarations;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Statements;

public sealed record ForeachDeclareValueStatementNode(
	Token ForeachKeywordToken,
	IVariableDeclarationNode ValueDeclaration,
	ExpressionNode Collection,
	StatementNode Body) : StatementNode
{
	public override void Accept(IStatementVisitor visitor)
	{
		visitor.Visit(this);
	}

	public override TokenLocation Location => ForeachKeywordToken.Location;
}
