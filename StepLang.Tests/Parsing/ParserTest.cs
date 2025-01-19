using StepLang.Diagnostics;
using StepLang.Parsing;
using StepLang.Parsing.Nodes.Statements;
using StepLang.Tokenizing;

namespace StepLang.Tests.Parsing;

public class ParserTest
{
	[Fact]
	public void TestParseIdentifierUsageThrowForIncompleteStatement()
	{
		// Arrange
		const string source = "identifier";
		var diagnostics = new DiagnosticCollection();
		var tokens = source.AsTokens(diagnostics);

		var parser = new Parser(tokens, diagnostics);

		// Act
		var root = parser.ParseRoot();

		// Assert
		var errorStatement = Assert.IsType<ErrorStatementNode>(root.Body.First());
		Assert.Equal("Unexpected end of tokens", errorStatement.Description);
		Assert.Equal(TokenType.EndOfFile, errorStatement.Tokens.First()?.Type);
	}
}
