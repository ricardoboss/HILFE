using StepLang.Expressions.Results;
using StepLang.Parsing;
using StepLang.Tokenizing;

namespace StepLang.LSP.Diagnostics;

public class VariableCollector : IStatementVisitor, IRootNodeVisitor, IExpressionEvaluator, IImportNodeVisitor
{
	public VariableScope RootScope { get; }

	private VariableScope currentScope;

	public VariableCollector()
	{
		RootScope = new VariableScope(new TokenLocation());
		currentScope = RootScope;
	}

	private void PushScope(Token openToken)
	{
		currentScope = currentScope.EnterChildScope(openToken.Location);
	}

	private void PopScope(Token closeToken)
	{
		currentScope.CloseLocation = closeToken.Location;

		currentScope = currentScope.Parent ?? throw new InvalidOperationException("Cannot pop root scope");
	}

	private void TrackUsage(Token identifier)
	{
		var identifierName = identifier.Value;
		if (!currentScope.Usages.TryGetValue(identifierName, out var usagesList))
		{
			usagesList = [];
			currentScope.Usages.Add(identifierName, usagesList);
		}

		usagesList.Add(identifier);
	}

	private void TrackDeclaration(IVariableDeclarationNode declaration)
	{
		if (currentScope.Declarations.TryAdd(declaration.Identifier.Value, declaration))
			return;

		throw new InvalidOperationException(
			$"Variable '{declaration.Identifier.Value}' is already declared in the current scope");
	}

	private void Visit(IEnumerable<StatementNode> nodes)
	{
		foreach (var n in nodes)
		{
			n.Accept(this);
		}
	}

	private void Evaluate(ExpressionNode node) => node.EvaluateUsing(this);

	private void Evaluate(IEnumerable<ExpressionNode> nodes)
	{
		foreach (var n in nodes)
		{
			_ = n.EvaluateUsing(this);
		}
	}

	public void Visit(RootNode node) => Visit(node.Body);

	public void Visit(CallStatementNode statementNode)
	{
		TrackUsage(statementNode.CallExpression.Identifier);

		Evaluate(statementNode.CallExpression.Arguments);
	}

	public void Visit(CodeBlockStatementNode statementNode)
	{
		PushScope(statementNode.OpenCurlyBraceToken);

		Visit(statementNode.Statements);

		PopScope(statementNode.CloseCurlyBraceToken);
	}

	public void Visit(ContinueStatementNode statementNode) { }

	public void Visit(ForeachDeclareKeyDeclareValueStatementNode statementNode)
	{
		PushScope(statementNode.ForeachKeywordToken);

		TrackDeclaration(statementNode.KeyDeclaration);
		TrackDeclaration(statementNode.ValueDeclaration);

		Evaluate(statementNode.Collection);

		Visit(statementNode.Body);

		PopScope(statementNode.Body.CloseCurlyBraceToken);
	}

	public void Visit(BreakStatementNode statementNode) { }

	public void Visit(ForeachDeclareKeyValueStatementNode statementNode)
	{
		PushScope(statementNode.ForeachKeywordToken);

		TrackDeclaration(statementNode.KeyDeclaration);
		TrackUsage(statementNode.ValueIdentifier);

		Evaluate(statementNode.Collection);

		Visit(statementNode.Body);

		PopScope(statementNode.Body.CloseCurlyBraceToken);
	}

	public void Visit(ForeachDeclareValueStatementNode statementNode)
	{
		PushScope(statementNode.ForeachKeywordToken);

		TrackDeclaration(statementNode.ValueDeclaration);

		Evaluate(statementNode.Collection);

		Visit(statementNode.Body);

		PopScope(statementNode.Body.CloseCurlyBraceToken);
	}

	public void Visit(ForeachKeyValueStatementNode statementNode)
	{
		PushScope(statementNode.ForeachKeywordToken);

		TrackUsage(statementNode.KeyIdentifier);
		TrackUsage(statementNode.ValueIdentifier);

		Evaluate(statementNode.Collection);

		Visit(statementNode.Body);

		PopScope(statementNode.Body.CloseCurlyBraceToken);
	}

