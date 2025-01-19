using StepLang.Diagnostics;
using StepLang.Parsing.Nodes;
using StepLang.Parsing.Nodes.Expressions;
using StepLang.Parsing.Nodes.Import;
using StepLang.Parsing.Nodes.Statements;
using StepLang.Parsing.Nodes.VariableDeclarations;
using StepLang.Tokenizing;
using StepLang.Utils;
using System.Diagnostics;

namespace StepLang.Parsing;

public class Parser(IEnumerable<Token> tokenList, DiagnosticCollection diagnostics)
{
	private readonly TokenQueue tokens = new(tokenList) { IgnoreMeaningless = true };

	public RootNode ParseRoot()
	{
		var imports = ParseImports();
		var statements = ParseStatements(TokenType.EndOfFile);

		return new RootNode(imports, statements);
	}

	private List<IImportNode> ParseImports()
	{
		var imports = new List<IImportNode>();
		while (tokens.PeekType() is TokenType.ImportKeyword)
		{
			imports.Add(ParseImport());
		}

		return imports;
	}

	private IImportNode ParseImport()
	{
		_ = tokens.Dequeue(TokenType.ImportKeyword);
		var pathResult = tokens.Dequeue(TokenType.LiteralString);
		switch (pathResult)
		{
			case Ok<Token> { Value: var path }:
				if (tokens.PeekType() is TokenType.NewLine)
				{
					_ = tokens.Dequeue(TokenType.NewLine);
				}

				return new ImportNode(path);
			case Err<Token> err:
				return diagnostics.AddImportError(err);
			default:
				throw new InvalidOperationException("Unexpected result type");
		}
	}

	private List<StatementNode> ParseStatements(TokenType stopTokenType)
	{
		var statements = new List<StatementNode>();
		TokenType? nextType;
		while ((nextType = tokens.PeekType()) != null && nextType != stopTokenType)
		{
			if (nextType is TokenType.NewLine)
			{
				_ = tokens.Dequeue(TokenType.NewLine);

				continue;
			}

			statements.Add(ParseStatement());
		}

		return statements;
	}

	private StatementNode ParseStatement()
	{
		var token = tokens.Peek();
		switch (token?.Type)
		{
			case TokenType.TypeName:
				{
					var declaration = ParseVariableDeclaration();

					return new VariableDeclarationStatementNode(declaration);
				}
			case TokenType.Identifier:
				return ParseIdentifierUsage();
			case TokenType.UnderscoreSymbol:
				return ParseDiscardStatement();
			case TokenType.IfKeyword:
				return ParseIfStatement();
			case TokenType.WhileKeyword:
				return ParseWhileStatement();
			case TokenType.ForEachKeyword:
				return ParseForeachStatement();
			case TokenType.ReturnKeyword:
				return ParseReturnStatement();
			case TokenType.BreakKeyword:
				return ParseBreakStatement();
			case TokenType.ContinueKeyword:
				return ParseContinueStatement();
			case TokenType.OpeningCurlyBracket:
				return ParseCodeBlock();
			case null:
				{
					_ = tokens.Dequeue();

					return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
				}
			default:
				{
					_ = tokens.Dequeue();

					return diagnostics.AddUnexpectedToken(tokens.LastToken!, TokenType.TypeName, TokenType.Identifier,
						TokenType.NewLine, TokenType.LineComment);
				}
		}
	}

	public StatementNode ParseIdentifierUsage()
	{
		var next = tokens.Peek(1);
		if (next is null)
		{
			_ = tokens.Dequeue(TokenType.Identifier);

			return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
		}

		switch (next.Type)
		{
			case TokenType.EqualsSymbol:
				return ParseVariableAssignment();
			case TokenType.OpeningParentheses:
				return ParseFunctionCall();
			case TokenType.OpeningSquareBracket:
				return ParseIndexAssignment();
		}

		var nextNext = tokens.Peek(2);
		if (nextNext is null)
		{
			_ = tokens.Dequeue(TokenType.Identifier);
			_ = tokens.Dequeue();

			return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
		}

		if (next.Type.IsShorthandMathematicalOperation() && nextNext.Type == next.Type)
		{
			return ParseShorthandMathOperation();
		}

		if (next.Type.IsShorthandMathematicalOperationWithAssignment())
		{
			return ParseShorthandMathOperationExpression();
		}

		_ = tokens.Dequeue(2);

		return diagnostics.AddUnexpectedToken(tokens.LastToken!, next.Type, TokenType.EqualsSymbol);
	}

	private StatementNode ParseDiscardStatement()
	{
		var result = tokens.Dequeue(TokenType.UnderscoreSymbol);
		if (result is Err<Token> error)
			return diagnostics.AddError(error);

		var underscore = result.Value;

		_ = tokens.Dequeue(TokenType.EqualsSymbol);

		var expression = ParseExpression();

		return new DiscardStatementNode(underscore.Location, expression);
	}

