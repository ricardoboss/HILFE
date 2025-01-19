using System.Diagnostics.CodeAnalysis;

namespace StepLang.Tokenizing;

public enum TokenType
{
	TypeName,
	Identifier,
	EqualsSymbol,
	LiteralString,
	LiteralNumber,
	LiteralBoolean,
	LiteralNull,
	Whitespace,
	NewLine,
	IfKeyword,
	ElseKeyword,
	WhileKeyword,
	BreakKeyword,
	ContinueKeyword,
	OpeningCurlyBracket,
	ClosingCurlyBracket,
	OpeningParentheses,
	ClosingParentheses,
	CommaSymbol,
	GreaterThanSymbol,
	LessThanSymbol,
	PlusSymbol,
	MinusSymbol,
	AsteriskSymbol,
	SlashSymbol,
	PercentSymbol,
	PipeSymbol,
	AmpersandSymbol,
	ExclamationMarkSymbol,
	HatSymbol,
	QuestionMarkSymbol,
	ReturnKeyword,
	UnderscoreSymbol,
	LineComment,
	OpeningSquareBracket,
	ClosingSquareBracket,
	ColonSymbol,
	ImportKeyword,
	ForEachKeyword,
	InKeyword,
	EndOfFile,
	Error,
}

public static class TokenTypes
{
	[ExcludeFromCodeCoverage]
	public static string ToDisplay(this TokenType type)
	{
		return type switch
		{
			TokenType.TypeName => "type name",
			TokenType.Identifier => "identifier",
			TokenType.EqualsSymbol => "'='",
			TokenType.LiteralString => "literal string",
			TokenType.LiteralNumber => "literal number",
			TokenType.LiteralBoolean => "literal boolean",
			TokenType.LiteralNull => "null",
			TokenType.Whitespace => "whitespace",
			TokenType.NewLine => "new line",
			TokenType.IfKeyword => "'if'",
			TokenType.ElseKeyword => "'else'",
			TokenType.WhileKeyword => "'while'",
			TokenType.BreakKeyword => "'break'",
			TokenType.ContinueKeyword => "'continue'",
			TokenType.OpeningCurlyBracket => "'{'",
			TokenType.ClosingCurlyBracket => "'}'",
			TokenType.OpeningParentheses => "'('",
			TokenType.ClosingParentheses => "')'",
			TokenType.CommaSymbol => "','",
			TokenType.GreaterThanSymbol => "'>'",
			TokenType.LessThanSymbol => "'<'",
			TokenType.PlusSymbol => "'+'",
			TokenType.MinusSymbol => "'-'",
			TokenType.AsteriskSymbol => "'*'",
			TokenType.SlashSymbol => "'/'",
			TokenType.PercentSymbol => "'%'",
			TokenType.PipeSymbol => "'|'",
			TokenType.AmpersandSymbol => "'&'",
			TokenType.ExclamationMarkSymbol => "'!'",
			TokenType.HatSymbol => "'^'",
			TokenType.QuestionMarkSymbol => "'?'",
			TokenType.ReturnKeyword => "'return'",
			TokenType.UnderscoreSymbol => "'_'",
			TokenType.LineComment => "line comment",
			TokenType.OpeningSquareBracket => "'['",
			TokenType.ClosingSquareBracket => "']'",
			TokenType.ColonSymbol => "':'",
			TokenType.ImportKeyword => "'import'",
			TokenType.ForEachKeyword => "'foreach'",
			TokenType.InKeyword => "'in'",
			TokenType.EndOfFile => "end of file",
			TokenType.Error => "error",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown token type"),
		};
	}

