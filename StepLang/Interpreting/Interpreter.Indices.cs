using StepLang.Expressions;
using StepLang.Expressions.Results;
using StepLang.Framework;
using StepLang.Parsing.Nodes.Expressions;
using StepLang.Parsing.Nodes.Statements;
using System.Globalization;

namespace StepLang.Interpreting;

public partial class Interpreter
{
	public void Visit(IdentifierIndexAssignmentNode statementNode)
	{
		BaseVariable variable = CurrentScope.GetVariable(statementNode.Identifier);

		foreach (var indexExpression in statementNode.IndexExpressions)
		{
			var indexResult = indexExpression.EvaluateUsing(this);
			switch (variable.Value)
			{
				case ListResult { Value: var list } when indexResult is NumberResult index:
					variable = new DelegateVariable
					{
						Evaluator = () => list[index],
						Assigner = (_, value) => list[index] = value,
					};

					break;
				case MapResult { Value: var map } when indexResult is StringResult { Value: var key }:
					variable = new DelegateVariable
					{
						Evaluator = () => map[key],
						Assigner = (_, value) => map[key] = value,
					};

					break;
				default:
					var indexRepresentation = indexResult switch
					{
						StringResult stringResult => stringResult.Value,
						NumberResult numberResult => numberResult.Value.ToString(CultureInfo.InvariantCulture),
						_ => indexResult.ToString(),
					};

					throw new InvalidIndexOperatorException(indexRepresentation, variable.Value.ResultType, "assign");
			}
		}

		var valueResult = statementNode.ValueExpression.EvaluateUsing(this);

		variable.Assign(statementNode.AssignmentToken.Location, valueResult);
	}

	public ExpressionResult Evaluate(IndexAccessExpressionNode expressionNode)
	{
		var valueResult = expressionNode.Left.EvaluateUsing(this);
		var indexResult = expressionNode.Index.EvaluateUsing(this);

		switch (valueResult)
		{
			case ListResult { Value: var values } when indexResult is NumberResult index:
				{
					if (index < 0 || index >= values.Count)
					{
						throw new IndexOutOfBoundsException(expressionNode.Location, index, values.Count);
					}

					return values[index];
				}
			case MapResult { Value: var pairs } when indexResult is StringResult { Value: var key }:
				return pairs[key];
			case StringResult { Value: var str } when indexResult is NumberResult index:
				{
					var grapheme = str.GraphemeAt(index);
					if (grapheme == null)
					{
						throw new IndexOutOfBoundsException(expressionNode.Location, index, str.GraphemeLength());
					}

					return new StringResult(grapheme);
				}
			default:
				{
					var indexRepresentation = indexResult switch
					{
						StringResult stringResult => stringResult.Value,
						NumberResult numberResult => numberResult.Value.ToString(CultureInfo.InvariantCulture),
						_ => indexResult.ToString(),
					};

					throw new InvalidIndexOperatorException(indexRepresentation, valueResult.ResultType, "access");
				}
		}
	}
}
