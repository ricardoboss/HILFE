using StepLang.Expressions.Results;
using StepLang.Parsing;
using StepLang.Parsing.Nodes;
using StepLang.Parsing.Nodes.Expressions;
using StepLang.Parsing.Nodes.Statements;

namespace StepLang.Interpreting;

public partial class Interpreter : IRootNodeVisitor, IStatementVisitor, IExpressionEvaluator
{
	public TextWriter? StdOut { get; }
	public TextWriter? StdErr { get; }
	public TextReader? StdIn { get; }
	public TextWriter? DebugOut { get; }
	public int ExitCode { get; set; }

	private readonly Stack<Scope> scopes = new();

	private Lazy<Random> random = new(() => new Random());

	public void SetRandomSeed(int value)
	{
		random = new Lazy<Random>(() => new Random(value));
	}

	public Random Random => random.Value;

	public Interpreter(TextWriter? stdOut = null, TextWriter? stdErr = null, TextReader? stdIn = null,
		TextWriter? debugOut = null)
	{
		StdOut = stdOut;
		StdErr = stdErr;
		StdIn = stdIn;
		DebugOut = debugOut;

		_ = PushScope(Scope.GlobalScope);
	}

	public Scope CurrentScope => scopes.Peek();

	public Scope PushScope(Scope? parent = null)
	{
		var newScope = new Scope(parent ?? CurrentScope);

		scopes.Push(newScope);

		DebugOut?.WriteLine($"Pushed new scope (new depth: {scopes.Count - 1})");

		return newScope;
	}

	public Scope PopScope()
	{
		DebugOut?.WriteLine($"Popping scope (new depth: {scopes.Count - 2})");

		return scopes.Pop();
	}

	public void Execute(IEnumerable<StatementNode> statements)
	{
		foreach (var statement in statements)
		{
			Execute(statement);

			if (CurrentScope.TryGetResult(out _, out _))
			{
				DebugOut?.WriteLine("Result found, continuing");

				return;
			}

			if (CurrentScope.ShouldBreak())
			{
				DebugOut?.WriteLine("Break found, breaking");

				return;
			}

			if (CurrentScope.ShouldContinue())
			{
				DebugOut?.WriteLine("Continue found, continuing");

				return;
			}
		}
	}

	public void Execute(StatementNode statement)
	{
		DebugOut?.WriteLine("Executing: " + statement);

		statement.Accept(this);
	}

	public void Visit(RootNode node)
	{
		foreach (var importNode in node.Imports)
		{
			importNode.Accept(this);
		}

		Execute(node.Body);
	}

	public void Visit(CodeBlockStatementNode statementNode)
	{
		_ = PushScope();

		Execute(statementNode.Statements);

		var resultScope = PopScope();

		if (resultScope.TryGetResult(out var resultValue, out var resultLocation))
		{
			CurrentScope.SetResult(resultLocation, resultValue);
		}
		else if (resultScope.ShouldBreak())
		{
			CurrentScope.SetBreak();
		}
		else if (resultScope.ShouldContinue())
		{
			CurrentScope.SetContinue();
		}
	}

	public void Visit(ErrorStatementNode errorStatementNode)
	{
		throw new NotSupportedException("Error statement nodes cannot be interpreted");
	}

	public ExpressionResult Evaluate(IdentifierExpressionNode expressionNode)
	{
		var variable = CurrentScope.GetVariable(expressionNode.Identifier);

		return variable.Value;
	}

	public ExpressionResult Evaluate(ErrorExpressionNode expressionNode)
	{
		throw new NotSupportedException("Error expression nodes cannot be interpreted");
	}
}
