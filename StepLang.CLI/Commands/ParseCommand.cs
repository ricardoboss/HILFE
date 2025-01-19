using Spectre.Console;
using Spectre.Console.Cli;
using StepLang.Diagnostics;
using StepLang.Expressions.Results;
using StepLang.Interpreting;
using StepLang.Parsing;
using StepLang.Parsing.Nodes;
using StepLang.Parsing.Nodes.Expressions;
using StepLang.Parsing.Nodes.Import;
using StepLang.Parsing.Nodes.Statements;
using StepLang.Parsing.Nodes.VariableDeclarations;
using StepLang.Tokenizing;
using StepLang.Tooling.CLI;
using StepLang.Tooling.CLI.Widgets;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.CLI.Commands;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class ParseCommand : AsyncCommand<ParseCommand.Settings>
{
	public sealed class Settings : HiddenGlobalCommandSettings
	{
		[CommandArgument(0, "<file>")]
		[Description("The path to a .step-file to run.")]
		public string File { get; init; } = null!;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		var scriptFile = new FileInfo(settings.File);
		var source = await CharacterSource.FromFileAsync(scriptFile);
		var diagnostics = new DiagnosticCollection();
		diagnostics.CollectionChanged += (_, e) =>
		{
			if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null })
			{
				ReportDiagnostics(e.NewItems);
			}
		};

		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize();
		var parser = new Parser(tokens, diagnostics);
		var root = parser.ParseRoot();

		var tree = new Tree(scriptFile.Name);
		var treeBuilder = new RootTreeBuilder(tree);

		root.Accept(treeBuilder);

		AnsiConsole.Write(tree);

		return 0;
	}

	private static void ReportDiagnostics(IReadOnlyList<Diagnostic> eNewItems)
	{
		foreach (var diagnostic in eNewItems)
		{
			var line = new DiagnosticLine(diagnostic);

			AnsiConsole.Write(line);
		}
	}

	private sealed class RootTreeBuilder(IHasTreeNodes root) : IRootNodeVisitor
	{
		public void Visit(RootNode node)
		{
			IHasTreeNodes statementsRoot;
			if (node.Imports.Count > 0)
			{
				var importsNode = root.AddNode("Imports:");
				var importTreeBuilder = new ImportTreeBuilder(importsNode);
				foreach (var import in node.Imports)
				{
					import.Accept(importTreeBuilder);
				}

				statementsRoot = root.AddNode("Statements:");
			}
			else
			{
				statementsRoot = root;
			}

			var statementTreeBuilder = new StatementTreeBuilder(statementsRoot);
			foreach (var statement in node.Body)
			{
				statement.Accept(statementTreeBuilder);
			}
		}
	}

	private sealed class ImportTreeBuilder(IHasTreeNodes parent) : IImportNodeVisitor
	{
		public void Visit(ImportNode importNode)
		{
			_ = parent.AddNode("Import: " + importNode.PathToken.StringValue);
		}

		public void Visit(ErrorImportNode importNode)
		{
			_ = parent.AddNode("ErrorImport: " + importNode.Description);
		}
	}

	private sealed class StatementTreeBuilder(IHasTreeNodes root) : IStatementVisitor
	{
		public void Visit(CallStatementNode statementNode)
		{
			var node = root.AddNode("CallStatement:");
			var evaluator = new ExpressionTreeBuilder(node);
			var expression = statementNode.CallExpression;
			_ = expression.EvaluateUsing(evaluator);
		}

		public void Visit(CodeBlockStatementNode statementNode)
		{
			var node = root.AddNode("CodeBlockStatement:");
			var statementTreeBuilder = new StatementTreeBuilder(node);
			foreach (var statement in statementNode.Statements)
			{
				statement.Accept(statementTreeBuilder);
			}
		}

		public void Visit(ContinueStatementNode statementNode)
		{
			root.AddNode("ContinueStatement");
		}

		public void Visit(ForeachDeclareKeyDeclareValueStatementNode statementNode)
		{
			var node = root.AddNode("ForeachStatement:");

			var keyDeclarationNode = node.AddNode("KeyDeclaration:");
			var keyExpressionBuilder = new VariableDeclarationTreeBuilder(keyDeclarationNode);
			_ = statementNode.KeyDeclaration.EvaluateUsing(keyExpressionBuilder);

			var valueDeclarationNode = node.AddNode("ValueDeclaration:");
			var valueExpressionBuilder = new VariableDeclarationTreeBuilder(valueDeclarationNode);
			_ = statementNode.ValueDeclaration.EvaluateUsing(valueExpressionBuilder);

			var expressionNode = node.AddNode("Expression:");
			var expressionBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Collection.EvaluateUsing(expressionBuilder);

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(BreakStatementNode statementNode)
		{
			root.AddNode("BreakStatement");
		}

		public void Visit(ForeachDeclareKeyValueStatementNode statementNode)
		{
			var node = root.AddNode("ForeachStatement:");

			var keyDeclarationNode = node.AddNode("KeyDeclaration:");
			var keyExpressionBuilder = new VariableDeclarationTreeBuilder(keyDeclarationNode);
			_ = statementNode.KeyDeclaration.EvaluateUsing(keyExpressionBuilder);

			node.AddNode("ValueIdentifier: " + statementNode.ValueIdentifier.ToString().EscapeMarkup());

			var expressionNode = node.AddNode("Expression:");
			var expressionBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Collection.EvaluateUsing(expressionBuilder);

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(ForeachDeclareValueStatementNode statementNode)
		{
			var node = root.AddNode("ForeachStatement:");

			var valueDeclarationNode = node.AddNode("ValueDeclaration:");
			var valueExpressionBuilder = new VariableDeclarationTreeBuilder(valueDeclarationNode);
			_ = statementNode.ValueDeclaration.EvaluateUsing(valueExpressionBuilder);

			var expressionNode = node.AddNode("Expression:");
			var expressionBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Collection.EvaluateUsing(expressionBuilder);

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(ForeachKeyValueStatementNode statementNode)
		{
			var node = root.AddNode("ForeachStatement:");

			node.AddNode("KeyIdentifier: " + statementNode.KeyIdentifier.ToString().EscapeMarkup());
			node.AddNode("ValueIdentifier: " + statementNode.ValueIdentifier.ToString().EscapeMarkup());

			var expressionNode = node.AddNode("Expression:");
			var expressionBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Collection.EvaluateUsing(expressionBuilder);

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(ForeachKeyDeclareValueStatementNode statementNode)
		{
			var node = root.AddNode("ForeachStatement:");

			node.AddNode("KeyIdentifier: " + statementNode.KeyIdentifier.ToString().EscapeMarkup());

			var valueDeclarationNode = node.AddNode("ValueDeclaration:");
			var valueExpressionBuilder = new VariableDeclarationTreeBuilder(valueDeclarationNode);
			_ = statementNode.ValueDeclaration.EvaluateUsing(valueExpressionBuilder);

			var expressionNode = node.AddNode("Expression:");
			var expressionBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Collection.EvaluateUsing(expressionBuilder);

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(ForeachValueStatementNode statementNode)
		{
			var node = root.AddNode("ForeachStatement:");

			node.AddNode("ValueIdentifier: " + statementNode.Identifier.ToString().EscapeMarkup());

			var expressionNode = node.AddNode("Expression:");
			var expressionBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Collection.EvaluateUsing(expressionBuilder);

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(IdentifierIndexAssignmentNode statementNode)
		{
			var node = root.AddNode("IdentifierIndexAssignment:");
			node.AddNode("Identifier: " + statementNode.Identifier.ToString().EscapeMarkup());

			var indexNode = node.AddNode("Index:");
			var indexTreeBuilder = new ExpressionTreeBuilder(indexNode);

			// TODO: add support for multiple index expressions
			_ = statementNode.IndexExpressions[0].EvaluateUsing(indexTreeBuilder);

			var expressionNode = node.AddNode("Expression:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.ValueExpression.EvaluateUsing(expressionTreeBuilder);
		}

		public void Visit(IfStatementNode statementNode)
		{
			var node = root.AddNode("IfStatement:");

			foreach (var (condition, statements) in statementNode.ConditionBodyMap)
			{
				var branchNode = node.AddNode("Branch:");

				var conditionNode = branchNode.AddNode("Condition:");
				var expressionTreeBuilder = new ExpressionTreeBuilder(conditionNode);
				_ = condition.EvaluateUsing(expressionTreeBuilder);

				var body = branchNode.AddNode("Body:");
				var statementTreeBuilder = new StatementTreeBuilder(body);
				statements.Accept(statementTreeBuilder);
			}
		}

		public void Visit(ReturnExpressionStatementNode statementNode)
		{
			var node = root.AddNode("ReturnExpressionStatement:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(node);
			_ = statementNode.Expression.EvaluateUsing(expressionTreeBuilder);
		}

		public void Visit(VariableAssignmentNode statementNode)
		{
			var node = root.AddNode("VariableAssignment:");
			node.AddNode("Identifier: " + statementNode.Identifier.ToString().EscapeMarkup());

			var expressionNode = node.AddNode("Expression:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = statementNode.Expression.EvaluateUsing(expressionTreeBuilder);
		}

		public void Visit(WhileStatementNode statementNode)
		{
			var node = root.AddNode("WhileStatement:");

			var conditionNode = node.AddNode("Condition:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(conditionNode);
			_ = statementNode.Condition.EvaluateUsing(expressionTreeBuilder);

			var body = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(body);
			statementNode.Body.Accept(statementTreeBuilder);
		}

		public void Visit(IncrementStatementNode statementNode)
		{
			var node = root.AddNode("IncrementStatement:");

			_ = node.AddNode("Operator: ++");
			_ = node.AddNode("Identifier: " + statementNode.Identifier.ToString().EscapeMarkup());
		}

		public void Visit(DecrementStatementNode statementNode)
		{
			var node = root.AddNode("DecrementStatement:");

			_ = node.AddNode("Operator: --");
			_ = node.AddNode("Identifier: " + statementNode.Identifier.ToString().EscapeMarkup());
		}

		public void Visit(VariableDeclarationStatementNode statementNode)
		{
			var variableDeclarationTreeBuilder = new VariableDeclarationTreeBuilder(root);
			_ = statementNode.Declaration.EvaluateUsing(variableDeclarationTreeBuilder);
		}

		public void Visit(ReturnStatementNode statementNode)
		{
			root.AddNode("ReturnStatement");
		}

		public void Visit(DiscardStatementNode discardStatementNode)
		{
			var node = root.AddNode("DiscardStatement");

			var expressionNode = node.AddNode("Expression:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = discardStatementNode.Expression.EvaluateUsing(expressionTreeBuilder);
		}

		public void Visit(ErrorStatementNode errorStatementNode)
		{
			var node = root.AddNode("ErrorStatement");

			_ = node.AddNode("Description: " + errorStatementNode.Description.EscapeMarkup());
			var tokensNode = node.AddNode("Tokens:");

			foreach (var token in errorStatementNode.Tokens.OfType<Token>())
			{
				tokensNode.AddNode(token.ToString().EscapeMarkup());
			}
		}
	}

	private sealed class VariableDeclarationTreeBuilder(IHasTreeNodes root) : IVariableDeclarationEvaluator
	{
		public Variable Evaluate(VariableDeclarationNode variableDeclarationNode)
		{
			var node = root.AddNode("VariableDeclaration:");
			node.AddNode("Types: " + string.Join("|", variableDeclarationNode.Types).EscapeMarkup());
			node.AddNode("Identifier: " + variableDeclarationNode.Identifier.ToString().EscapeMarkup());

			return DummyVariable;
		}

		public Variable Evaluate(NullableVariableDeclarationNode variableDeclarationNode)
		{
			var node = root.AddNode("NullableVariableDeclaration:");
			node.AddNode("Types: " + string.Join("|", variableDeclarationNode.Types).EscapeMarkup());
			node.AddNode("Nullability: " + variableDeclarationNode.NullabilityIndicator.ToString().EscapeMarkup());
			node.AddNode("Identifier: " + variableDeclarationNode.Identifier.ToString().EscapeMarkup());

			return DummyVariable;
		}

		public Variable Evaluate(VariableInitializationNode variableDeclarationNode)
		{
			var node = root.AddNode("VariableInitialization:");
			node.AddNode("Types: " + string.Join("|", variableDeclarationNode.Types).EscapeMarkup());
			node.AddNode("Identifier: " + variableDeclarationNode.Identifier.ToString().EscapeMarkup());

			var expressionNode = node.AddNode("Expression:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = variableDeclarationNode.Expression.EvaluateUsing(expressionTreeBuilder);

			return DummyVariable;
		}

		public Variable Evaluate(NullableVariableInitializationNode variableDeclarationNode)
		{
			var node = root.AddNode("NullableVariableInitialization:");
			node.AddNode("Types: " + string.Join("|", variableDeclarationNode.Types).EscapeMarkup());
			node.AddNode("Nullability: " + variableDeclarationNode.NullabilityIndicator.ToString().EscapeMarkup());
			node.AddNode("Identifier: " + variableDeclarationNode.Identifier.ToString().EscapeMarkup());

			var expressionNode = node.AddNode("Expression:");
			var expressionTreeBuilder = new ExpressionTreeBuilder(expressionNode);
			_ = variableDeclarationNode.Expression.EvaluateUsing(expressionTreeBuilder);

			return DummyVariable;
		}

		public Variable Evaluate(ErrorVariableDeclarationNode variableDeclarationNode)
		{
			var node = root.AddNode("ErrorVariableDeclaration:");
			node.AddNode("Description: " + variableDeclarationNode.Description.EscapeMarkup());

			return DummyVariable;
		}

		private static readonly Variable DummyVariable = new("dummy", Array.Empty<ResultType>(), false);
	}

	private sealed class ExpressionTreeBuilder(IHasTreeNodes root) : IExpressionEvaluator
	{
		public ExpressionResult Evaluate(CallExpressionNode expressionNode)
		{
			var node = root.AddNode("CallExpression:");
			node.AddNode("Identifier: " + expressionNode.Identifier.ToString().EscapeMarkup());
			if (expressionNode.Arguments.Count > 0)
			{
				var argumentsNode = node.AddNode("Arguments:");
				var evaluator = new ExpressionTreeBuilder(argumentsNode);
				foreach (var argument in expressionNode.Arguments)
				{
					_ = argument.EvaluateUsing(evaluator);
				}
			}
			else
			{
				node.AddNode("No arguments");
			}

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(FunctionDefinitionExpressionNode expressionNode)
		{
			var node = root.AddNode("FunctionDefinitionExpression:");

			var argumentsNode = node.AddNode("Parameters:");
			var variableDeclarationTreeBuilder = new VariableDeclarationTreeBuilder(argumentsNode);
			foreach (var argument in expressionNode.Parameters)
			{
				_ = argument.EvaluateUsing(variableDeclarationTreeBuilder);
			}

			var bodyNode = node.AddNode("Body:");
			var statementTreeBuilder = new StatementTreeBuilder(bodyNode);
			expressionNode.Body.Accept(statementTreeBuilder);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(IdentifierExpressionNode expressionNode)
		{
			root.AddNode("IdentifierExpression: " + expressionNode.Identifier.ToString().EscapeMarkup());

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(ListExpressionNode expressionNode)
		{
			var node = root.AddNode("ListExpression:");

			var entriesNode = node.AddNode("Entries:");
			var entryTreeBuilder = new ExpressionTreeBuilder(entriesNode);
			foreach (var entry in expressionNode.Expressions)
			{
				_ = entry.EvaluateUsing(entryTreeBuilder);
			}

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(LiteralExpressionNode expressionNode)
		{
			root.AddNode("LiteralExpression: " + expressionNode.Literal.ToString().EscapeMarkup());

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(MapExpressionNode expressionNode)
		{
			var node = root.AddNode("MapExpression:");

			var entriesNode = node.AddNode("Entries:");
			foreach (var kvp in expressionNode.Expressions)
			{
				var entryNode = entriesNode.AddNode(kvp.Key.ToString().EscapeMarkup());
				var entriesTreeBuilder = new ExpressionTreeBuilder(entryNode);
				_ = kvp.Value.EvaluateUsing(entriesTreeBuilder);
			}

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(AddExpressionNode expressionNode)
		{
			var node = root.AddNode("AddExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(CoalesceExpressionNode expressionNode)
		{
			var node = root.AddNode("CoalesceExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(NotEqualsExpressionNode expressionNode)
		{
			var node = root.AddNode("NotEqualsExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(EqualsExpressionNode expressionNode)
		{
			var node = root.AddNode("EqualsExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(NegateExpressionNode expressionNode)
		{
			var node = root.AddNode("NegateExpression:");

			_ = node.AddNode("Operator: -");

			var expressionNodeTreeBuilder = new ExpressionTreeBuilder(node);
			_ = expressionNode.Expression.EvaluateUsing(expressionNodeTreeBuilder);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(SubtractExpressionNode expressionNode)
		{
			var node = root.AddNode("SubtractExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(MultiplyExpressionNode expressionNode)
		{
			var node = root.AddNode("MultiplyExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(DivideExpressionNode expressionNode)
		{
			var node = root.AddNode("DivideExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(ModuloExpressionNode expressionNode)
		{
			var node = root.AddNode("ModuloExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(PowerExpressionNode expressionNode)
		{
			var node = root.AddNode("PowerExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(GreaterThanExpressionNode expressionNode)
		{
			var node = root.AddNode("GreaterThanExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(LessThanExpressionNode expressionNode)
		{
			var node = root.AddNode("LessThanExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(GreaterThanOrEqualExpressionNode expressionNode)
		{
			var node = root.AddNode("GreaterThanOrEqualExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(LessThanOrEqualExpressionNode expressionNode)
		{
			var node = root.AddNode("LessThanOrEqualExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(LogicalAndExpressionNode expressionNode)
		{
			var node = root.AddNode("LogicalAndExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(LogicalOrExpressionNode expressionNode)
		{
			var node = root.AddNode("LogicalOrExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseXorExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseXorExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseAndExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseAndExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseOrExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseOrExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseShiftLeftExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseShiftLeftExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseShiftRightExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseShiftRightExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseRotateLeftExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseRotateLeftExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(BitwiseRotateRightExpressionNode expressionNode)
		{
			var node = root.AddNode("BitwiseRotateRightExpression:");

			EvaluateBinary(node, expressionNode);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(NotExpressionNode expressionNode)
		{
			var node = root.AddNode("NotExpression:");

			_ = node.AddNode("Operator: !");

			var expressionNodeTreeBuilder = new ExpressionTreeBuilder(node);
			_ = expressionNode.Expression.EvaluateUsing(expressionNodeTreeBuilder);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(FunctionDefinitionCallExpressionNode expressionNode)
		{
			var node = root.AddNode("FunctionDefinitionCallExpression:");

			var definitionNode = node.AddNode("Definition:");
			var definitionParameters = definitionNode.AddNode("Parameters:");
			var definitionParametersTreeBuilder = new VariableDeclarationTreeBuilder(definitionParameters);
			foreach (var parameter in expressionNode.Parameters)
			{
				_ = parameter.EvaluateUsing(definitionParametersTreeBuilder);
			}

			var definitionBody = definitionNode.AddNode("Body:");
			var definitionBodyTreeBuilder = new StatementTreeBuilder(definitionBody);
			expressionNode.Body.Accept(definitionBodyTreeBuilder);

			var argumentsNode = node.AddNode("Arguments:");
			var argumentsTreeBuilder = new ExpressionTreeBuilder(argumentsNode);
			foreach (var argument in expressionNode.CallArguments)
			{
				_ = argument.EvaluateUsing(argumentsTreeBuilder);
			}

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(NativeFunctionDefinitionExpressionNode expressionNode)
		{
			var node = root.AddNode("NativeFunctionDefinitionExpression:");

			var argumentsNode = node.AddNode("Parameters:");
			var variableDeclarationTreeBuilder = new VariableDeclarationTreeBuilder(argumentsNode);
			foreach (var argument in expressionNode.Definition.Parameters)
			{
				_ = argument.EvaluateUsing(variableDeclarationTreeBuilder);
			}

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(IndexAccessExpressionNode expressionNode)
		{
			var node = root.AddNode("IndexAccessExpression:");

			var expression = node.AddNode("Expression:");
			var expressionNodeTreeBuilder = new ExpressionTreeBuilder(expression);
			_ = expressionNode.Left.EvaluateUsing(expressionNodeTreeBuilder);

			var index = node.AddNode("Index:");
			var indexNodeTreeBuilder = new ExpressionTreeBuilder(index);
			_ = expressionNode.Index.EvaluateUsing(indexNodeTreeBuilder);

			return VoidResult.Instance;
		}

		public ExpressionResult Evaluate(ErrorExpressionNode expressionNode)
		{
			var node = root.AddNode("ErrorExpression");

			_ = node.AddNode("Description: " + expressionNode.Description.EscapeMarkup());
			var tokensNode = node.AddNode("Tokens:");

			foreach (var token in expressionNode.Tokens.OfType<Token>())
			{
				tokensNode.AddNode(token.ToString().EscapeMarkup());
			}

			return VoidResult.Instance;
		}

		private static void EvaluateBinary(IHasTreeNodes parent, IBinaryExpressionNode node)
		{
			_ = parent.AddNode("Operator: " + node.Op.ToSymbol().EscapeMarkup());

			var leftNode = parent.AddNode("Left:");
			var leftExpressionTreeBuilder = new ExpressionTreeBuilder(leftNode);
			_ = node.Left.EvaluateUsing(leftExpressionTreeBuilder);

			var rightNode = parent.AddNode("Right:");
			var rightExpressionTreeBuilder = new ExpressionTreeBuilder(rightNode);
			_ = node.Right.EvaluateUsing(rightExpressionTreeBuilder);
		}
	}
}
