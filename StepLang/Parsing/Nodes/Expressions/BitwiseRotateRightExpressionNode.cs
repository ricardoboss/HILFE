using StepLang.Expressions.Results;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Expressions;

public record BitwiseRotateRightExpressionNode(
	TokenLocation OperatorLocation,
	ExpressionNode Left,
	ExpressionNode Right)
	: BinaryExpressionNode(OperatorLocation, Left, Right, BinaryExpressionOperator.BitwiseRotateRight)
{
	public override ExpressionResult EvaluateUsing(IExpressionEvaluator evaluator)
	{
		return evaluator.Evaluate(this);
	}
}
