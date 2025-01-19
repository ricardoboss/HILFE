using StepLang.Expressions.Results;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Nodes.Expressions;

public sealed record ErrorExpressionNode(string Description, IReadOnlyCollection<Token?> Tokens) : ExpressionNode
{
	public override ExpressionResult EvaluateUsing(IExpressionEvaluator evaluator)
	{
		return evaluator.Evaluate(this);
	}

	public override TokenLocation Location => Tokens.FirstOrDefault()?.Location ?? new TokenLocation();
}
