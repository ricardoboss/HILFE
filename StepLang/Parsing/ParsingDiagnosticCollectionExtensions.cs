using StepLang.Diagnostics;
using StepLang.Parsing.Nodes;
using StepLang.Parsing.Nodes.Expressions;
using StepLang.Parsing.Nodes.Import;
using StepLang.Parsing.Nodes.Statements;
using StepLang.Parsing.Nodes.VariableDeclarations;
using StepLang.Tokenizing;
using StepLang.Utils;

namespace StepLang.Parsing;

public static class ParsingDiagnosticCollectionExtensions
{
	public static ErrorStatementNode AddUnexpectedToken(this DiagnosticCollection collection, Token errorToken,
		params TokenType[] allowed)
	{
		var typeInfo = allowed.Length switch
		{
			0 => "any token",
			1 => $"a {allowed[0].ToDisplay()}",
			_ => $"any one of {string.Join(", ", allowed.Select(TokenTypes.ToDisplay))}",
		};

		var message = $"Unexpected {errorToken.Type.ToDisplay()}, expected {typeInfo}";

		return AddUnexpectedToken(collection, message, errorToken);
	}

	public static ErrorStatementNode AddUnexpectedToken(this DiagnosticCollection collection,
		string message, params Token[] errorTokens)
	{
		collection.Add(DiagnosticArea.Parsing, Severity.Error, message, "PAR001", errorTokens.First());

		return new ErrorStatementNode(message, errorTokens);
	}

	public static ErrorExpressionNode AddUnexpectedTokenExpression(this DiagnosticCollection collection,
		string message, params Token[] errorTokens)
	{
		collection.Add(DiagnosticArea.Parsing, Severity.Error, message, "PAR001", errorTokens.First());

		return new ErrorExpressionNode(message, errorTokens);
	}

	public static ErrorStatementNode AddUnexpectedEndOfTokens(this DiagnosticCollection collection, Token? lastToken,
		string? message = null)
	{
		collection.Add(DiagnosticArea.Parsing, Severity.Error, message ?? "Unexpected end of tokens", "PAR004",
			lastToken);

		return new ErrorStatementNode(message ?? "Unexpected end of tokens", [lastToken]);
	}

	public static ErrorExpressionNode AddUnexpectedEndOfTokensExpression(this DiagnosticCollection collection,
		Token? lastToken,
		string? message = null)
	{
		collection.Add(DiagnosticArea.Parsing, Severity.Error, message ?? "Unexpected end of tokens", "PAR004",
			lastToken);

		return new ErrorExpressionNode(message ?? "Unexpected end of tokens", [lastToken]);
	}

	public static ErrorExpressionNode AddMissingExpression(this DiagnosticCollection collection, Token? lastToken,
		string? message = null)
	{
		message ??= "A value was expected, but none was found";

		collection.Add(DiagnosticArea.Parsing, Severity.Error, message, "PAR003", lastToken);

		return new ErrorExpressionNode(message, [lastToken]);
	}

	public static ErrorImportNode AddImportError(this DiagnosticCollection collection, Err<Token> err)
	{
		return AddError(collection, err, (message, tokens) => new ErrorImportNode(message, tokens));
	}

	public static StatementNode AddError(this DiagnosticCollection collection, Err<Token> error)
	{
		return AddError(collection, error, (message, tokens) => new ErrorStatementNode(message, tokens));
	}

	public static IVariableDeclarationNode AddVariableDeclarationError(this DiagnosticCollection collection,
		Err<Token> error)
	{
		return AddError(collection, error, (message, tokens) => new ErrorVariableDeclarationNode(message, tokens));
	}

	public static ErrorExpressionNode AddErrorExpression<T>(this DiagnosticCollection collection, Err<T> error)
	{
		return AddError(collection, error, (message, tokens) => new ErrorExpressionNode(message, tokens));
	}

	private static TNode AddError<TNode, TResult>(this DiagnosticCollection collection, Err<TResult> error,
		Func<string, Token[], TNode> createNode)
		where TNode : INode
	{
		switch (error.Exception)
		{
			case ParserException { Message: { } message, LastToken: { } lastToken, ErrorCode: { } code }:
				collection.Add(DiagnosticArea.Parsing, Severity.Error, message, code, lastToken);

				return createNode(message, [lastToken]);
			case StepLangException { Message: { } message, ErrorCode: { } code }:
				collection.Add(DiagnosticArea.Parsing, Severity.Error, message, code);

				return createNode(message, []);
			default:
				var unexpected = $"Unexpected error: {error.Exception.Message}";

				collection.Add(DiagnosticArea.Other, Severity.Error, unexpected, "?????");

				return createNode(unexpected, []);
		}
	}
}