	public static string ToCode(this TokenType? type)
	{
		return type switch
		{
			TokenType.BreakKeyword => "break",
			TokenType.ContinueKeyword => "continue",
			TokenType.ElseKeyword => "else",
			TokenType.IfKeyword => "if",
			TokenType.ImportKeyword => "import",
			TokenType.ReturnKeyword => "return",
			TokenType.WhileKeyword => "while",
			TokenType.AmpersandSymbol => "&",
			TokenType.AsteriskSymbol => "*",
			TokenType.ClosingCurlyBracket => "}",
			TokenType.ClosingParentheses => ")",
			TokenType.ColonSymbol => ":",
			TokenType.CommaSymbol => ",",
			TokenType.EqualsSymbol => "=",
			TokenType.ExclamationMarkSymbol => "!",
			TokenType.GreaterThanSymbol => ">",
			TokenType.HatSymbol => "^",
			TokenType.LessThanSymbol => "<",
			TokenType.MinusSymbol => "-",
			TokenType.OpeningCurlyBracket => "{",
			TokenType.OpeningParentheses => "(",
			TokenType.PercentSymbol => "%",
			TokenType.PipeSymbol => "|",
			TokenType.PlusSymbol => "+",
			TokenType.QuestionMarkSymbol => "?",
			TokenType.SlashSymbol => "/",
			TokenType.UnderscoreSymbol => "_",
			TokenType.OpeningSquareBracket => "[",
			TokenType.ClosingSquareBracket => "]",
			TokenType.ForEachKeyword => "foreach",
			TokenType.InKeyword => "in",
			TokenType.LiteralNull => "null",
			_ => throw new InvalidOperationException("Token type does not have a static code representation"),
		};
	}

	public static bool IsKnownTypeName(this string name)
	{
		return name.ToUpperInvariant() is "STRING" or "NUMBER" or "BOOL" or "FUNCTION" or "LIST" or "MAP";
	}

	public static bool TryParseKeyword(this string name, [NotNullWhen(true)] out TokenType? type)
	{
		switch (name.ToUpperInvariant())
		{
			case "IF":
				type = TokenType.IfKeyword;
				return true;
			case "ELSE":
				type = TokenType.ElseKeyword;
				return true;
			case "WHILE":
				type = TokenType.WhileKeyword;
				return true;
			case "BREAK":
				type = TokenType.BreakKeyword;
				return true;
			case "CONTINUE":
				type = TokenType.ContinueKeyword;
				return true;
			case "RETURN":
				type = TokenType.ReturnKeyword;
				return true;
			case "IMPORT":
				type = TokenType.ImportKeyword;
				return true;
			case "FOREACH":
				type = TokenType.ForEachKeyword;
				return true;
			case "IN":
				type = TokenType.InKeyword;
				return true;
		}

		type = null;
		return false;
	}

	public static bool TryParseSymbol(this char symbol, [NotNullWhen(true)] out TokenType? type)
	{
		switch (symbol)
		{
			case ' ':
			case '\t':
				type = TokenType.Whitespace;
				return true;
			case '\n':
			case '\r':
				type = TokenType.NewLine;
				return true;
			case '{':
				type = TokenType.OpeningCurlyBracket;
				return true;
			case '}':
				type = TokenType.ClosingCurlyBracket;
				return true;
			case '(':
				type = TokenType.OpeningParentheses;
				return true;
			case ')':
				type = TokenType.ClosingParentheses;
				return true;
			case '[':
				type = TokenType.OpeningSquareBracket;
				return true;
			case ']':
				type = TokenType.ClosingSquareBracket;
				return true;
			case '=':
				type = TokenType.EqualsSymbol;
				return true;
			case '|':
				type = TokenType.PipeSymbol;
				return true;
			case '&':
				type = TokenType.AmpersandSymbol;
				return true;
			case '!':
				type = TokenType.ExclamationMarkSymbol;
				return true;
			case '?':
				type = TokenType.QuestionMarkSymbol;
				return true;
			case '^':
				type = TokenType.HatSymbol;
				return true;
			case '>':
				type = TokenType.GreaterThanSymbol;
				return true;
			case '<':
				type = TokenType.LessThanSymbol;
				return true;
			case '+':
				type = TokenType.PlusSymbol;
				return true;
			case '-':
				type = TokenType.MinusSymbol;
				return true;
			case '*':
				type = TokenType.AsteriskSymbol;
				return true;
			case '/':
				type = TokenType.SlashSymbol;
				return true;
			case '%':
				type = TokenType.PercentSymbol;
				return true;
			case ',':
				type = TokenType.CommaSymbol;
				return true;
			case '_':
				type = TokenType.UnderscoreSymbol;
				return true;
			case ':':
				type = TokenType.ColonSymbol;
				return true;
		}

		type = null;
		return false;
	}

