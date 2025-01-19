using StepLang.Diagnostics;
using StepLang.Tokenizing;
using System.Diagnostics.CodeAnalysis;

namespace StepLang.Tests.Tokenizing;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class TokenizerTest
{
	[Fact]
	public void TestTokenizeLiteralString()
	{
		const string source = "\"abc\"";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(2, tokens.Length);
		Assert.Equal(TokenType.LiteralString, tokens[0].Type);
		Assert.Equal(source, tokens[0].Value);
		Assert.Equal("abc", tokens[0].StringValue);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeLiteralStringWithEscapedChars()
	{
		const string source = "\"\\n\"";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(2, tokens.Length);
		Assert.Equal(TokenType.LiteralString, tokens[0].Type);
		Assert.Equal("\n", tokens[0].StringValue);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeLiteralNumber()
	{
		const string source = "123";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(2, tokens.Length);
		Assert.Equal(TokenType.LiteralNumber, tokens[0].Type);
		Assert.Equal("123", tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeLiteralNegativeNumber()
	{
		const string source = "-123";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(3, tokens.Length);
		Assert.Equal(TokenType.MinusSymbol, tokens[0].Type);
		Assert.Equal("-", tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.LiteralNumber, tokens[1].Type);
		Assert.Equal("123", tokens[1].Value);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(2, tokens[1].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[2].Type);
		Assert.Equal(1, tokens[2].Location.Line);
		Assert.Equal(source.Length + 1, tokens[2].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeLiteralNegativeNumberWithDecimals()
	{
		const string source = "-1.23";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(3, tokens.Length);
		Assert.Equal(TokenType.MinusSymbol, tokens[0].Type);
		Assert.Equal("-", tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.LiteralNumber, tokens[1].Type);
		Assert.Equal("1.23", tokens[1].Value);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(2, tokens[1].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[2].Type);
		Assert.Equal(1, tokens[2].Location.Line);
		Assert.Equal(source.Length + 1, tokens[2].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeLiteralStringWithSpaces()
	{
		const string source = "\"abc def\"";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(2, tokens.Length);
		Assert.Equal(TokenType.LiteralString, tokens[0].Type);
		Assert.Equal(source, tokens[0].Value);
		Assert.Equal("abc def", tokens[0].StringValue);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Theory]
	[InlineData("\"abc\\\"def\"", "abc\"def")]
	public void TestTokenizeLiteralStringWithEscapedQuotes(string source, string expected)
	{
		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(2, tokens.Length);
		Assert.Equal(TokenType.LiteralString, tokens[0].Type);
		Assert.Equal(expected, tokens[0].StringValue);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Theory]
	[InlineData("string")]
	[InlineData("number")]
	[InlineData("bool")]
	[InlineData("function")]
	public void TestTokenizeKnownType(string source)
	{
		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.TypeName, tokens[0].Type);
		Assert.Equal(source, tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Theory]
	[InlineData("if", TokenType.IfKeyword)]
	[InlineData("else", TokenType.ElseKeyword)]
	[InlineData("while", TokenType.WhileKeyword)]
	[InlineData("break", TokenType.BreakKeyword)]
	[InlineData("continue", TokenType.ContinueKeyword)]
	public void TestTokenizeKeyword(string source, TokenType type)
	{
		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToArray();

		Assert.Equal(2, tokens.Length);
		Assert.Equal(type, tokens[0].Type);
		Assert.Equal(source, tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(source.Length + 1, tokens[1].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeMultipleTokens()
	{
		const string source = "number identifier = 1";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(8, tokens.Count);

		Assert.Equal(TokenType.TypeName, tokens[0].Type);
		Assert.Equal("number", tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(6, tokens[0].Location.Length);

		Assert.Equal(TokenType.Whitespace, tokens[1].Type);
		Assert.Equal(" ", tokens[1].Value);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(7, tokens[1].Location.Column);
		Assert.Equal(1, tokens[1].Location.Length);

		Assert.Equal(TokenType.Identifier, tokens[2].Type);
		Assert.Equal("identifier", tokens[2].Value);
		Assert.Equal(1, tokens[2].Location.Line);
		Assert.Equal(8, tokens[2].Location.Column);
		Assert.Equal(10, tokens[2].Location.Length);

		Assert.Equal(TokenType.Whitespace, tokens[3].Type);
		Assert.Equal(" ", tokens[3].Value);
		Assert.Equal(1, tokens[3].Location.Line);
		Assert.Equal(18, tokens[3].Location.Column);
		Assert.Equal(1, tokens[3].Location.Length);

		Assert.Equal(TokenType.EqualsSymbol, tokens[4].Type);
		Assert.Equal("=", tokens[4].Value);
		Assert.Equal(1, tokens[4].Location.Line);
		Assert.Equal(19, tokens[4].Location.Column);
		Assert.Equal(tokens[3].Location.Column + tokens[3].Location.Length, tokens[4].Location.Column);
		Assert.Equal(1, tokens[4].Location.Length);

		Assert.Equal(TokenType.Whitespace, tokens[5].Type);
		Assert.Equal(" ", tokens[5].Value);
		Assert.Equal(1, tokens[5].Location.Line);
		Assert.Equal(20, tokens[5].Location.Column);
		Assert.Equal(1, tokens[5].Location.Length);

		Assert.Equal(TokenType.LiteralNumber, tokens[6].Type);
		Assert.Equal("1", tokens[6].Value);
		Assert.Equal(1, tokens[6].Location.Line);
		Assert.Equal(21, tokens[6].Location.Column);
		Assert.Equal(1, tokens[6].Location.Length);

		Assert.Equal(TokenType.EndOfFile, tokens[7].Type);
		Assert.Equal(1, tokens[7].Location.Line);
		Assert.Equal(22, tokens[7].Location.Column);
		Assert.Equal(0, tokens[7].Location.Length);

		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeKeywords()
	{
		const string source = "if (true)";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(6, tokens.Count);
		Assert.Equal(TokenType.IfKeyword, tokens[0].Type);
		Assert.Equal("if", tokens[0].Value);
		Assert.Equal(1, tokens[0].Location.Line);
		Assert.Equal(1, tokens[0].Location.Column);
		Assert.Equal(TokenType.Whitespace, tokens[1].Type);
		Assert.Equal(" ", tokens[1].Value);
		Assert.Equal(1, tokens[1].Location.Line);
		Assert.Equal(3, tokens[1].Location.Column);
		Assert.Equal(TokenType.OpeningParentheses, tokens[2].Type);
		Assert.Equal("(", tokens[2].Value);
		Assert.Equal(1, tokens[2].Location.Line);
		Assert.Equal(4, tokens[2].Location.Column);
		Assert.Equal(TokenType.LiteralBoolean, tokens[3].Type);
		Assert.Equal("true", tokens[3].Value);
		Assert.Equal(1, tokens[3].Location.Line);
		Assert.Equal(5, tokens[3].Location.Column);
		Assert.Equal(TokenType.ClosingParentheses, tokens[4].Type);
		Assert.Equal(")", tokens[4].Value);
		Assert.Equal(1, tokens[4].Location.Line);
		Assert.Equal(9, tokens[4].Location.Column);
		Assert.Equal(TokenType.EndOfFile, tokens[5].Type);
		Assert.Equal(1, tokens[5].Location.Line);
		Assert.Equal(10, tokens[5].Location.Column);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeFunctionCall()
	{
		const string source = "print(\"hello\")";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(5, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("print", tokens[0].Value);
		Assert.Equal(TokenType.OpeningParentheses, tokens[1].Type);
		Assert.Equal("(", tokens[1].Value);
		Assert.Equal(TokenType.LiteralString, tokens[2].Type);
		Assert.Equal("\"hello\"", tokens[2].Value);
		Assert.Equal(TokenType.ClosingParentheses, tokens[3].Type);
		Assert.Equal(")", tokens[3].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[4].Type);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeLineComment()
	{
		const string source = "f(variable) // this is a comment\nprintln(\"text\") // more comments";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(14, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("f", tokens[0].Value);
		Assert.Equal(TokenType.OpeningParentheses, tokens[1].Type);
		Assert.Equal("(", tokens[1].Value);
		Assert.Equal(TokenType.Identifier, tokens[2].Type);
		Assert.Equal("variable", tokens[2].Value);
		Assert.Equal(TokenType.ClosingParentheses, tokens[3].Type);
		Assert.Equal(")", tokens[3].Value);
		Assert.Equal(TokenType.Whitespace, tokens[4].Type);
		Assert.Equal(" ", tokens[4].Value);
		Assert.Equal(TokenType.LineComment, tokens[5].Type);
		Assert.Equal("// this is a comment", tokens[5].Value);
		Assert.Equal(TokenType.NewLine, tokens[6].Type);
		Assert.Equal("\n", tokens[6].Value);
		Assert.Equal(TokenType.Identifier, tokens[7].Type);
		Assert.Equal("println", tokens[7].Value);
		Assert.Equal(TokenType.OpeningParentheses, tokens[8].Type);
		Assert.Equal("(", tokens[8].Value);
		Assert.Equal(TokenType.LiteralString, tokens[9].Type);
		Assert.Equal("\"text\"", tokens[9].Value);
		Assert.Equal(TokenType.ClosingParentheses, tokens[10].Type);
		Assert.Equal(")", tokens[10].Value);
		Assert.Equal(TokenType.Whitespace, tokens[11].Type);
		Assert.Equal(" ", tokens[11].Value);
		Assert.Equal(TokenType.LineComment, tokens[12].Type);
		Assert.Equal("// more comments", tokens[12].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[13].Type);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestTokenizeEmitsAnIdentifierBeforeALiteralString()
	{
		const string source = "identifier\"\"";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(3, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("identifier", tokens[0].Value);
		Assert.Equal(TokenType.LiteralString, tokens[1].Type);
		Assert.Equal("\"\"", tokens[1].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[2].Type);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestReportsInvalidIdentifierDiagnostic()
	{
		const string source = "number a.b = 1";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(8, tokens.Count);
		Assert.Equal(TokenType.TypeName, tokens[0].Type);
		Assert.Equal("number", tokens[0].Value);
		Assert.Equal(TokenType.Whitespace, tokens[1].Type);
		Assert.Equal(" ", tokens[1].Value);
		Assert.Equal(TokenType.Error, tokens[2].Type);
		Assert.Equal("a.b", tokens[2].Value);
		Assert.Equal(TokenType.Whitespace, tokens[3].Type);
		Assert.Equal(" ", tokens[3].Value);
		Assert.Equal(TokenType.EqualsSymbol, tokens[4].Type);
		Assert.Equal("=", tokens[4].Value);
		Assert.Equal(TokenType.Whitespace, tokens[5].Type);
		Assert.Equal(" ", tokens[5].Value);
		Assert.Equal(TokenType.LiteralNumber, tokens[6].Type);
		Assert.Equal("1", tokens[6].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[7].Type);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(Severity.Error, diagnostic.Severity);
		Assert.Equal("Invalid identifier", diagnostic.Message);
		Assert.Equal("TOK001", diagnostic.Code);
		Assert.Null(diagnostic.Kind);
		Assert.Equal(DiagnosticArea.Tokenizing, diagnostic.Area);
		Assert.Equal(tokens[2], diagnostic.Token);
		Assert.Equal(1, diagnostic.Location?.Line);
		Assert.Equal(8, diagnostic.Location?.Column);
		Assert.Equal(3, diagnostic.Location?.Length);
		Assert.Null(diagnostic.RelatedTokens);
	}

	[Fact]
	public void TestReportsUnterminatedStringDiagnostic()
	{
		const string source = "\"string";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Error, tokens[0].Type);
		Assert.Equal("\"string", tokens[0].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);

		var diagnostic = Assert.Single(diagnostics);
		Assert.Equal(Severity.Error, diagnostic.Severity);
		Assert.Equal("Unterminated string", diagnostic.Message);
		Assert.Equal("TOK002", diagnostic.Code);
		Assert.Null(diagnostic.Kind);
		Assert.Equal(DiagnosticArea.Tokenizing, diagnostic.Area);
		Assert.Equal(tokens[0], diagnostic.Token);
		Assert.Equal(1, diagnostic.Location?.Line);
		Assert.Equal(1, diagnostic.Location?.Column);
		Assert.Equal(source.Length, diagnostic.Location?.Length);
		Assert.Null(diagnostic.RelatedTokens);
	}

	[Fact]
	public void TestTokenizeKeywordInIdentifier()
	{
		// identifier "ifempty" contains the keyword "if"
		const string source = "println(ifempty(\"abc\", \"b\"))";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(11, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("println", tokens[0].Value);
		Assert.Equal(TokenType.OpeningParentheses, tokens[1].Type);
		Assert.Equal(TokenType.Identifier, tokens[2].Type);
		Assert.Equal("ifempty", tokens[2].Value);
		Assert.Equal(TokenType.OpeningParentheses, tokens[3].Type);
		Assert.Equal(TokenType.LiteralString, tokens[4].Type);
		Assert.Equal("\"abc\"", tokens[4].Value);
		Assert.Equal(TokenType.CommaSymbol, tokens[5].Type);
		Assert.Equal(TokenType.Whitespace, tokens[6].Type);
		Assert.Equal(TokenType.LiteralString, tokens[7].Type);
		Assert.Equal("\"b\"", tokens[7].Value);
		Assert.Equal(TokenType.ClosingParentheses, tokens[8].Type);
		Assert.Equal(TokenType.ClosingParentheses, tokens[9].Type);
		Assert.Equal(TokenType.EndOfFile, tokens[10].Type);
	}

	[Fact]
	public void TestTokenizeLineCommentAfterTokens()
	{
		const string source = "a = b// more comments";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(7, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("a", tokens[0].Value);
		Assert.Equal(TokenType.Whitespace, tokens[1].Type);
		Assert.Equal(" ", tokens[1].Value);
		Assert.Equal(TokenType.EqualsSymbol, tokens[2].Type);
		Assert.Equal("=", tokens[2].Value);
		Assert.Equal(TokenType.Whitespace, tokens[3].Type);
		Assert.Equal(" ", tokens[3].Value);
		Assert.Equal(TokenType.Identifier, tokens[4].Type);
		Assert.Equal("b", tokens[4].Value);
		Assert.Equal(TokenType.LineComment, tokens[5].Type);
		Assert.Equal("// more comments", tokens[5].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[6].Type);
		Assert.Empty(diagnostics);
	}

	[Fact]
	public void TestParsesStringsWithUnescapedControlCharacters()
	{
		const string source = "\"abc\r";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(3, tokens.Count);
		Assert.Equal(TokenType.Error, tokens[0].Type);
		Assert.Equal("\"abc", tokens[0].Value);
		Assert.Equal(TokenType.Error, tokens[1].Type);
		Assert.Equal("\r", tokens[1].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[2].Type);

		Assert.Equal(2, diagnostics.Count);

		Assert.Equal(DiagnosticArea.Tokenizing, diagnostics[0].Area);
		Assert.Equal("TOK002", diagnostics[0].Code);
		Assert.Equal(1, diagnostics[0].Column);
		Assert.Equal(4, diagnostics[0].Length);
		Assert.Equal(1, diagnostics[0].Line);
		Assert.Equal("Unterminated string", diagnostics[0].Message);
		Assert.Equal(Severity.Error, diagnostics[0].Severity);

		Assert.Equal(DiagnosticArea.Tokenizing, diagnostics[1].Area);
		Assert.Equal("TOK002", diagnostics[1].Code);
		Assert.Equal(5, diagnostics[1].Column);
		Assert.Equal(1, diagnostics[1].Length);
		Assert.Equal(1, diagnostics[1].Line);
		Assert.Equal("Unescaped control character", diagnostics[1].Message);
		Assert.Equal(Severity.Error, diagnostics[1].Severity);
	}

	[Fact]
	public void TestReturnsNewLineAfterUnterminatedString()
	{
		const string source = "\"abc\n";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(3, tokens.Count);
		Assert.Equal(TokenType.Error, tokens[0].Type);
		Assert.Equal("\"abc", tokens[0].Value);
		Assert.Equal(TokenType.NewLine, tokens[1].Type);
		Assert.Equal("\n", tokens[1].Value);
		Assert.Equal(TokenType.EndOfFile, tokens[2].Type);

		var diagnostic = Assert.Single(diagnostics);

		Assert.Equal(DiagnosticArea.Tokenizing, diagnostic.Area);
		Assert.Equal("TOK002", diagnostic.Code);
		Assert.Equal(1, diagnostic.Column);
		Assert.Equal(4, diagnostic.Length);
		Assert.Equal(1, diagnostic.Line);
		Assert.Equal("Unterminated string", diagnostic.Message);
		Assert.Equal(Severity.Error, diagnostic.Severity);
	}

	[Fact]
	public void TestSkipsCarriageReturns()
	{
		const string source = "\r\n";

		var diagnostics = new DiagnosticCollection();
		var tokenizer = new Tokenizer(source, diagnostics);
		var tokens = tokenizer.Tokenize(TestContext.Current.CancellationToken).ToList();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.NewLine, tokens[0].Type);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
		Assert.Empty(diagnostics);
	}
}