	private StatementNode ParseIndexAssignment()
	{
		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddError(identifierError);

		var identifier = identifierResult.Value;

		_ = tokens.Dequeue(TokenType.OpeningSquareBracket);

		var initialIndex = ParseExpression();
		var indexExpressions = new List<ExpressionNode>([initialIndex]);

		_ = tokens.Dequeue(TokenType.ClosingSquareBracket);

		var postIndexTokenResult = tokens.Dequeue(TokenType.EqualsSymbol, TokenType.OpeningSquareBracket);
		if (postIndexTokenResult is Err<Token> postIndexError)
			return diagnostics.AddError(postIndexError);

		var postIndexToken = postIndexTokenResult.Value;

		while (postIndexToken.Type == TokenType.OpeningSquareBracket)
		{
			var indexExpression = ParseExpression();
			indexExpressions.Add(indexExpression);

			_ = tokens.Dequeue(TokenType.ClosingSquareBracket);

			postIndexTokenResult = tokens.Dequeue(TokenType.EqualsSymbol, TokenType.OpeningSquareBracket);
			if (postIndexTokenResult is Err<Token> nestedError)
				return diagnostics.AddError(nestedError);

			postIndexToken = postIndexTokenResult.Value;
		}

		Debug.Assert(postIndexToken.Type == TokenType.EqualsSymbol);

		var expression = ParseExpression();

		return new IdentifierIndexAssignmentNode(identifier, indexExpressions, postIndexToken, expression);
	}

	private StatementNode ParseShorthandMathOperation()
	{
		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddError(identifierError);

		var identifier = identifierResult.Value;

		var operationResult = tokens.Dequeue(TokenTypes.ShorthandMathematicalOperations);
		if (operationResult is Err<Token> operationError)
			return diagnostics.AddError(operationError);

		var operation = operationResult.Value;

		_ = tokens.Dequeue(operation.Type);

		switch (operation.Type)
		{
			case TokenType.PlusSymbol:
				return new IncrementStatementNode(identifier);
			case TokenType.MinusSymbol:
				return new DecrementStatementNode(identifier);
			default:
				return diagnostics.AddUnexpectedToken(operation, TokenType.PlusSymbol, TokenType.MinusSymbol);
		}
	}

