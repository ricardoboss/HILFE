using StepLang.Expressions;
using StepLang.Expressions.Results;
using StepLang.Parsing.Nodes;

namespace StepLang.Interpreting;

public partial class Interpreter
{
	public void Visit(CallStatementNode statementNode)
	{
		var variable = CurrentScope.GetVariable(statementNode.CallExpression.Identifier);
		if (variable.Value is not FunctionResult function)
		{
			throw new InvalidResultTypeException(statementNode.CallExpression.Location, variable.Value,
				ResultType.Function);
		}

		// TODO: check if function returns anything other than void and abort call

		var result = function.Value.Invoke(statementNode.Location, this, statementNode.CallExpression.Arguments);
		if (result is not VoidResult)
		{
			throw new InvalidResultTypeException(statementNode.Location, result, ResultType.Void);
		}
	}

	public ExpressionResult Evaluate(CallExpressionNode expressionNode)
	{
		var variable = CurrentScope.GetVariable(expressionNode.Identifier);
		if (variable.Value is not FunctionResult function)
		{
			throw new InvalidResultTypeException(expressionNode.Location, variable.Value, ResultType.Function);
		}

		return function.Value.Invoke(expressionNode.Location, this, expressionNode.Arguments);
	}

	public ExpressionResult Evaluate(FunctionDefinitionExpressionNode expressionNode)
	{
		var definition = new UserDefinedFunctionDefinition(expressionNode.Location, expressionNode.Parameters,
			expressionNode.Body, CurrentScope);

		return new FunctionResult(definition);
	}

	public ExpressionResult Evaluate(FunctionDefinitionCallExpressionNode expressionNode)
	{
		var definition = new UserDefinedFunctionDefinition(expressionNode.Location, expressionNode.Parameters,
			expressionNode.Body, CurrentScope);

		return definition.Invoke(expressionNode.Location, this, expressionNode.CallArguments);
	}

	public ExpressionResult Evaluate(NativeFunctionDefinitionExpressionNode expressionNode)
	{
		var definition = expressionNode.Definition;

		return new FunctionResult(definition);
	}
}
