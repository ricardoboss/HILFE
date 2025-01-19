using StepLang.Expressions.Results;
using StepLang.Parsing.Nodes.Expressions;

namespace StepLang.Parsing;

public interface IExpressionEvaluator
{
	ExpressionResult Evaluate(CallExpressionNode expressionNode);
	ExpressionResult Evaluate(FunctionDefinitionExpressionNode expressionNode);
	ExpressionResult Evaluate(IdentifierExpressionNode expressionNode);
	ExpressionResult Evaluate(ListExpressionNode expressionNode);
	ExpressionResult Evaluate(LiteralExpressionNode expressionNode);
	ExpressionResult Evaluate(MapExpressionNode expressionNode);
	ExpressionResult Evaluate(AddExpressionNode expressionNode);
	ExpressionResult Evaluate(CoalesceExpressionNode expressionNode);
	ExpressionResult Evaluate(NotEqualsExpressionNode expressionNode);
	ExpressionResult Evaluate(EqualsExpressionNode expressionNode);
	ExpressionResult Evaluate(NegateExpressionNode expressionNode);
	ExpressionResult Evaluate(SubtractExpressionNode expressionNode);
	ExpressionResult Evaluate(MultiplyExpressionNode expressionNode);
	ExpressionResult Evaluate(DivideExpressionNode expressionNode);
	ExpressionResult Evaluate(ModuloExpressionNode expressionNode);
	ExpressionResult Evaluate(PowerExpressionNode expressionNode);
	ExpressionResult Evaluate(GreaterThanExpressionNode expressionNode);
	ExpressionResult Evaluate(LessThanExpressionNode expressionNode);
	ExpressionResult Evaluate(GreaterThanOrEqualExpressionNode expressionNode);
	ExpressionResult Evaluate(LessThanOrEqualExpressionNode expressionNode);
	ExpressionResult Evaluate(LogicalAndExpressionNode expressionNode);
	ExpressionResult Evaluate(LogicalOrExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseXorExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseAndExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseOrExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseShiftLeftExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseShiftRightExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseRotateLeftExpressionNode expressionNode);
	ExpressionResult Evaluate(BitwiseRotateRightExpressionNode expressionNode);
	ExpressionResult Evaluate(NotExpressionNode expressionNode);
	ExpressionResult Evaluate(FunctionDefinitionCallExpressionNode expressionNode);
	ExpressionResult Evaluate(NativeFunctionDefinitionExpressionNode expressionNode);
	ExpressionResult Evaluate(IndexAccessExpressionNode expressionNode);
	ExpressionResult Evaluate(ErrorExpressionNode expressionNode);
}
