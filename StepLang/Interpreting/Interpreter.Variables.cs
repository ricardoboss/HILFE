using StepLang.Expressions.Results;
using StepLang.Parsing;
using StepLang.Parsing.Nodes.Statements;
using StepLang.Parsing.Nodes.VariableDeclarations;

namespace StepLang.Interpreting;

public partial class Interpreter : IVariableDeclarationEvaluator
{
	public void Visit(VariableDeclarationStatementNode statementNode)
	{
		_ = statementNode.Declaration.EvaluateUsing(this);
	}

	public Variable Evaluate(VariableDeclarationNode expressionNode)
	{
		var validResults = expressionNode.GetResultTypes().ToList();

		return CurrentScope.CreateVariable(expressionNode.Location, expressionNode.Identifier, validResults,
			ExpressionResult.DefaultFor(validResults.First()));
	}

	public Variable Evaluate(NullableVariableDeclarationNode expressionNode)
	{
		var validResults = expressionNode.GetResultTypes().ToList();

		return CurrentScope.CreateVariable(expressionNode.Location, expressionNode.Identifier, validResults,
			NullResult.Instance, true);
	}

	public Variable Evaluate(VariableInitializationNode expressionNode)
	{
		var validResults = expressionNode.GetResultTypes().ToList();

		return CurrentScope.CreateVariable(expressionNode.Location, expressionNode.Identifier, validResults,
			expressionNode.Expression.EvaluateUsing(this));
	}

	public Variable Evaluate(NullableVariableInitializationNode expressionNode)
	{
		var validResults = expressionNode.GetResultTypes().ToList();

		return CurrentScope.CreateVariable(expressionNode.Location, expressionNode.Identifier, validResults,
			expressionNode.Expression.EvaluateUsing(this), true);
	}

	public Variable Evaluate(ErrorVariableDeclarationNode variableDeclarationNode)
	{
		throw new NotSupportedException("Cannot evaluate error variable declaration node");
	}

	public void Visit(VariableAssignmentNode statementNode)
	{
		var variable = CurrentScope.GetVariable(statementNode.Identifier);

		var result = statementNode.Expression.EvaluateUsing(this);

		variable.Assign(statementNode.Location, result);
	}
}