	public static readonly TokenType[] ShorthandMathematicalOperations =
	[
		TokenType.PlusSymbol,
		TokenType.MinusSymbol,
	];

	public static bool IsShorthandMathematicalOperation(this TokenType type)
	{
		return type switch
		{
			TokenType.PlusSymbol or TokenType.MinusSymbol => true,
			_ => false,
		};
	}

	public static readonly TokenType[] ShorthandMathematicalOperationsWithAssignment =
	[
		TokenType.PlusSymbol,
		TokenType.MinusSymbol,
		TokenType.AsteriskSymbol,
		TokenType.SlashSymbol,
		TokenType.PercentSymbol,
		TokenType.PipeSymbol,
		TokenType.AmpersandSymbol,
		TokenType.HatSymbol,
		TokenType.QuestionMarkSymbol,
	];

	public static bool IsShorthandMathematicalOperationWithAssignment(this TokenType type)
	{
		return type switch
		{
			TokenType.PlusSymbol or TokenType.MinusSymbol or TokenType.AsteriskSymbol or TokenType.SlashSymbol
				or TokenType.PercentSymbol or TokenType.PipeSymbol or TokenType.AmpersandSymbol or TokenType.HatSymbol
				or TokenType.QuestionMarkSymbol => true,
			_ => false,
		};
	}

	public static readonly TokenType[] MathematicalOperations =
	[
		TokenType.PlusSymbol,
		TokenType.MinusSymbol,
		TokenType.AsteriskSymbol,
		TokenType.SlashSymbol,
		TokenType.PercentSymbol,
	];

	public static bool IsMathematicalOperation(this TokenType type)
	{
		return type switch
		{
			TokenType.PlusSymbol or TokenType.MinusSymbol or TokenType.AsteriskSymbol or TokenType.SlashSymbol
				or TokenType.PercentSymbol => true,
			_ => false,
		};
	}

	public static bool IsLiteral(this TokenType type)
	{
		return type switch
		{
			TokenType.LiteralString or TokenType.LiteralNumber or TokenType.LiteralBoolean
				or TokenType.LiteralNull => true,
			_ => false,
		};
	}

	public static readonly TokenType[] Operators =
	[
		TokenType.PlusSymbol,
		TokenType.MinusSymbol,
		TokenType.AsteriskSymbol,
		TokenType.SlashSymbol,
		TokenType.PercentSymbol,
		TokenType.PipeSymbol,
		TokenType.AmpersandSymbol,
		TokenType.ExclamationMarkSymbol,
		TokenType.HatSymbol,
		TokenType.QuestionMarkSymbol,
		TokenType.EqualsSymbol,
		TokenType.GreaterThanSymbol,
		TokenType.LessThanSymbol,
	];

	public static bool IsOperator(this TokenType type)
	{
		return type switch
		{
			TokenType.PlusSymbol or TokenType.MinusSymbol or TokenType.AsteriskSymbol or TokenType.SlashSymbol
				or TokenType.PercentSymbol or TokenType.PipeSymbol or TokenType.AmpersandSymbol
				or TokenType.ExclamationMarkSymbol or TokenType.HatSymbol or TokenType.QuestionMarkSymbol
				or TokenType.EqualsSymbol or TokenType.GreaterThanSymbol or TokenType.LessThanSymbol => true,
			_ => false,
		};
	}

	public static bool HasMeaning(this TokenType type)
	{
		return type switch
		{
			TokenType.Whitespace or TokenType.LineComment => false,
			_ => true,
		};
	}
}