	private StatementNode ParseShorthandMathOperationExpression()
	{
		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddError(identifierError);

		var identifier = identifierResult.Value;

		var identifierExpression = new IdentifierExpressionNode(identifier);

		List<Token> operatorTokens;
		try
		{
			operatorTokens = PeekContinuousOperators(TokenTypes.ShorthandMathematicalOperationsWithAssignment);
		}
		catch (UnexpectedTokenException e)
		{
			return diagnostics.AddUnexpectedToken(e.Message, e.Token);
		}

		Debug.Assert(operatorTokens.Count > 0);

		_ = tokens.Dequeue(operatorTokens.Count);
		var assignmentResult = tokens.Dequeue(TokenType.EqualsSymbol);
		if (assignmentResult is Err<Token> assignmentError)
			return diagnostics.AddError(assignmentError);

		var assignmentToken = assignmentResult.Value;

		var expression = ParseExpression();
		var firstOperator = operatorTokens[0];

		return firstOperator.Type switch
		{
			TokenType.PlusSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new AddExpressionNode(firstOperator.Location, identifierExpression, expression)
			),
			TokenType.MinusSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new SubtractExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			TokenType.AsteriskSymbol => operatorTokens.Count switch
			{
				1 => new VariableAssignmentNode(
					assignmentToken.Location,
					identifier,
					new MultiplyExpressionNode(
						firstOperator.Location,
						identifierExpression,
						expression
					)
				),
				2 => operatorTokens[1].Type switch
				{
					TokenType.AsteriskSymbol => new VariableAssignmentNode(
						assignmentToken.Location,
						identifier,
						new PowerExpressionNode(
							firstOperator.Location,
							identifierExpression,
							expression
						)
					),
					_ => diagnostics.AddUnexpectedToken(operatorTokens[1], TokenType.AsteriskSymbol),
				},
				_ => diagnostics.AddUnexpectedEndOfTokens(firstOperator, "Expected an operator"),
			},
			TokenType.SlashSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new DivideExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			TokenType.PercentSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new ModuloExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			TokenType.PipeSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new BitwiseOrExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			TokenType.AmpersandSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new BitwiseAndExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			TokenType.HatSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new BitwiseXorExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			TokenType.QuestionMarkSymbol => new VariableAssignmentNode(
				assignmentToken.Location,
				identifier,
				new CoalesceExpressionNode(
					firstOperator.Location,
					identifierExpression,
					expression
				)
			),
			_ => diagnostics.AddUnexpectedToken(firstOperator, TokenType.PlusSymbol, TokenType.MinusSymbol,
				TokenType.AsteriskSymbol, TokenType.SlashSymbol, TokenType.PercentSymbol, TokenType.PipeSymbol,
				TokenType.AmpersandSymbol, TokenType.HatSymbol, TokenType.QuestionMarkSymbol),
		};
	}

	private StatementNode ParseForeachStatement()
	{
		var foreachKeywordResult = tokens.Dequeue(TokenType.ForEachKeyword);
		if (foreachKeywordResult is Err<Token> foreachError)
			return diagnostics.AddError(foreachError);

		var foreachKeyword = foreachKeywordResult.Value;

		_ = tokens.Dequeue(TokenType.OpeningParentheses);

		IVariableDeclarationNode? keyDeclaration = null;
		Token? keyIdentifier = null;
		IVariableDeclarationNode? valueDeclaration = null;
		Token? valueIdentifier = null;

		var next = tokens.Peek();
		switch (next?.Type)
		{
			case TokenType.TypeName:
				{
					var firstDeclaration = ParseVariableDeclaration();

					next = tokens.Peek();
					switch (next?.Type)
					{
						case TokenType.ColonSymbol:
							{
								keyDeclaration = firstDeclaration;

								_ = tokens.Dequeue(TokenType.ColonSymbol);

								next = tokens.Peek();
								switch (next?.Type)
								{
									case TokenType.TypeName:
										valueDeclaration = ParseVariableDeclaration();
										break;
									case TokenType.Identifier:
										var valueIdentifierResult = tokens.Dequeue(TokenType.Identifier);
										if (valueIdentifierResult is Err<Token> valueIdentifierError)
											return diagnostics.AddError(valueIdentifierError);

										valueIdentifier = valueIdentifierResult.Value;

										break;
									case null:
										return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
									default:
										_ = tokens.Dequeue(next.Type);

										return diagnostics.AddUnexpectedToken(next, TokenType.TypeName,
											TokenType.Identifier);
								}

								break;
							}
						case TokenType.InKeyword:
							valueDeclaration = firstDeclaration;
							break;
						case null:
							return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
						default:
							_ = tokens.Dequeue(next.Type);

							return diagnostics.AddUnexpectedToken(next, TokenType.ColonSymbol, TokenType.InKeyword);
					}

					break;
				}
			case TokenType.Identifier:
				{
					var firstIdentifierResult = tokens.Dequeue(TokenType.Identifier);
					if (firstIdentifierResult is Err<Token> firstIdentifierError)
						return diagnostics.AddError(firstIdentifierError);

					var firstIdentifier = firstIdentifierResult.Value;

					next = tokens.Peek();
					switch (next?.Type)
					{
						case TokenType.ColonSymbol:
							{
								keyIdentifier = firstIdentifier;

								_ = tokens.Dequeue(TokenType.ColonSymbol);

								next = tokens.Peek();
								switch (next?.Type)
								{
									case TokenType.TypeName:
										valueDeclaration = ParseVariableDeclaration();
										break;
									case TokenType.Identifier:
										var valueIdentifierResult = tokens.Dequeue(TokenType.Identifier);
										if (valueIdentifierResult is Err<Token> valueIdentifierError)
											return diagnostics.AddError(valueIdentifierError);

										valueIdentifier = valueIdentifierResult.Value;

										break;
									case null:
										return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
									default:
										_ = tokens.Dequeue(next.Type);

										return diagnostics.AddUnexpectedToken(next, TokenType.TypeName,
											TokenType.Identifier);
								}

								break;
							}
						case TokenType.InKeyword:
							valueIdentifier = firstIdentifier;
							break;
						case null:
							return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
						default:
							_ = tokens.Dequeue(next.Type);

							return diagnostics.AddUnexpectedToken(next, TokenType.ColonSymbol, TokenType.InKeyword);
					}

					break;
				}
			case null:
				return diagnostics.AddUnexpectedEndOfTokens(tokens.LastToken);
			default:
				return diagnostics.AddUnexpectedToken(next, TokenType.TypeName, TokenType.Identifier);
		}

		if (valueDeclaration is null && valueIdentifier is null)
		{
			_ = tokens.Dequeue();

			return diagnostics.AddUnexpectedToken("Foreach without value declaration or identifier", tokens.LastToken!);
		}

		_ = tokens.Dequeue(TokenType.InKeyword);

		var list = ParseExpression();

		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		var body = ParseCodeBlock();

		if (keyDeclaration is not null)
		{
			if (valueDeclaration is not null)
			{
				return new ForeachDeclareKeyDeclareValueStatementNode(foreachKeyword, keyDeclaration, valueDeclaration,
					list, body);
			}

			return new ForeachDeclareKeyValueStatementNode(foreachKeyword, keyDeclaration, valueIdentifier!, list,
				body);
		}

		if (keyIdentifier is not null)
		{
			if (valueDeclaration is not null)
			{
				return new ForeachKeyDeclareValueStatementNode(foreachKeyword, keyIdentifier, valueDeclaration, list,
					body);
			}

			return new ForeachKeyValueStatementNode(foreachKeyword, keyIdentifier, valueIdentifier!, list, body);
		}

		if (valueDeclaration is not null)
		{
			return new ForeachDeclareValueStatementNode(foreachKeyword, valueDeclaration, list, body);
		}

		return new ForeachValueStatementNode(foreachKeyword, valueIdentifier!, list, body);
	}

	private StatementNode ParseCodeBlock()
	{
		var openCurlyBraceResult = tokens.Dequeue(TokenType.OpeningCurlyBracket);
		if (openCurlyBraceResult is Err<Token> openError)
			return diagnostics.AddError(openError);

		var openCurlyBrace = openCurlyBraceResult.Value;

		var statements = ParseStatements(TokenType.ClosingCurlyBracket);

		var closingCurlyBraceResult = tokens.Dequeue(TokenType.ClosingCurlyBracket);
		if (closingCurlyBraceResult is Err<Token> closingError)
			return diagnostics.AddError(closingError);

		var closingCurlyBrace = closingCurlyBraceResult.Value;

		return new CodeBlockStatementNode(openCurlyBrace, statements, closingCurlyBrace);
	}

	private StatementNode ParseContinueStatement()
	{
		var continueTokenResult = tokens.Dequeue(TokenType.ContinueKeyword);
		if (continueTokenResult is Err<Token> continueError)
			return diagnostics.AddError(continueError);

		return new ContinueStatementNode(continueTokenResult.Value);
	}

	private StatementNode ParseBreakStatement()
	{
		var breakTokenResult = tokens.Dequeue(TokenType.BreakKeyword);
		if (breakTokenResult is Err<Token> breakError)
			return diagnostics.AddError(breakError);

		return new BreakStatementNode(breakTokenResult.Value);
	}

	private StatementNode ParseReturnStatement()
	{
		var returnKeywordResult = tokens.Dequeue(TokenType.ReturnKeyword);
		if (returnKeywordResult is Err<Token> returnError)
			return diagnostics.AddError(returnError);

		var returnKeyword = returnKeywordResult.Value;

		if (tokens.PeekType() is TokenType.NewLine)
		{
			return new ReturnStatementNode(returnKeyword);
		}

		var expression = ParseExpression();

		return new ReturnExpressionStatementNode(returnKeyword, expression);
	}

	private StatementNode ParseWhileStatement()
	{
		var whileKeywordResult = tokens.Dequeue(TokenType.WhileKeyword);
		if (whileKeywordResult is Err<Token> whileError)
			return diagnostics.AddError(whileError);

		var whileKeyword = whileKeywordResult.Value;

		_ = tokens.Dequeue(TokenType.OpeningParentheses);

		var condition = ParseExpression();

		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		var codeBlock = ParseCodeBlock();

		return new WhileStatementNode(whileKeyword, condition, codeBlock);
	}

	private StatementNode ParseIfStatement()
	{
		var ifKeywordResult = tokens.Dequeue(TokenType.IfKeyword);
		if (ifKeywordResult is Err<Token> ifError)
			return diagnostics.AddError(ifError);

		var ifKeyword = ifKeywordResult.Value;

		_ = tokens.Dequeue(TokenType.OpeningParentheses);
		var condition = ParseExpression();
		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		var codeBlock = ParseCodeBlock();

		var conditionBodyMap = new LinkedList<(ExpressionNode, StatementNode)>();
		_ = conditionBodyMap.AddLast((condition, codeBlock));

		if (tokens.PeekType() is not TokenType.ElseKeyword)
		{
			return new IfStatementNode(ifKeyword, conditionBodyMap);
		}

		_ = tokens.Dequeue(TokenType.ElseKeyword);

		if (tokens.PeekType() is TokenType.IfKeyword)
		{
			var nested = ParseIfStatement();
			if (nested is ErrorStatementNode error)
				return error;

			if (nested is not IfStatementNode nestedIf)
				throw new InvalidOperationException("Unexpected statement type");

			foreach (var (nestedCondition, nestedCodeBlock) in nestedIf.ConditionBodyMap)
			{
				_ = conditionBodyMap.AddLast((nestedCondition, nestedCodeBlock));
			}

			return new IfStatementNode(ifKeyword, conditionBodyMap);
		}

		var elseCodeBlock = ParseCodeBlock();

		_ = conditionBodyMap.AddLast((LiteralExpressionNode.FromBoolean(true), elseCodeBlock));

		return new IfStatementNode(ifKeyword, conditionBodyMap);
	}

	private CallStatementNode ParseFunctionCall()
	{
		var callExpression = ParseFunctionCallExpression();

		return new CallStatementNode(callExpression);
	}

	private IVariableDeclarationNode ParseVariableDeclaration()
	{
		var typeResult = tokens.Dequeue(TokenType.TypeName);
		if (typeResult is Err<Token> typeError)
			return diagnostics.AddVariableDeclarationError(typeError);

		var type = typeResult.Value;

		Token? nullabilityIndicator = null;
		if (tokens.Peek() is { Type: TokenType.QuestionMarkSymbol })
		{
			var nullabilityIndicatorResult = tokens.Dequeue(TokenType.QuestionMarkSymbol);
			if (nullabilityIndicatorResult is Err<Token> nullabilityIndicatorError)
				return diagnostics.AddVariableDeclarationError(nullabilityIndicatorError);

			nullabilityIndicator = nullabilityIndicatorResult.Value;
		}

		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddVariableDeclarationError(identifierError);

		var identifier = identifierResult.Value;

		if (tokens.PeekType() is not TokenType.EqualsSymbol)
		{
			if (nullabilityIndicator is not null)
			{
				return new NullableVariableDeclarationNode([type], nullabilityIndicator, identifier);
			}

			return new VariableDeclarationNode([type], identifier);
		}

		var assignmentTokenResult = tokens.Dequeue(TokenType.EqualsSymbol);
		if (assignmentTokenResult is Err<Token> assignmentError)
			return diagnostics.AddVariableDeclarationError(assignmentError);

		var assignmentToken = assignmentTokenResult.Value;

		var expression = ParseExpression();

		if (nullabilityIndicator is not null)
		{
			return new NullableVariableInitializationNode(assignmentToken.Location, [type],
				nullabilityIndicator, identifier, expression);
		}

		return new VariableInitializationNode(assignmentToken.Location, [type], identifier, expression);
	}

	private StatementNode ParseVariableAssignment()
	{
		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddError(identifierError);

		var identifier = identifierResult.Value;

		var assignmentTokenResult = tokens.Dequeue(TokenType.EqualsSymbol);
		if (assignmentTokenResult is Err<Token> assignmentError)
			return diagnostics.AddError(assignmentError);

		var assignmentToken = assignmentTokenResult.Value;

		var expression = ParseExpression();

		return new VariableAssignmentNode(assignmentToken.Location, identifier, expression);
	}

	private ExpressionNode ParseExpression(int parentPrecedence = 0)
	{
		var left = ParsePrimaryExpression();

		while (tokens.PeekType() is TokenType.OpeningSquareBracket)
		{
			_ = tokens.Dequeue(TokenType.OpeningSquareBracket);

			var index = ParseExpression();

			_ = tokens.Dequeue(TokenType.ClosingSquareBracket);

			left = new IndexAccessExpressionNode(left, index);
		}

		while (tokens.PeekType() is { } nextType && nextType.IsOperator())
		{
			List<Token> operatorTokens;
			try
			{
				operatorTokens = PeekContinuousOperators(TokenTypes.Operators);
			}
			catch (UnexpectedTokenException e)
			{
				return diagnostics.AddUnexpectedTokenExpression(e.Message, e.Token);
			}

			var binaryOperatorResult = ParseExpressionOperator(operatorTokens);
			if (binaryOperatorResult is Err<BinaryExpressionOperator> binaryOperatorError)
				return diagnostics.AddErrorExpression(binaryOperatorError);

			var binaryOperator = binaryOperatorResult.Value;

			var precedence = binaryOperator.Precedence();
			if (precedence < parentPrecedence)
			{
				break;
			}

			// actually consume the operator
			_ = tokens.Dequeue(operatorTokens.Count);

			var right = ParseExpression(precedence + 1);

			left = Combine(operatorTokens.First().Location, left, binaryOperator, right);
		}

		return left;
	}

	private static ExpressionNode Combine(TokenLocation operatorLocation, ExpressionNode left,
		BinaryExpressionOperator binaryOperator, ExpressionNode right)
	{
		return binaryOperator switch
		{
			BinaryExpressionOperator.Add => new AddExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Coalesce => new CoalesceExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.NotEqual => new NotEqualsExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Equal => new EqualsExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Subtract => new SubtractExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Multiply => new MultiplyExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Divide => new DivideExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Modulo => new ModuloExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.Power => new PowerExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.GreaterThan => new GreaterThanExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.LessThan => new LessThanExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.GreaterThanOrEqual => new GreaterThanOrEqualExpressionNode(operatorLocation, left,
				right),
			BinaryExpressionOperator.LessThanOrEqual =>
				new LessThanOrEqualExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.LogicalAnd => new LogicalAndExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.LogicalOr => new LogicalOrExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.BitwiseAnd => new BitwiseAndExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.BitwiseOr => new BitwiseOrExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.BitwiseXor => new BitwiseXorExpressionNode(operatorLocation, left, right),
			BinaryExpressionOperator.BitwiseShiftLeft => new BitwiseShiftLeftExpressionNode(operatorLocation, left,
				right),
			BinaryExpressionOperator.BitwiseShiftRight => new BitwiseShiftRightExpressionNode(operatorLocation, left,
				right),
			BinaryExpressionOperator.BitwiseRotateLeft => new BitwiseRotateLeftExpressionNode(operatorLocation, left,
				right),
			BinaryExpressionOperator.BitwiseRotateRight => new BitwiseRotateRightExpressionNode(operatorLocation, left,
				right),
			_ => throw new NotSupportedException("Expression for operator " + binaryOperator.ToSymbol() +
												 " not supported"),
		};
	}

	private List<Token> PeekContinuousOperators(TokenType[] allowedOperators)
	{
		// whitespaces have meaning in operators
		var previousIgnoreMeaningless = tokens.IgnoreMeaningless;
		tokens.IgnoreMeaningless = false;

		try
		{
			var offset = 0;
			var operators = new List<Token>();
			while (tokens.Peek(offset) is { Type: not TokenType.EndOfFile } next)
			{
				if (allowedOperators.Contains(next.Type))
				{
					operators.Add(next);

					offset++;
				}
				else if (operators.Count == 0)
				{
					if (next.Type is not TokenType.Whitespace)
					{
						throw new UnexpectedTokenException(next, allowedOperators);
					}

					offset++; // skip leading whitespace
				}
				else
				{
					break;
				}
			}

			return operators;
		}
		finally
		{
			tokens.IgnoreMeaningless = previousIgnoreMeaningless;
		}
	}

	private Result<BinaryExpressionOperator> ParseExpressionOperator(List<Token> operatorTokens)
	{
		switch (operatorTokens.Count)
		{
			case 3:
				{
					var (first, second, third) = (operatorTokens[0], operatorTokens[1], operatorTokens[2]);

					return first.Type switch
					{
						TokenType.GreaterThanSymbol => second.Type switch
						{
							TokenType.GreaterThanSymbol => third.Type switch
							{
								TokenType.GreaterThanSymbol => BinaryExpressionOperator.BitwiseRotateRight.ToResult(),
								_ => new UnexpectedTokenException(third, TokenType.GreaterThanSymbol)
									.ToErr<BinaryExpressionOperator>(),
							},
							_ => new UnexpectedTokenException(second, TokenType.GreaterThanSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						TokenType.LessThanSymbol => second.Type switch
						{
							TokenType.LessThanSymbol => third.Type switch
							{
								TokenType.LessThanSymbol => BinaryExpressionOperator.BitwiseRotateLeft.ToResult(),
								_ => new UnexpectedTokenException(third, TokenType.LessThanSymbol)
									.ToErr<BinaryExpressionOperator>(),
							},
							_ => new UnexpectedTokenException(second, TokenType.LessThanSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						_ => new UnexpectedTokenException(first, TokenType.GreaterThanSymbol,
							TokenType.LessThanSymbol).ToErr<BinaryExpressionOperator>(),
					};
				}
			case 2:
				{
					var (first, second) = (operatorTokens[0], operatorTokens[1]);

					return first.Type switch
					{
						TokenType.AsteriskSymbol => second.Type switch
						{
							TokenType.AsteriskSymbol => BinaryExpressionOperator.Power.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.AsteriskSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						TokenType.EqualsSymbol => second.Type switch
						{
							TokenType.EqualsSymbol => BinaryExpressionOperator.Equal.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.EqualsSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						TokenType.ExclamationMarkSymbol => second.Type switch
						{
							TokenType.EqualsSymbol => BinaryExpressionOperator.NotEqual.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.EqualsSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						TokenType.AmpersandSymbol => second.Type switch
						{
							TokenType.AmpersandSymbol => BinaryExpressionOperator.LogicalAnd.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.AmpersandSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						TokenType.PipeSymbol => second.Type switch
						{
							TokenType.PipeSymbol => BinaryExpressionOperator.LogicalOr.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.PipeSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						TokenType.LessThanSymbol => second.Type switch
						{
							TokenType.EqualsSymbol => BinaryExpressionOperator.LessThanOrEqual.ToResult(),
							TokenType.LessThanSymbol => BinaryExpressionOperator.BitwiseShiftLeft.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.EqualsSymbol,
								TokenType.LessThanSymbol).ToErr<BinaryExpressionOperator>(),
						},
						TokenType.GreaterThanSymbol => second.Type switch
						{
							TokenType.EqualsSymbol => BinaryExpressionOperator.GreaterThanOrEqual.ToResult(),
							TokenType.GreaterThanSymbol => BinaryExpressionOperator.BitwiseShiftRight.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.EqualsSymbol,
								TokenType.GreaterThanSymbol).ToErr<BinaryExpressionOperator>(),
						},
						TokenType.QuestionMarkSymbol => second.Type switch
						{
							TokenType.QuestionMarkSymbol => BinaryExpressionOperator.Coalesce.ToResult(),
							_ => new UnexpectedTokenException(second, TokenType.QuestionMarkSymbol)
								.ToErr<BinaryExpressionOperator>(),
						},
						_ => new UnexpectedTokenException(first, TokenType.AsteriskSymbol, TokenType.EqualsSymbol,
								TokenType.ExclamationMarkSymbol, TokenType.AmpersandSymbol, TokenType.PipeSymbol,
								TokenType.LessThanSymbol, TokenType.GreaterThanSymbol, TokenType.QuestionMarkSymbol)
							.ToErr<BinaryExpressionOperator>(),
					};
				}
			case 1:
				{
					var first = operatorTokens[0];

					return first.Type switch
					{
						TokenType.PlusSymbol => BinaryExpressionOperator.Add.ToResult(),
						TokenType.MinusSymbol => BinaryExpressionOperator.Subtract.ToResult(),
						TokenType.AsteriskSymbol => BinaryExpressionOperator.Multiply.ToResult(),
						TokenType.SlashSymbol => BinaryExpressionOperator.Divide.ToResult(),
						TokenType.PercentSymbol => BinaryExpressionOperator.Modulo.ToResult(),
						TokenType.GreaterThanSymbol => BinaryExpressionOperator.GreaterThan.ToResult(),
						TokenType.LessThanSymbol => BinaryExpressionOperator.LessThan.ToResult(),
						TokenType.PipeSymbol => BinaryExpressionOperator.BitwiseOr.ToResult(),
						TokenType.AmpersandSymbol => BinaryExpressionOperator.BitwiseAnd.ToResult(),
						TokenType.HatSymbol => BinaryExpressionOperator.BitwiseXor.ToResult(),
						_ => new UnexpectedTokenException(first, TokenType.PlusSymbol, TokenType.MinusSymbol,
							TokenType.AsteriskSymbol, TokenType.SlashSymbol, TokenType.PercentSymbol,
							TokenType.GreaterThanSymbol, TokenType.LessThanSymbol, TokenType.PipeSymbol,
							TokenType.AmpersandSymbol, TokenType.HatSymbol).ToErr<BinaryExpressionOperator>(),
					};
				}
			case 0:
				return new UnexpectedEndOfTokensException(tokens.LastToken, "Expected an operator")
					.ToErr<BinaryExpressionOperator>();
			default:
				return new UnexpectedTokenException("Operators can only be chained up to 3 times", operatorTokens[0])
					.ToErr<BinaryExpressionOperator>();
		}
	}

	private ExpressionNode ParsePrimaryExpression()
	{
		var maybeTokenType = tokens.PeekType();
		if (maybeTokenType is not { } tokenType)
		{
			return diagnostics.AddUnexpectedEndOfTokensExpression(tokens.LastToken);
		}

		if (tokenType.IsLiteral())
		{
			var literalResult = tokens.Dequeue(tokenType);
			if (literalResult is Err<Token> literalError)
				return diagnostics.AddErrorExpression(literalError);

			return new LiteralExpressionNode(literalResult.Value);
		}

		switch (tokenType)
		{
			case TokenType.OpeningParentheses:
				{
					var nextType = tokens.PeekType(1);
					return nextType switch
					{
						TokenType.TypeName => ParseFunctionDefinitionExpression(),
						_ => ParseNestedExpression(),
					};
				}
			case TokenType.Identifier:
				{
					var nextType = tokens.PeekType(1);
					return nextType switch
					{
						TokenType.OpeningParentheses => ParseFunctionCallExpression(),
						_ => ParseIdentifierExpression(),
					};
				}
			case TokenType.OpeningSquareBracket:
				return ParseListExpression();
			case TokenType.OpeningCurlyBracket:
				return ParseMapExpression();
			case TokenType.MinusSymbol:
				return ParseNegateExpression();
			case TokenType.ExclamationMarkSymbol:
				return ParseNotExpression();
			case TokenType.TypeName
				when tokens.Peek()?.Value.Equals("null", StringComparison.OrdinalIgnoreCase) ?? false:
				var nullTokenResult = tokens.Dequeue(TokenType.TypeName);
				if (nullTokenResult is Err<Token> nullTokenError)
					return diagnostics.AddErrorExpression(nullTokenError);

				return new LiteralExpressionNode(new Token(TokenType.LiteralNull, nullTokenResult.Value.Value,
					nullTokenResult.Value.Location));
			default:
				return diagnostics.AddMissingExpression(tokens.LastToken);
		}
	}

	private ExpressionNode ParseNotExpression()
	{
		var exclamationMarkResult = tokens.Dequeue(TokenType.ExclamationMarkSymbol);
		if (exclamationMarkResult is Err<Token> exclamationMarkError)
			return diagnostics.AddErrorExpression(exclamationMarkError);

		var expression = ParseExpression();

		return new NotExpressionNode(exclamationMarkResult.Value, expression);
	}

	private ExpressionNode ParseNegateExpression()
	{
		var minusResult = tokens.Dequeue(TokenType.MinusSymbol);
		if (minusResult is Err<Token> minusError)
			return diagnostics.AddErrorExpression(minusError);

		var expression = ParseExpression();

		return new NegateExpressionNode(minusResult.Value, expression);
	}

	private ExpressionNode ParseMapExpression()
	{
		var openCurlyBraceResult = tokens.Dequeue(TokenType.OpeningCurlyBracket);
		if (openCurlyBraceResult is Err<Token> openError)
			return diagnostics.AddErrorExpression(openError);

		var openCurlyBrace = openCurlyBraceResult.Value;

		var map = new Dictionary<Token, ExpressionNode>();

		while (tokens.PeekType() is not TokenType.ClosingCurlyBracket)
		{
			var keyResult = tokens.Dequeue(TokenType.LiteralString);
			if (keyResult is Err<Token> keyError)
				return diagnostics.AddErrorExpression(keyError);

			_ = tokens.Dequeue(TokenType.ColonSymbol);

			var value = ParseExpression();

			map.Add(keyResult.Value, value);

			if (tokens.PeekType() is TokenType.CommaSymbol)
			{
				_ = tokens.Dequeue(TokenType.CommaSymbol);
			}
		}

		_ = tokens.Dequeue(TokenType.ClosingCurlyBracket);

		return new MapExpressionNode(openCurlyBrace, map);
	}

	private ExpressionNode ParseListExpression()
	{
		var openSquareBracketResult = tokens.Dequeue(TokenType.OpeningSquareBracket);
		if (openSquareBracketResult is Err<Token> openError)
			return diagnostics.AddErrorExpression(openError);

		var openSquareBracket = openSquareBracketResult.Value;

		var list = new List<ExpressionNode>();

		while (tokens.PeekType() is not TokenType.ClosingSquareBracket)
		{
			list.Add(ParseExpression());

			if (tokens.PeekType() is TokenType.CommaSymbol)
			{
				_ = tokens.Dequeue(TokenType.CommaSymbol);
			}
		}

		_ = tokens.Dequeue(TokenType.ClosingSquareBracket);

		return new ListExpressionNode(openSquareBracket, list);
	}

	private ExpressionNode ParseIdentifierExpression()
	{
		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddErrorExpression(identifierError);

		return new IdentifierExpressionNode(identifierResult.Value);
	}

	private ExpressionNode ParseFunctionCallExpression()
	{
		var identifierResult = tokens.Dequeue(TokenType.Identifier);
		if (identifierResult is Err<Token> identifierError)
			return diagnostics.AddErrorExpression(identifierError);

		_ = tokens.Dequeue(TokenType.OpeningParentheses);

		var arguments = ParseCallArguments();

		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		return new CallExpressionNode(identifierResult.Value, arguments);
	}

	private ExpressionNode ParseNestedExpression()
	{
		_ = tokens.Dequeue(TokenType.OpeningParentheses);

		var expression = ParseExpression();

		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		return expression;
	}

	private ExpressionNode ParseFunctionDefinitionExpression()
	{
		var openParenthesisResult = tokens.Dequeue(TokenType.OpeningParentheses);
		if (openParenthesisResult is Err<Token> openError)
			return diagnostics.AddErrorExpression(openError);

		var parameters = new List<IVariableDeclarationNode>();
		while (tokens.PeekType() is not TokenType.ClosingParentheses)
		{
			var declaration = ParseVariableDeclaration();

			parameters.Add(declaration);

			if (tokens.PeekType() is TokenType.CommaSymbol)
			{
				_ = tokens.Dequeue(TokenType.CommaSymbol);
			}
		}

		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		var body = ParseCodeBlock();

		if (tokens.PeekType() is not TokenType.OpeningParentheses)
		{
			return new FunctionDefinitionExpressionNode(openParenthesisResult.Value, parameters, body);
		}

		// direct definition call
		_ = tokens.Dequeue(TokenType.OpeningParentheses);

		var callArguments = ParseCallArguments();

		_ = tokens.Dequeue(TokenType.ClosingParentheses);

		return new FunctionDefinitionCallExpressionNode(openParenthesisResult.Value, parameters, body, callArguments);
	}

	private List<ExpressionNode> ParseCallArguments()
	{
		var arguments = new List<ExpressionNode>();

		while (tokens.PeekType() is not TokenType.ClosingParentheses)
		{
			var argumentExpression = ParseExpression();

			arguments.Add(argumentExpression);

			if (tokens.PeekType() is TokenType.CommaSymbol)
			{
				_ = tokens.Dequeue();
			}
			else
			{
				break;
			}
		}

		return arguments;
	}
}
