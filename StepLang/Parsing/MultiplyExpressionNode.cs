using StepLang.Expressions.Results;

namespace StepLang.Parsing;

public record MultiplyExpressionNode(ExpressionNode Left, ExpressionNode Right) : ExpressionNode, IBinaryExpressionNode
{
    public BinaryExpressionOperator Op => BinaryExpressionOperator.Multiply;

    public override ExpressionResult EvaluateUsing(IExpressionEvaluator evaluator)
    {
        return evaluator.Evaluate(this);
    }
}