	public void Visit(ForeachKeyDeclareValueStatementNode statementNode)
	{
		PushScope(statementNode.ForeachKeywordToken);

		TrackUsage(statementNode.KeyIdentifier);
		TrackDeclaration(statementNode.ValueDeclaration);

		Evaluate(statementNode.Collection);

		Visit(statementNode.Body);

		PopScope(statementNode.Body.CloseCurlyBraceToken);
	}

	public void Visit(ForeachValueStatementNode statementNode)
	{
		PushScope(statementNode.ForeachKeywordToken);

		TrackUsage(statementNode.Identifier);

		Evaluate(statementNode.Collection);

		Visit(statementNode.Body);

		PopScope(statementNode.Body.CloseCurlyBraceToken);
	}

	public void Visit(IdentifierIndexAssignmentNode statementNode)
	{
		TrackUsage(statementNode.Identifier);
		Evaluate(statementNode.IndexExpressions);
		Evaluate(statementNode.ValueExpression);
	}

	public void Visit(IfStatementNode statementNode)
	{
		foreach (var (condition, statements) in statementNode.ConditionBodyMap)
		{
			Evaluate(condition);

			Visit(statements);
		}
	}

	public void Visit(ReturnExpressionStatementNode statementNode)
	{
		Evaluate(statementNode.Expression);
	}

	public void Visit(VariableAssignmentNode statementNode)
	{
		TrackUsage(statementNode.Identifier);

		Evaluate(statementNode.Expression);
	}

	public void Visit(WhileStatementNode statementNode)
	{
		Evaluate(statementNode.Condition);

		Visit(statementNode.Body);
	}

	public void Visit(IncrementStatementNode statementNode)
	{
		TrackUsage(statementNode.Identifier);
	}

	public void Visit(DecrementStatementNode statementNode)
	{
		TrackUsage(statementNode.Identifier);
	}

	public void Visit(VariableDeclarationStatementNode statementNode)
	{
		TrackDeclaration(statementNode.Declaration);
	}

	public void Visit(ReturnStatementNode statementNode) { }

	public void Visit(DiscardStatementNode discardStatementNode)
	{
		Evaluate(discardStatementNode.Expression);
	}

	public ExpressionResult Evaluate(CallExpressionNode expressionNode)
	{
		TrackUsage(expressionNode.Identifier);
		Evaluate(expressionNode.Arguments);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(FunctionDefinitionExpressionNode expressionNode)
	{
		foreach (var parameter in expressionNode.Parameters)
		{
			TrackDeclaration(parameter);
		}

		Visit(expressionNode.Body);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(IdentifierExpressionNode expressionNode)
	{
		TrackUsage(expressionNode.Identifier);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(ListExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Expressions);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(LiteralExpressionNode expressionNode) => VoidResult.Instance;

	public ExpressionResult Evaluate(MapExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Expressions.Values);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(AddExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(CoalesceExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(NotEqualsExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(EqualsExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(NegateExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Expression);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(SubtractExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(MultiplyExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(DivideExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(ModuloExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(PowerExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(GreaterThanExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(LessThanExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(GreaterThanOrEqualExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(LessThanOrEqualExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(LogicalAndExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(LogicalOrExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseXorExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseAndExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseOrExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseShiftLeftExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseShiftRightExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseRotateLeftExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(BitwiseRotateRightExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Right);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(NotExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Expression);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(FunctionDefinitionCallExpressionNode expressionNode)
	{
		foreach (var declarationNode in expressionNode.Parameters)
		{
			TrackDeclaration(declarationNode);
		}

		Visit(expressionNode.Body);

		Evaluate(expressionNode.CallArguments);

		return VoidResult.Instance;
	}

	public ExpressionResult Evaluate(NativeFunctionDefinitionExpressionNode expressionNode) => VoidResult.Instance;

	public ExpressionResult Evaluate(IndexAccessExpressionNode expressionNode)
	{
		Evaluate(expressionNode.Left);
		Evaluate(expressionNode.Index);

		return VoidResult.Instance;
	}

	public void Visit(ImportNode importNode)
	{
		throw new NotImplementedException("Cannot yet handle definitions in imported files");
	}
}
