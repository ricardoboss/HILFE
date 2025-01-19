using StepLang.Expressions.Results;
using StepLang.Parsing.Nodes.Statements;
using StepLang.Parsing.Nodes.VariableDeclarations;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Expressions;

public record FunctionDefinitionCallExpressionNode(
	Token OpenParenthesisToken,
	IReadOnlyList<IVariableDeclarationNode> Parameters,
	StatementNode Body,
	IReadOnlyList<ExpressionNode> CallArguments) : ExpressionNode
{
	public override ExpressionResult EvaluateUsing(IExpressionEvaluator evaluator)
	{
		return evaluator.Evaluate(this);
	}

	public override TokenLocation Location => OpenParenthesisToken.Location;
}
